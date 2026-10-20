using System.Collections.Generic;
using System.Linq;
using FightingGame.VfxLab.Files;
using FightingGame.VfxLab.Panels;
using FightingGame.VfxLab.View;
using Godot;

namespace FightingGame.VfxLab;

/// <summary>
/// The VFX Lab (ADR-0029, dev only): a sheet's variants playing side by side on one clock, the rack to tune the
/// selected one, and the bars for playback and view options. Saves the devs' feedback and reloads changed files live.
/// </summary>
/// <remarks>Space plays or pauses, ←/→ step a frame (Shift: 10), Home goes to the loop's start.</remarks>
public partial class LabRoot : Control
{
	// Developer settings: how often files are checked and feedback saved, and frames to wait before a capture so
	// screen-reading shaders have a picture to read.
	private const double CheckSeconds = 0.5;
	private const int CaptureSettleFrames = 8;
	private const string EffectsFolder = "res://Assets/Effects";
	private static readonly Color _background = new(0.06f, 0.055f, 0.075f);

	private readonly LabClock _clock = new();
	private readonly LabViewOptions _options = new();
	private readonly VBoxContainer _headerSlot = new();
	private readonly VBoxContainer _viewSlot = new();
	private readonly LabTransportBar _transport = new();
	private readonly GridContainer _grid = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	private readonly LabRack _rack = new();
	private readonly List<LabVariantCell> _cells = [];
	private LabArguments _arguments = new();
	private LabStagePicture? _picture;
	private LabSession? _session;
	private LabFileWatch? _watch;
	private LabHeaderBar _header = new();
	private string _sheetId = string.Empty;
	private string? _selected;
	private bool _dirty;
	private double _sinceCheck;
	private int _captureWait = CaptureSettleFrames;

	public override void _Ready()
	{
		_arguments = LabArguments.Parse(OS.GetCmdlineUserArgs(), out string argumentError);
		_arguments.ApplyTo(_options);
		RenderingServer.SetDefaultClearColor(_background);
		_picture = LabStagePicture.Load();
		if (_arguments.Capture is not null)
		{
			_clock.Playing = false;
			_clock.Frame = _arguments.Frame;
			if (_arguments.Size is { } size)
			{
				GetWindow().Size = new Vector2I(size.Width, size.Height);
			}
		}

		BuildLayout();
		_rack.ValuesChanged = id => ApplyValues(id, dirty: true);
		_rack.TimingChanged = () => _dirty = true;
		_rack.ForkAsked = Fork;
		List<string> sheets = LabSheetFolder.List();
		OpenSheet(_arguments.Sheet ?? sheets.FirstOrDefault() ?? string.Empty);
		if (argumentError.Length > 0)
		{
			_header.ShowStatus(argumentError, problem: true);
		}
	}

	public override void _Process(double delta)
	{
		_clock.Advance(delta);
		if (_session is not null)
		{
			ShowFrame(_session.Timing);
		}

		_sinceCheck += delta;
		if (_sinceCheck >= CheckSeconds)
		{
			_sinceCheck = 0.0;
			SaveIfChanged();
			ReloadChangedFiles();
		}

		if (_arguments.Capture is { } path && --_captureWait == 0)
		{
			Error saved = GetViewport().GetTexture().GetImage().SavePng(path);
			GD.Print(saved == Error.Ok ? $"VFX Lab: saved {path}" : $"VFX Lab: could not save {path}: {saved}");
			GetTree().Quit();
		}
	}

	public override void _UnhandledKeyInput(InputEvent input)
	{
		if (input is not InputEventKey { Pressed: true } key || GetViewport().GuiGetFocusOwner() is LineEdit or SpinBox)
		{
			return;
		}

		int step = key.ShiftPressed ? 10 : 1;
		switch (key.Keycode)
		{
			case Key.Space:
				_clock.Playing = !_clock.Playing;
				break;
			case Key.Left:
				_clock.Step(-step);
				break;
			case Key.Right:
				_clock.Step(step);
				break;
			case Key.Home:
				_clock.Frame = 0;
				_clock.Playing = false;
				break;
			default:
				return;
		}

		GetViewport().SetInputAsHandled();
	}

	private void BuildLayout()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MarginContainer margin = new();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		foreach (string side in new[] { "left", "right", "top", "bottom" })
		{
			margin.AddThemeConstantOverride($"margin_{side}", 10);
		}

