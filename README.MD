# Boxroom Studio

A companion application for **BOXROOM** that makes creating, editing,
and managing games in your BOXROOM library easy.

Whether you're adding custom games, updating metadata, downloading
artwork, configuring Boxroom Plus features, or recovering from cache
issues, Boxroom Studio provides a simple interface for managing your
library.

![Mario running in BOXROOM](DOCS/image.png)

------------------------------------------------------------------------

## Features

- 🎮 Create and edit custom games
- ✏️ Edit existing Steam game metadata *(optional)*
- 🖼 Download metadata directly from IGDB
- 📷 Download up to 3 screenshots automatically
- 🖼 Browse and apply SteamGridDB artwork
- 🚀 Configure custom launch settings *(requires Boxroom Plus)*
- 🎨 Configure custom case colors *(requires Boxroom Plus)*
- 🏷 Configure application platforms *(Steam, GOG, Epic, EA, Ubisoft, itch.io, Emulator, ROM, Custom)*
- 📁 Browse for executables, working directories, and cover art
- 🔄 Sync custom games back into `owned_games.json`
- 💾 Automatic metadata backup and recovery
- 🔧 Automatic legacy metadata migration
- ⚙️ First-run setup wizard
- 🌙 Dark and Light themes
- 🔄 Built-in automatic updater

------------------------------------------------------------------------

## Requirements

- Windows or Linux
- BOXROOM installed
- IGDB API credentials *(optional but recommended)*
- SteamGridDB API key *(optional)*

Without API keys you can still create and edit games manually.

------------------------------------------------------------------------

## First Launch

On first launch you'll be asked to configure:

- BOXROOM Cache Folder
- IGDB Client ID / Secret
- SteamGridDB API Key
- Theme

The application also allows testing both API connections before saving.

------------------------------------------------------------------------

## Editing Steam Games

By default, Boxroom Studio focuses on custom games.

If you would also like to edit existing Steam games, enable:

**Settings → Show Steam Games in Editor**

This allows artwork, metadata, and screenshots to be updated for your
existing Steam library.

------------------------------------------------------------------------

## Boxroom Plus Integration

> **Requires Boxroom Plus**

Boxroom Studio includes native support for configuring Boxroom Plus
features.

Supported options include:

- Custom launch settings
- Platform selection
- Custom case colors
- Automatic generation of `meta.helper.json`

These settings are ignored by the base version of BOXROOM and are only
used when Boxroom Plus is installed.

GitHub:

https://github.com/MidgetBrony/Boxroom-Plus

------------------------------------------------------------------------

## Sync owned_games.json

BOXROOM may regenerate `owned_games.json`.

If your custom games disappear from your shelf, simply click:

**Sync owned_games.json**

This safely re-adds every custom AppID without modifying your Steam
library.

------------------------------------------------------------------------

## Automatic Metadata Backup

Boxroom Studio automatically creates a backup of every `meta.json` it
writes.

If an older version of BOXROOM removes a game's metadata during a cache
refresh, Studio will automatically restore it the next time the cache is
loaded.

Older metadata formats are also automatically upgraded when loaded to
maintain compatibility with newer releases.

------------------------------------------------------------------------

## Screenshots

Supports downloading screenshots directly from IGDB.

- Downloads up to **3 screenshots**
- Automatically replaces old screenshots
- Uses **720p** images

------------------------------------------------------------------------

## Linux Notes

### Updater

On some Linux desktop environments (such as KDE Plasma), the updater may
fail to launch the first time with an error similar to:

> "For security reasons, launching executables is not allowed in this context."

This is caused by the desktop environment marking the downloaded updater
as untrusted.

Simply launch **boxroom-studio-updater** manually once and, if prompted,
mark it as **Trusted**.

After the initial launch, automatic updates will work normally.

### Launching Games

When using **BOXROOM** through **Proton**, launching native Linux
executables directly from Boxroom Plus may not work.

The recommended workaround is to:

1. Add the application to Steam as a **Non-Steam Game**.
2. Create a desktop shortcut from Steam.
3. Open the generated `.desktop` file in a text editor.
4. Copy the generated launch command into `launch.json`.

Steam will then handle launching the native application correctly.

------------------------------------------------------------------------

## Known Limitations

- Boxroom Plus features require **Boxroom Plus**
- IGDB and SteamGridDB require personal API keys
- Up to **3 screenshots** are downloaded per game
- Custom AppIDs begin at **900000000** to avoid conflicts
- Native Linux launching depends on Proton limitations

------------------------------------------------------------------------

## Credits

- NestedLoop Studios — BOXROOM
- IGDB
- SteamGridDB

------------------------------------------------------------------------

## License

MIT
