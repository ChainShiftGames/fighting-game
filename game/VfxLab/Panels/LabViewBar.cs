using System;
using System.Collections.Generic;
using System.Linq;
using FightingGame.Presentation.World.Projectiles;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>The grid's view options: which round, layout, backdrop, framing, impact kind and the readability checks.</summary>
public partial class LabViewBar : HFlowContainer
{
    private static readonly string[] _backdrops = ["Day stage", "Night", "Flat grey", "Black", "White", "Checker"];

    /// <summary>Call once per sheet, before the bar joins the scene.</summary>
    /// <param name="hasPicture">Whether the stage art file has a picture to offer as a backdrop.</param>
    public void Build(LabSheet sheet, LabViewOptions options, bool hasPicture, Action<LabViewChange> changed)
    {
        List<string> roundIds = [LabSession.AllRounds, .. sheet.Rounds.Select(round => round.Id), LabSession.ForkRound];
        List<string> roundNames = ["All rounds", .. sheet.Rounds.Select(round => round.Name), "Your forks"];
        AddChild(LabChoice.Of("Which round to show", [.. roundNames], Math.Max(0, roundIds.IndexOf(options.Round)), index =>
        {
            options.Round = roundIds[index];
            options.Solo = null;
            changed(LabViewChange.Grid);
        }));
        AddChild(LabChoice.Toggle("Picks only", "Only the variants marked ★ Pick", options.PicksOnly, on => Set(() => options.PicksOnly = on, LabViewChange.Grid)));
        AddChild(LabChoice.Toggle("Show rejected", "Bring back the variants you rejected", options.ShowHidden, on => Set(() => options.ShowHidden = on, LabViewChange.Grid)));
        AddChild(LabChoice.Of("Columns", ["1 column", "2 columns", "3 columns", "4 columns", "5 columns"], options.Columns - 1, index => Set(() => options.Columns = index + 1, LabViewChange.Grid)));

        string[] backdrops = hasPicture ? [.. _backdrops, "Stage picture"] : _backdrops;
        AddChild(LabChoice.Of("Backdrop", backdrops, (int)options.Backdrop, index => Set(() => options.Backdrop = (LabBackdrop)index, LabViewChange.Look)));
        AddChild(LabChoice.Of("Camera", ["Match camera", "Close-up", "Extreme close-up"], (int)options.Framing, index => Set(() => options.Framing = (LabFramingKind)index, LabViewChange.Look)));
        AddChild(new Label { Text = "Zoom" });
        HSlider zoom = new() { MinValue = 0.4, MaxValue = 3, Step = 0.05, Value = options.Zoom, CustomMinimumSize = new Vector2(110f, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = "Zoom on top of the camera choice" };
        zoom.ValueChanged += value => Set(() => options.Zoom = (float)value, LabViewChange.Look);
        AddChild(zoom);
        AddChild(LabChoice.Of("How the projectile ends", ["Impact: hit", "Impact: block", "Impact: clash", "Impact: fizzle"], (int)options.Impact, index => Set(() => options.Impact = (ImpactKind)index, LabViewChange.Look)));

        AddChild(LabChoice.Toggle("Mirror", "Cast to the left", options.Mirror, on => Set(() => options.Mirror = on, LabViewChange.Look)));
        AddChild(LabChoice.Toggle("Greyscale", "Does it read by brightness alone?", options.Greyscale, on => Set(() => options.Greyscale = on, LabViewChange.Look)));
        AddChild(LabChoice.Toggle("Super flash", "Darken the stage as a super's flash does", options.SuperFlash, on => Set(() => options.SuperFlash = on, LabViewChange.Look)));
        AddChild(LabChoice.Toggle("Fighters", "Stand-ins for the caster and target, their body box size", options.StandIns, on => Set(() => options.StandIns = on, LabViewChange.Look)));
        AddChild(LabChoice.Toggle("Hitbox", "The projectile's hitbox while it flies", options.Hitbox, on => Set(() => options.Hitbox = on, LabViewChange.Look)));
        AddChild(LabChoice.Toggle("Lock palette", "Every variant uses the character's colours", options.PaletteLock, on => Set(() => options.PaletteLock = on, LabViewChange.Values)));

        void Set(Action apply, LabViewChange change)
        {
            apply();
            changed(change);
        }
    }
}
