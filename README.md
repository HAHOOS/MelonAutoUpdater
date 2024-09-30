<div align="center"><a href="https://github.com/HAHOOS/MelonAutoUpdater/wiki/Icon"><img src="https://github.com/HAHOOS/MelonAutoUpdater/blob/master/.github/Images/MAUIcon.png" /></a></div>

<h1 align="center">MAU | Melon Auto Updater</h1>

<p align="center">
<a href="https://github.com/HAHOOS/MelonAutoUpdater/releases/latest"><img src="https://img.shields.io/github/v/release/HAHOOS/MelonAutoUpdater?include_prereleases&sort=semver&display_name=tag&style=for-the-badge"></a>
<a href="https://github.com/HAHOOS/MelonAutoUpdater/releases/"><img src="https://img.shields.io/github/downloads/HAHOOS/MelonAutoUpdater/total?style=for-the-badge"></a>
<a href="https://github.com/HAHOOS/MelonAutoUpdater/blob/master/LICENSE.txt"><img src="https://img.shields.io/github/license/HAHOOS/MelonAutoUpdater?style=for-the-badge"></a>
<a href="https://github.com/LavaGang/MelonLoader/releases"><img src="https://img.shields.io/badge/ML_Support-v0.5.3_or_later-blue?style=for-the-badge&labelColor=gray&color=blue"></a>
</p>

Melon Auto Updater is a plugin for MelonLoader that automatically updates all of your mods!<br/>
If a mod has included a download link in the assembly, the updater will pick that up and use that to get the newest version and if needed, install it.<br/>
Currently supported links (this list will be expanded in the near future):<br/>
 - [Thunderstore](https://thunderstore.io)
 - [Github](https://github.com/)

Tested Games & Versions:
- BONELAB (ML v0.6.4 & v0.6.5)
- BONEWORKS (ML v0.5.4 & v0.5.3)


**This plugin is currently in an alpha release, currently it is only available on Github for testing purposes, as it is not ready for release yet. It would be really appreciated if you would help me find any bugs by creating an issue in the repository if you find one** 

<h2 align="center">Installation</h2>
To install the plugin, you obviously need to have MelonLoader installed in your game.<br/>
<br/>

When you've done that, simply put `MelonAutoUpdater.dll` to the `Plugins` folder

And now, you're done! Enjoy your mods being automatically updated! 🎉

<h2 align="center">Preferences</h2>

| Preference | Description |
| --- | --- |
| Enabled | If true, the plugin will update the mods |
| Ignore List | List of mods (file names without extension example ".dll") that will not be updated |
| Brute Check | If enabled, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author. **WARNING: You may get rate-limited with large amounts of mods/plugins, use with caution** |
| Dont Update | This will cause the plugin to only check for the latest version and notify you if there is a newer version you can install

<h2 align="center">Command Line Arguments</h2>

With MelonLoader v0.6.5, now mods & plugins can have their own command line arguments.

Below is a list of all available command line arguments for MelonAutoUpdater:

| Argument | Description |
| --- | --- |
| `--melonautoupdater.disable` | Disables the plugin from executing |
| `--melonautoupdater.debug` | Turns on DEBUG mode |
| `--melonautoupdater.dontupdate` | Disallows the plugin from updating when there's a newer version of a mod available, instead it will just tell you in the console |

<h2 align="center">For Developers</h2>

To implement auto-updating into your code mod, simply set a correct **download link** in the [MelonInfo Attribute](https://melonwiki.xyz/#/modders/attributes?id=meloninfo) and please make sure to use the [VerifyLoaderVersion Attribute](https://melonwiki.xyz/#/modders/attributes?id=verifyloaderversion) and specify on what versions of ML does the mod/plugin work


<h2 align="center">Licensing & Credits</h2>

MelonAutoUpdater (MAU) is licensed under the MIT License. See [LICENSE](https://github.com/HAHOOS/MelonAutoUpdater/blob/master/LICENSE.txt) for the full License.

Third-party Libraries used as Source Code and/or bundled in Binary Form:
- [MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under the Apache 2.0 License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md) for the full License.
- [Mono.Cecil](https://github.com/jbevain/cecil) is licensed under the MIT License. See [LICENSE](https://github.com/jbevain/cecil/blob/master/LICENSE.txt) for the full License.
- [TinyJSON](https://github.com/pbhogan/TinyJSON) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/TinyJSON/LICENSE.md) for the full License.
- [Tomlet](https://github.com/SamboyCoding/Tomlet) is licensed under the MIT License. See [LICENSE](https://github.com/SamboyCoding/Tomlet/blob/master/LICENSE) for the full License.
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/SharpZipLib/LICENSE.txt) for the full License.
- [Pastel](https://github.com/silkfire/Pastel) is licensed under the MIT License. See [LICENSE](https://github.com/silkfire/Pastel/blob/master/LICENSE) for the full License.

Assets from Third-party Libraries used as Source Code and/or bundled in Binary Form:
- [db.json](https://github.com/jshttp/mime-db/blob/master/db.json) from [mime-db](https://github.com/jshttp/mime-db) is licensed under the MIT License. See [LICENSE](https://github.com/jshttp/mime-db/blob/master/LICENSE) for the full License.

MelonAutoUpdater is not sponsored by, affiliated with or endorsed by Lava Gang or its affiliates.

"MelonLoader" is a trademark or a registered trademark of Lava Gang or its affiliates in the U.S. and elsewhere.
