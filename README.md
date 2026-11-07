> [!WARNING]
> **This tool was Vibecoded.** The code, tests and documentation were written by Claude
> (an AI model by Anthropic) and reviewed and tested only as described below. Use it at your own
> risk and keep backups of your saves.

# Azimuth Save Importer for Forza Horizon 3 (PC → Azimuth)

Brings your **Forza Horizon 3 PC (Microsoft Store / Xbox app) saves** into **Azimuth**: your
profile and progress, liveries, tuning setups, photos and garage thumbnails.

This is a fork of [spenaat/azimuth-save-importer](https://github.com/spenaat/azimuth-save-importer)
(the Forza Horizon 2 tool, MIT). The Azimuth side (commit manifests, format check, backups,
rollback) is unchanged. What is new is a reader for the PC save folder.

**Why this works:** FH3 is Xbox Play Anywhere, so the PC and Xbox One versions share one cloud save
and the save files are byte for byte the same. Only the way they are stored on disk differs:

- **PC** keeps them in `%LOCALAPPDATA%\Packages\Microsoft.OpusPG_8wekyb3d8bbwe\SystemAppData\wgs`,
  as GUID-named files listed by `containers.index` and `container.N` files.
- **Azimuth** keeps every container in `<name>\revisions\<n>\data\` plus a `commit` manifest.

The tool reads the first layout and writes the second. File contents are not changed.

## Download

Get `AzimuthSaveImporter.exe` from this repo's Releases (or the *Actions* build artifact).
It is a single file, nothing to install. Requires Windows 10 or 11.

## How to use it

1. **Play FH3 on PC once** (Microsoft Store / Xbox app version), so your cloud save is synced to the
   PC, and close it.
2. **Start FH3 in Azimuth once, let it save, and close it.** The importer checks its output against
   the saves Azimuth wrote itself before it changes anything, so it needs at least one.
3. **Run `AzimuthSaveImporter.exe`.**
   - *FH3 PC saves*: filled in automatically when the default folder exists. Otherwise pick the
     `wgs` folder above. If you have several Xbox accounts, pick the `<id>_<id>` folder of the one
     you want.
   - *Azimuth saves*: pick the folder Azimuth is installed in.
   - *Review* lists every item and what will happen to it.
4. **Click _Import into Azimuth_**, choose *replace* or *merge*, then start the game and check.

Xbox saves downloaded with [XBsync](https://github.com/d3fekt/xbsync) still work as a source too.

Backups go to `Documents\Azimuth Save Importer\Backups`. **Restore a backup...** puts any of them back.

## What it does

| FH3 PC save / XBsync download                  | Azimuth                                                       |
| ---------------------------------------------- | ------------------------------------------------------------- |
| `User_<XUID>,savedgame\*` (profile)            | `User_9000000000000\revisions\0000000000000001\data\*`, or the name of the profile Azimuth already has |
| `Livery_0289_20150618180709,savedgame\*`       | `Livery_0289_20150618180709\revisions\0000000000000001\data\*` |
| ... every other container the same way         |                                                               |
| `TransactionLogFile` (Xbox only)               | left out                                                      |

Every revision gets a `commit` file:

```
Azimuth complete save revision v2
blobs <count>
<sha256> <size> <name>        (one line per file, ordinal name order)
```

File contents are copied byte for byte; nothing inside the saves is modified.

## Safety

- **Format check.** Before importing, the tool regenerates the `commit` of every save Azimuth
  already wrote and compares it byte for byte. If any differs (for example because a newer
  Azimuth changed its format), it refuses to import and changes nothing.
- **Backup first.** The whole player folder is zipped, and the ZIP is checked file by file,
  before anything is replaced.
- **Staged and verified.** Everything is written to a temporary folder next to the save folder
  and verified, then moved into place and verified again.
- **Automatic rollback.** If anything fails while saves are being replaced, the backup is
  restored automatically.
- **The game should be closed.** The tool warns if it looks like the game is running.

## Known limitations

- The save format was worked out from saves Azimuth wrote, not from Azimuth's code. The format
  check above guards against changes, but treat this as a community tool and keep your backups.
- The FH3 PC reader was tested only against synthetic save folders built from the public description
  of the format, not against a real FH3 PC save. The Azimuth writer was tested by its author with
  Forza Horizon 2 only. Keep your backups, and please report what happens with FH3.
- PC containers that are listed but not downloaded yet are skipped with a warning. A container with a
  missing file is refused outright, so a half profile is never imported.
- Liveries, tunes and photos keep the Xbox owner (your gamertag) in their headers; they are not
  rewritten. If an item does not show up in the game, that is the most likely reason. Please open
  an issue.
- Photos are numbered (`Photo_0000_...`). If Azimuth already has a photo with the same number,
  choose *replace everything* or delete that photo, or one of them may not show up.

## Command line

The same exe works from a terminal:

```
AzimuthSaveImporter import  --source <folder|zip> --target <folder> [--mode replace|merge] [--dry-run] [--yes]
AzimuthSaveImporter verify  --target <folder>
AzimuthSaveImporter restore --backup <zip> --target <folder> [--yes]
AzimuthSaveImporter --help
```

## Building

The release build uses Mono's C# compiler, so it builds on Linux (this is what CI does):

```
sudo apt-get install mono-devel
./build.sh                # dist/AzimuthSaveImporter.exe
./tests/run-tests.sh      # end-to-end tests of the command line
```

On Windows you can also open `AzimuthSaveImporter.csproj` in Visual Studio 2022 or run
`dotnet build -c Release`.

Pushing a tag such as `v1.0.0` builds the exe and attaches it to a GitHub release.

## Credits and license

- Based on [azimuth-save-importer](https://github.com/spenaat/azimuth-save-importer) by spenaat (MIT).
- PC save layout from [XGP-save-extractor](https://github.com/Z1ni/XGP-save-extractor) by Z1ni (MIT).
- [XBsync](https://github.com/d3fekt/xbsync) by d3fekt does the Xbox download. It is a separate
  project (GPL-3.0); no code from it is included here.
- Not affiliated with Microsoft, Xbox, Playground Games or the Azimuth project.

MIT License, see [LICENSE](LICENSE).
