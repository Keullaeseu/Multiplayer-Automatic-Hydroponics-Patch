# Multiplayer Automatic Hydroponics Patch

A RimWorld Multiplayer compatibility patch for [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718).

This mod is designed to improve multiplayer synchronization when playing with the [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718) mod and RimWorld Multiplayer.

## Features

- Adds multiplayer compatibility for [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718).
- The mod itself needs no sync of its own (XML defs plus a render-only visual patch). Processor interactions (add, reorder, suspend, target counts including Do Forever, copy and paste processes) and pipe net interactions are synced by the required Multiplayer Vanilla Expanded Framework Patch.

## Requirements

- RimWorld
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- RimWorld Multiplayer
  - [GitHub version](https://github.com/rwmt/Multiplayer) or [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2606448745) version
- [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013)
- [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718)
- [Multiplayer Vanilla Expanded Framework Patch](https://github.com/Keullaeseu/Multiplayer-Vanilla-Expanded-Framework-Patch/releases/latest)

The host and every connected player must use compatible versions of all required mods.

## Installation

### Steam Workshop

Subscribe to the required mods and add them to your RimWorld mod list in the following order:

1. Harmony
2. Core
3. Royalty, Ideology, Biotech, and Anomaly, if applicable
4. RimWorld Multiplayer
5. [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013)
6. [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718)
7. [Multiplayer Vanilla Expanded Framework Patch](https://github.com/Keullaeseu/Multiplayer-Vanilla-Expanded-Framework-Patch/releases/latest)
8. [Multiplayer Automatic Hydroponics Patch](https://github.com/Keullaeseu/Multiplayer-Automatic-Hydroponics-Patch/releases/latest)

The patch should load after both RimWorld Multiplayer and [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718).

### Manual Installation

1. Download the latest release from the [**Releases**](https://github.com/Keullaeseu/Multiplayer-Automatic-Hydroponics-Patch/releases/latest) section.
2. Extract the mod folder into your RimWorld `Mods` directory.
3. Enable the required mods in RimWorld.
4. Use the recommended load order listed above.
5. Make sure every multiplayer player has the same mod list, configuration, and load order.

## Multiplayer Usage

All players should have the following mods installed and enabled:

- RimWorld Multiplayer
- [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013)
- [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718)
- [Multiplayer Vanilla Expanded Framework Patch](https://github.com/Keullaeseu/Multiplayer-Vanilla-Expanded-Framework-Patch/releases/latest)
- [Multiplayer Automatic Hydroponics Patch](https://github.com/Keullaeseu/Multiplayer-Automatic-Hydroponics-Patch/releases/latest)
- All required Automatic Hydroponics dependencies

The host and all connected clients should use the same:

- RimWorld version
- RimWorld Multiplayer version
- Automatic Hydroponics version
- Multiplayer Automatic Hydroponics Patch version
- Mod configuration
- Mod load order

Do not add, remove, update, or reorder mods while players are connected to the same multiplayer session.

Copy and paste of processes is per-client and per-session: press Copy on a hydroponic first, then Paste on the same size (large to large, small to small) on the same instance.

## Compatibility

This patch is intended to provide multiplayer compatibility for [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718).

It does not replace:

- [RimWorld Multiplayer](https://steamcommunity.com/sharedfiles/filedetails/?id=2606448745)
- [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718)

## Known Limitations

- Compatibility may be affected by future RimWorld updates.
- Compatibility may be affected by future updates to RimWorld Multiplayer or Automatic Hydroponics.

## Credits

- [RimWorld Multiplayer on GitHub](https://github.com/rwmt/Multiplayer)
- [RimWorld Multiplayer on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2606448745)
- [Automatic Hydroponics on Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718)
- [Multiplayer Automatic Hydroponics Patch](https://github.com/Keullaeseu)
