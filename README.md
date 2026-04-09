## Description
Updated version of @zombiebook's 3rd-person compass mod for Escape from Duckov
* Translated UI from Korean to English
* Added UI draw optimisations
* Automated creation\update of mod subfolder after compilation
* New info and preview picture required for ingame mod manasger
* The compass will not shown if  inventory or map is open, or camera mod not loaded\switched to vanilla camera view.

## Dependencies
>[!IMPORTANT]
>This mod required 3rd-person camera mod: **TPS Shoulder Surfing** ([Github](https://github.com/scudrt/Duckov_ShoulderSurfing) or [SteamWorkshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3597405574)) and must be loaded **AFTER** it

## Installation

### From sources
If you want to compile from sources, first thing you need is Microsoft [Visual Studio](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&channel=Stable). Download it and install, then download mod sources, unpack zip archive and open `compass.csproj` with any text editor, even standard Notepad is fine. Find line `<DuckovPath>` near the head of the file and change path to the one where your game is installed. For example, its `C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov` for the Steam version. Then open `compass.csproj` with Visual Studio, then select `Release` in top-down menu and press a green :arrow_forward: icon for build. After build check if exist folder `Duckov_Data\Mods\TPSCompass`, if yes, then everything went well, run game and check mod load order & activate your newly compiled mod.

### Manual
If you choose manual installation, move to the folder where game is installed. Download archive with pre-compiled version from [releases page](https://github.com/apple-pony/compass/releases/latest) then unzip its contents to the `Duckov_Data\Mods`. Run game and check mod load order & activate your newly installed mod.