		VBoxContainer column = new();
		column.AddChild(_headerSlot);
		_transport.Build(_clock, _options, () => _clock.Frame = 0);
		column.AddChild(_transport);
		column.AddChild(_viewSlot);
		HSplitContainer split = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
		ScrollContainer scroll = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		_grid.AddThemeConstantOverride("h_separation", 10);
		_grid.AddThemeConstantOverride("v_separation", 10);
		scroll.AddChild(_grid);
		split.AddChild(scroll);
		split.AddChild(_rack);
		column.AddChild(split);
		margin.AddChild(column);
		AddChild(margin);
	}

	private void OpenSheet(string sheetId)
	{
		SaveIfChanged();
		_sheetId = sheetId;
		string error = $"No sheets in {LabSheetFolder.Root}";
		_session = sheetId.Length > 0 ? LabSheetFolder.Open(sheetId, out error) : null;

		Replace(_headerSlot, _header = new LabHeaderBar());
		_header.Build(LabSheetFolder.List(), sheetId, _session?.Sheet, OpenSheet);
		LabViewBar viewBar = new();
		if (_session is not null)
		{
			viewBar.Build(_session.Sheet, _options, _picture is not null, OnViewChanged);
		}

		Replace(_viewSlot, viewBar);
		_watch = sheetId.Length > 0 ? new LabFileWatch(LabSheetFolder.FolderOf(sheetId), EffectsFolder) : null;
		BuildGrid();
		string problem = error.Length > 0 ? error : _session?.Problem ?? string.Empty;
		_header.ShowStatus(problem.Length > 0 ? problem : "Changes save to feedback.json as you make them.", problem.Length > 0);
	}

	private void BuildGrid()
	{
		foreach (LabVariantCell cell in _cells)
		{
			cell.QueueFree();
		}

		_cells.Clear();
		if (_session is null)
		{
			_rack.Show(null!, null, [], _options);
			return;
		}

		LabCellActions actions = new(Select, Fork, Solo, () => _dirty = true);
		List<LabVariant> shown = _session.Shown(_options);
		_grid.Columns = _options.Solo is null ? _options.Columns : 1;
		foreach (LabVariant variant in shown)
		{
			LabVariantCell cell = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			cell.Build(variant, _session, _picture, actions, _options.Solo == variant.Id);
			_cells.Add(cell);
			_grid.AddChild(cell);
			ApplyValues(variant.Id, dirty: false);
		}

		Select(shown.Any(variant => variant.Id == _selected) ? _selected : shown.FirstOrDefault()?.Id);
	}

	private void ShowFrame(LabTiming timing)
	{
		LabMoment moment = LabTimeline.At(_clock.Frame, timing, _options.Loop);
		foreach (LabVariantCell cell in _cells)
		{
			cell.View.ShowFrame(moment, timing, _options);
			cell.SetGreyscale(_options.Greyscale);
		}

		int phaseLength = moment.Phase switch
		{
			LabPhase.Charge => timing.Charge,
			LabPhase.Flight => timing.Flight,
			LabPhase.Impact => timing.Impact,
			_ => timing.Rest,
		};
		_transport.ShowTime(moment, LabTimeline.Length(timing, _options.Loop), phaseLength);
	}

	private void ApplyValues(string variantId, bool dirty)
	{
		_dirty |= dirty;
		if (_session?.Variants.GetValueOrDefault(variantId) is not { } variant)
		{
			return;
		}

		Dictionary<string, LabValue> values = _session.ValuesFor(variant, _options.PaletteLock);
		foreach (LabVariantCell cell in _cells.Where(cell => cell.VariantId == variantId))
		{
			cell.View.Apply(values);
		}
	}

	private void Select(string? variantId)
	{
		_selected = variantId;
		LabVariantCell? chosen = null;
		foreach (LabVariantCell cell in _cells)
		{
			cell.SetSelected(cell.VariantId == variantId);
			chosen = cell.VariantId == variantId ? cell : chosen;
		}

		LabVariant? variant = variantId is null ? null : _session?.Variants.GetValueOrDefault(variantId);
		_rack.Show(_session!, variant, chosen?.View.Cards ?? [], _options);
	}

	private void Fork(string variantId)
	{
		LabVariant fork = _session!.Fork(variantId);
		_dirty = true;
		_selected = fork.Id;
		if (_options.Round is not LabSession.AllRounds and not LabSession.ForkRound)
		{
			_options.Round = LabSession.ForkRound;
		}

		_options.Solo = null;
		BuildGrid();
		_header.ShowStatus($"Forked {variantId} as {fork.Id}");
	}

	private void Solo(string variantId)
	{
		_options.Solo = _options.Solo == variantId ? null : variantId;
		_selected = variantId;
		BuildGrid();
	}

	private void OnViewChanged(LabViewChange change)
	{
		if (change == LabViewChange.Grid)
		{
			BuildGrid();
		}
		else if (change == LabViewChange.Values)
		{
			foreach (LabVariantCell cell in _cells)
			{
				ApplyValues(cell.VariantId, dirty: false);
			}

			Select(_selected);
		}
	}

	private void SaveIfChanged()
	{
		if (!_dirty || _session is null || _arguments.Capture is not null)
		{
			return;
		}

		_dirty = false;
		LabSheetFolder.Save(_sheetId, _session.Feedback);
		_header.ShowStatus($"Saved feedback.json at {Time.GetTimeStringFromSystem()}");
	}

	private void ReloadChangedFiles()
	{
		if (_watch?.Changed() is not { Count: > 0 } changed)
		{
			return;
		}

		LabShaderReload.Reload(changed, _watch.Shaders());
		if (changed.Any(path => path.EndsWith("sheet.json", System.StringComparison.Ordinal)))
		{
			OpenSheet(_sheetId);
		}
		else
		{
			BuildGrid();
		}

		_header.ShowStatus($"Reloaded {string.Join(", ", changed.Select(path => path.GetFile()))}");
	}

	private static void Replace(Container slot, Control content)
	{
		foreach (Node child in slot.GetChildren())
		{
			child.QueueFree();
		}

		slot.AddChild(content);
	}
}
