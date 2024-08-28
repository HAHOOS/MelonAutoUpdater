# Melon Auto Updater
Melon Auto Updater is a plugin for MelonLoader that automatically updates all of your mods!
If a mod has included a download link in the assembly, the updater will pick that up and use that to get the newest version and if needed, install it.
Currently supported links (this list will be expanded in the near future):
 - Thunderstore
 - Github

**This plugin is currently in an alpha release, it lacks certain required features and compability with older versions of MelonLoader (is known to not work with MelonLoader v0.5.7).** 

## Preferences

### Enabled
If true, the plugin will update the mods
### Ignore List
List of mods (file names without extension example ".dll") that will not be updated
### Priority List
List of mods (file names without extension example ".dll") that will be updated first, even if another mod has a set priority higher
### Brute Check
If enabled, when there's no download link provided with mod/plugin, it will check every supported platform providing the Name & Author
This is not recommended as it will very easily result in this plugin being rate-limited
