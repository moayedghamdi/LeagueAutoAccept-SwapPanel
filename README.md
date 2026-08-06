<div align="center">

# LeagueAutoAccept Swap Panel

**A lightweight League of Legends auto-accept utility with champion-select swap controls.**

[![Latest release](https://img.shields.io/github/v/release/moayedghamdi/LeagueAutoAccept-SwapPanel?style=for-the-badge)](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest)
[![Download](https://img.shields.io/github/downloads/moayedghamdi/LeagueAutoAccept-SwapPanel/total?style=for-the-badge&label=downloads)](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest)
[![License](https://img.shields.io/github/license/moayedghamdi/LeagueAutoAccept-SwapPanel?style=for-the-badge)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=for-the-badge&logo=windows)](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest)

[**Download LeagueAutoAccept.exe — no installation required**](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest/download/LeagueAutoAccept.exe)

</div>

> [!IMPORTANT]
> This repository is a fork of [sweetriverfish/LeagueAutoAccept](https://github.com/sweetriverfish/LeagueAutoAccept). The original application, architecture, and auto-accept functionality were created by its upstream authors. This fork builds on that work with a champion-select swap panel, pick-order swapping, position swapping, champion trading controls, and self-contained Windows releases.

## What this fork adds

| Addition | What it does |
| --- | --- |
| Champion-select swap panel | Displays your four allies, assigned roles, pick order, and selected champions. |
| Pick-order swaps | Requests a draft-order swap through League's local client API. |
| Position swaps | Requests an assigned-role swap with an eligible teammate. |
| Champion swaps | Requests a champion trade when League exposes an eligible trade contract. |
| Safe sequential requests | Processes selected teammates one at a time and stops after an accepted swap. |
| Live status | Shows connection, champion-select, pending-request, timeout, and error states. |
| League Classic labels | Marks Classic champion entries while preserving the correct pick and ban IDs. |
| Swiftplay champion selection | Applies your primary and secondary configured champions to Swiftplay's two position slots. |
| Swiftplay summoner spells | Configures a separate spell pair for each of your two selected Swiftplay positions. |
| Self-contained release | Download and run one executable; no .NET installation or source build is required. |

All original features remain available, including automatic ready-check acceptance, champion selection, bans, rune pages, summoner spells, lobby chat messages, and queue options.

For Swiftplay, choose your two positions in League's lobby and configure the app's primary and secondary champions. Open **Swiftplay summoner spells** on the main screen to choose a separate spell pair for each position. While auto accept is enabled, the app assigns the primary settings to the first-position slot and the secondary settings to the second-position slot. Swiftplay perks remain unchanged, and any spell left **Unselected** is preserved from League.

## Screenshot

<div align="center">
  <img src="screenshot.png" alt="LeagueAutoAccept console interface" width="760">
</div>

## Download and run

1. Download [`LeagueAutoAccept.exe`](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest/download/LeagueAutoAccept.exe).
2. Start the League of Legends client.
3. Run `LeagueAutoAccept.exe`.

The Windows x64 executable is self-contained, including its logging configuration. You do **not** need to install .NET, Visual Studio, Git, or build the source. A [ZIP package](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel/releases/latest/download/LeagueAutoAccept-win-x64.zip) containing the same standalone executable is also available.

Windows SmartScreen may warn about an unsigned community executable. The complete source and repeatable publishing script are included in this repository so the build can be inspected or reproduced.

## Using the swap panel

1. Enter champion select.
2. Open **Swap Panel** from the application's main screen.
3. Select a teammate.
4. Choose one of the available actions:

   - **Request Pick-Order Swap** — exchange draft positions.
   - **Request Position Swap** — exchange assigned roles.
   - **Request Champion Swap** — trade locked champions when League permits it.

The application reads swap eligibility directly from the active champion-select session. An action remains disabled when League does not expose a valid swap contract.

## How it works

The application reads League's local lockfile, authenticates against the local League Client API (LCU), and monitors gameflow and champion-select state. Swap requests are sent only to the locally running League client.

- No cloud service is used.
- No League credentials are uploaded.
- Lockfile passwords and authorization headers are not logged.
- Ranked players whom League anonymizes remain anonymous.

## Limitations

- Windows x64 only.
- League client updates can change or remove local API behavior.
- Swap buttons are available only in queues and phases where League exposes the corresponding action.
- Champion trades require both players and champions to satisfy League's normal eligibility rules.
- This project is not endorsed by Riot Games.

Use of unofficial local client APIs may be subject to League patch changes or regional rules. Use the application at your own risk.

## Build from source

Requirements: Windows and the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```powershell
git clone https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel.git
cd LeagueAutoAccept-SwapPanel
dotnet restore ".\Leauge Auto Accept.sln"
dotnet build ".\Leauge Auto Accept.sln" -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1
```

The publishing script creates both `artifacts\LeagueAutoAccept.exe` and `artifacts\LeagueAutoAccept-win-x64.zip`.

## Credits and license

- Original project: [sweetriverfish/LeagueAutoAccept](https://github.com/sweetriverfish/LeagueAutoAccept)
- Original copyright notice and contributor history are preserved in the repository.
- Fork additions are maintained at [moayedghamdi/LeagueAutoAccept-SwapPanel](https://github.com/moayedghamdi/LeagueAutoAccept-SwapPanel).

Distributed under the [MIT License](LICENSE).
