# Melon Auto Updater
![GitHub Release](https://img.shields.io/github/v/release/HAHOOS/MelonAutoUpdater?include_prereleases&sort=semver&display_name=tag&style=for-the-badge)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/HAHOOS/MelonAutoUpdater/total?style=for-the-badge)
![GitHub License](https://img.shields.io/github/license/HAHOOS/MelonAutoUpdater?style=for-the-badge)

Melon Auto Updater is a plugin for MelonLoader that automatically updates all of your mods!
If a mod has included a download link in the assembly, the updater will pick that up and use that to get the newest version and if needed, install it.
Currently supported links (this list will be expanded in the near future):
 - Thunderstore
 - Github

**This plugin is currently in an alpha release, currently it is only available on Github for testing purposes, as it is not ready for release yet** 

## Preferences

### Enabled
If true, the plugin will update the mods
### Ignore List
List of mods (file names without extension example ".dll") that will not be updated
### Priority List
List of mods (file names without extension example ".dll") that will be updated first, even if another mod has a set priority higher
### Brute Check
If enabled, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author
This is not recommended as it will very easily result in this plugin being rate-limited<br/>
**Github has a limit of 60 requests/hr, having brute check enabled will rate limit the plugin if the brute check was ran multiple times and/or you have a lot of mods**

## Licensing & Credits
MelonAutoUpdater (MAU) is licensed under the MIT License. See [LICENSE](https://github.com/HAHOOS/MelonAutoUpdater/blob/master/LICENSE.txt) for the full License.

Third-party Libraries used as Source Code and/or bundled in Binary Form:
- [MelonLoader](https://github.com/LavaGang/MelonLoader) is licensed under the Apache 2.0 License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md) for the full License.
- [Mono.Cecil](https://github.com/jbevain/cecil) is licensed under the MIT License. See [LICENSE](https://github.com/jbevain/cecil/blob/master/LICENSE.txt) for the full License.
- [Rackspace.Threading](https://github.com/tunnelvisionlabs/dotnet-threading) is licensed under the Apache 2.0 License. See [LICENSE](https://github.com/tunnelvisionlabs/dotnet-threading/blob/master/LICENSE) for the full License.
- [TinyJSON](https://github.com/pbhogan/TinyJSON) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/TinyJSON/LICENSE.md) for the full License.
- [Tomlet](https://github.com/SamboyCoding/Tomlet) is licensed under the MIT License. See [LICENSE](https://github.com/SamboyCoding/Tomlet/blob/master/LICENSE) for the full License.
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) is licensed under the MIT License. See [LICENSE](https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/SharpZipLib/LICENSE.txt) for the full License.
