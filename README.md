# Maker and Studio File Browser Folders for games by Illusion
A BepInEx plugin for Koikatu, Koikatsu Party, EmotionCreators, AI-Shoujo and HoneySelect2 that adds subfolder support to main game and studio. It works with character, outfit(coordinate) and scene file browsers. You can organize your cards and scenes into folders and use this plugin to browse them (features vary between different games).

![folders preview](https://user-images.githubusercontent.com/39247311/77538502-230b7e80-6ea0-11ea-8722-79f93138e2f6.PNG)

You can support development of my plugins through my Patreon page: https://www.patreon.com/ManlyMarco

## How to use
1. Make sure the latest version of [Illusion ModdingAPI](https://github.com/IllusionMods/IllusionModdingAPI) for your game is installed (with all of its prerequisites) and that your game is fully updated.
2. Download the latest release from [here](https://github.com/ManlyMarco/Illusion_BrowserFolders/releases).
3. Extract the plugin into your game directory. The dll file(s) should end up inside the folder `BepInEx\plugins`.
4. Start Studio and open the scene load window. You should see a new window next to the standard file list. Click on the folder names in the list to open contents of that folder.

#### Incompatibilities
- Remove `KKSceneBrowserFolders.dll` from `BepInEx` if you have it, as it's an old version of this mod.
- The KKS version is incompatible with `KKS_StudioDefaultData`. You must remove `KKS_StudioDefaultData.dll` from your plugins folder or BrowserFolders will not work.
- There is a mild incompatibility with `KK_ReloadCharaListOnChange v1.2 and older`; check for an updated version if you use this plugin.

## How to compile
1. Clone this repository.
2. Open the solution in Visual Studio 2022.
3. Make sure you have the .NET Framework 3.5 and 4.6 development tools installed (via Visual Studio Installer).
4. Build the solution. It should succeed without errors. Build outputs will be in the `bin` folder.

#### How to contribute
Feel free to fork the repository and submit pull requests. If you find any bugs report them in the Issues section (a PR with a fix would be ideal though).
