# BG3ModSharer
The purpose of this app is to synchronize Baldur's Gate 3 mods between multiple systems.
I designed this to easily keep mods in sync and/or uninstalled across my PC and Steam Deck.
It could also serve to synchronize mods between multiple playerss for MP campaign purposes.

Supports installing/uninstalling all included mods at once only. Does not support individual modding.
Copies files from the "included" game/appdata folders to actual game/appdata folders or reverts them.

## Usage
Grab the latest [release](https://github.com/swg1251/BG3ModSharer/releases) and extract `Mods.7z` to the `Mods` folder in the same directory as BG3M.exe
This program requires certain mods to be downloaded. These files will be included
with each release. The directory structure after extracting will look like:
```
BG3M.exe
Mods
|---AppData
|   |---Mods
|       |  (your .pak files here)
|   |---PlayerProfiles
|       |---Public
|           | modsettings.lsx (your current mod list)
|           | modsettings_original.lsx (a copy of the unmodified file)
|---Game
    |---bin
        | bink2w64.dll (party limit begone mod)
        | bink2w64_original.dll (backup of original dll)
        | DWrite.dll (script extender)
        | ScriptExtenderSettings.json
    |---Data
        |---Mods
            | (folders/files from party limit begone mod here)
```
Once the above is ready:
- Windows - simply run BG3M.exe and follow the prompts.
- Steam Deck - use [Protontricks](https://github.com/Matoking/protontricks) to launch BG3M.exe in the Baldur's Gate 3 proton context
