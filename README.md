
<h1 align="center">MAU | Melon Auto Updater</h1>

<p align="center">
<a href="github.com/HAHOOS/MelonAutoUpdater/releases/latest"><img src="https://img.shields.io/github/v/release/HAHOOS/MelonAutoUpdater?include_prereleases&sort=semver&display_name=tag&style=for-the-badge"></a>
<a href="github.com/HAHOOS/MelonAutoUpdater/releases/"><img src="https://img.shields.io/github/downloads/HAHOOS/MelonAutoUpdater/total?style=for-the-badge"></a>
<a href="https://github.com/HAHOOS/MelonAutoUpdater/blob/master/LICENSE.txt"><img src="https://img.shields.io/github/license/HAHOOS/MelonAutoUpdater?style=for-the-badge"></a>
<a href="https://github.com/LavaGang/MelonLoader/releases"><img src="https://img.shields.io/badge/ML_Support-v0.5.2_or_later-blue?style=for-the-badge&labelColor=gray&color=blue"></a>
</p>

Melon Auto Updater is a plugin for MelonLoader that automatically updates all of your mods!
If a mod has included a download link in the assembly, the updater will pick that up and use that to get the newest version and if needed, install it.
Currently supported links (this list will be expanded in the near future):
 - [Thunderstore](https://thunderstore.io)
 - [Github](https://github.com/)

**This plugin is currently in an alpha release, currently it is only available on Github for testing purposes, as it is not ready for release yet** 

<h2 align="center">Preferences</h2>

| Preference | Description |
| --- | --- |
| Enabled | If true, the plugin will update the mods |
| Ignore List | List of mods (file names without extension example ".dll") that will not be updated |
| Brute Check | If enabled, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author. **Currently Github is not supported in brute checking due to extremely strict rate limits** |

<h2 align="center">For Developers</h2>

To implement auto-updating into your code mod, simply set a correct **download link** in the [MelonInfo Attribute](https://melonwiki.xyz/#/modders/attributes?id=meloninfo) and please make sure to use the [VerifyLoaderVersion Attribute](https://melonwiki.xyz/#/modders/attributes?id=verifyloaderversion) and specify on what versions of ML does the mod/plugin work

<h2 align="center">Licensing & Credits</h2>

MelonAutoUpdater (MAU) is licensed under the MIT License. See [LICENSE](https://github.com/HAHOOS/MelonAutoUpdater/blob/master/LICENSE.txt) for the full License.

Third-party Libraries used as Source Code and/or bundled in Binary Form:
- [MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under the Apache 2.0 License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md) for the full License.
- [Mono.Cecil](https://github.com/jbevain/cecil) is licensed under the MIT License. See [LICENSE](https://github.com/jbevain/cecil/blob/master/LICENSE.txt) for the full License.
- [Rackspace.Threading](https://github.com/tunnelvisionlabs/dotnet-threading) is licensed under the Apache 2.0 License. See [LICENSE](https://github.com/tunnelvisionlabs/dotnet-threading/blob/master/LICENSE) for the full License.
- [TinyJSON](https://github.com/pbhogan/TinyJSON) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/TinyJSON/LICENSE.md) for the full License.
- [Tomlet](https://github.com/SamboyCoding/Tomlet) is licensed under the MIT License. See [LICENSE](https://github.com/SamboyCoding/Tomlet/blob/master/LICENSE) for the full License.
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/SharpZipLib/LICENSE.txt) for the full License.
