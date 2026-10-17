# DesyncDiff

Compares the two dumps of one desync (one from each PC) and names the state fields that differ (ADR-0005 §5).

```
dotnet run --project tools/DesyncDiff -- <dump from P1>.json <dump from P2>.json
```

- Dumps are written by `Netcode.Desync.DesyncMonitor` to the `desyncs/` folder (developer setting
  `DesyncDumpSettings.Folder`; the game uses `user://desyncs`), named `desync-<utc time>-frame<N>-p<1|2>.json`.
  Online, both PCs write one (ADR-0019).
- Output: header warnings (different builds or data), then each frame both dumps hold (last agreed, first
  mismatched) with every differing field as `Fighters[1].Health: A=9400 B=9401`, then any frames whose confirmed
  inputs differ (an input sync bug rather than a logic one).
- Exit code 0 once compared, 1 on bad arguments or unreadable files.
