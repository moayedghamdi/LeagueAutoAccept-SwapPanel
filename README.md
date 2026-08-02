# League Auto Accept + Champion Select Swap Panel

A lightweight Windows utility for the League of Legends client. It connects only to the local League Client API (LCU) and keeps the original auto-accept, champion selection, rune, spell, chat, and queue features while adding a compact champion-select swap panel.

This project is based on [sweetriverfish/LeagueAutoAccept](https://github.com/sweetriverfish/LeagueAutoAccept) and remains available under the MIT License.

## Download and run

1. Open this repository's **Releases** page.
2. Download `LeagueAutoAccept-win-x64.zip` from the latest release.
3. Extract the ZIP.
4. Start League of Legends.
5. Run `LeagueAutoAccept.exe`.

The release is self-contained for Windows x64. A separate .NET installation is not required.

## Features

- Automatically accept ready checks.
- Select, hover, and lock champions and bans.
- Select rune pages and summoner spells.
- Send configured champion-select chat messages.
- Champion-select teammate panel with roles and selected champions.
- Request pick-order, position, and champion swaps when League reports them as available.
- Select eligible teammates and process swap requests sequentially.
- Cancel an active swap sequence.
- Optional target auto-dodge in lobbies where League legitimately exposes the configured Riot ID.

## Swap panel

Open **Swap Panel** from the main screen after entering champion select. Select a teammate, then choose the appropriate request:

- **Pick-Order Swap** changes draft order.
- **Position Swap** exchanges assigned roles.
- **Champion Swap** is available during phases where League exposes a valid trade.

Availability comes directly from the current champion-select session. Buttons remain disabled when League does not expose an eligible swap contract.

## Privacy and security

- All LCU requests stay on the local machine.
- The application does not upload League credentials or champion-select data.
- Lockfile passwords and authorization headers are not logged.
- Ranked teammates whose identities League anonymizes remain anonymous.
- This application is not endorsed by Riot Games.

Use of unofficial local client APIs may be affected by League patches or regional rules. Use the application at your own risk.

## Build from source

Requirements: Windows and the .NET 9 SDK.

```powershell
dotnet restore ".\Leauge Auto Accept.sln"
dotnet build ".\Leauge Auto Accept.sln" -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

The publish script creates `artifacts\LeagueAutoAccept-win-x64.zip`.

## License

Distributed under the [MIT License](LICENSE).
