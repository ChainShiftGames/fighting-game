# Vendored Steam libraries

From the official [Steamworks.NET 2025.164.1](https://github.com/rlabrecque/Steamworks.NET/releases/tag/2025.164.1)
release (`Steamworks.NET-Standalone_2025.164.1.zip`), built against Steamworks SDK 1.64. Why vendored: ADR-0001.

| File | Licence | SHA-256 |
|---|---|---|
| `win-x64/Steamworks.NET.dll` | MIT (`Steamworks.NET-LICENSE.txt`) | `88542b6163d3bf1597e80ed626e38d9d8ceecf7a363892017dfe158b3cc0a7e2` |
| `win-x64/steam_api64.dll` | Valve Steamworks SDK redistributable; signed by Valve Corp. | `eb17909a76668cf9ae0b92a618a34a50f6c73d3a6787cb4dd8ce36a8b10bfb75` |
| `linux-x64/Steamworks.NET.dll` | MIT (`Steamworks.NET-LICENSE.txt`) | `d35b612d99e7274f9dcaedd02da91c022db58cbaabd7c5333033f5b64ef79d9f` |
| `linux-x64/libsteam_api.so` | Valve Steamworks SDK redistributable | `ec4797f76a206eb0af627af0f0788eb5a2eabf5dee38ee0a6affbaa44a645f4e` |

- Upgrading: replace all four files from one release, update this table and ADR-0001 in the same PR.
- `Platform.Steam.csproj` picks the folder for the build's runtime (or the OS when there is none).
