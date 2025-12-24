## Mod Overview and Installation Methods

### Mod Overview

The vanilla game only saves limited statistics (overview page), while this mod precisely records the complete combat log of every raid, including:

**Kills**
> Use `info index [raid_index_to_view]` to view.
- The specific weapon used.
- Hit location (head, chest, limbs, etc.).
- Type of target killed (PMC, Scav, Boss, Scav Boss, Boss guard, etc.).
- Time the target was killed.

**Loot**
- List of items brought into the raid and items extracted from the raid.
  > Use `items index [raid_index_to_view] mode all` to view.
- Newly found loot in the raid, lost loot, changed loot (increased bullet count, decreased durability of meds/weapons, etc.).
  > Use `items index [raid_index_to_view] (mode change)` (parentheses and contents can be omitted) to view.

**Raid Profit/Loss**
> Use `info index [raid_index_to_view]` to view.
- Raid map information, raid duration.
- Upon raid entry:
    - Kit value (similar to "Delta Force", calculates the sum value of weapons, rig, backpack, armor, etc.).
    - Secure container value (total value of all items in the secure container. In many cases, this constitutes a significant portion) (0 for Scav runs).
    - Total brought-in value (equals Kit value + Secure container value + value of items in backpack, rig, pockets, and special slots).
- Upon leaving the raid:
    - Total extracted value (gross profit).
    - Losses (consumed items, discarded items, durability loss, etc.).
    - Net profit (Gross profit - Losses).
- Raid result:
  ```csharp
  // Theoretically includes these:
  public enum ExitStatus
  {
    SURVIVED, // Survived
    KILLED, // Killed
    LEFT, // Left raid (refers to the option after pressing Esc in-client)
    RUNNER, // Ran Through
    MISSINGINACTION, // Missing In Action
    TRANSIT, // Transfer (e.g., Car extract?)
  }
  ```
- If Survived/Transfer/Ran Through: Extraction point information, playstyle information.
- Other cases: Information on which faction and which enemy (name unimportant except for bosses) eliminated you, using what weapon, hitting which limb.

**Pricing**
> Use `price name "[item_name_keyword]"` (if the keyword contains no spaces, line breaks, etc., the double quotes can be omitted) to view.
> This feature leans towards debugging, mainly used to verify if the mod's calculated prices are correct. It can also be used for fuzzy searching of item names and IDs related to the keyword.

**Unexpected Situation Handling**
- The mod records data on the server-side. If the server crashes for reasons unrelated to this mod at the end of a raid, there is still a chance the raid data will be correctly recorded upon server restart.
- If you Alt+F4 or the client crashes after a raid starts, the next time you launch into a raid will cause the cached result of that previous raid to be recorded as an unknown outcome.

### Installation Method

Installation is simple. Just extract the .7z file to your SPT game root directory.
After installation, your file structure should look like this:
```
Your SPT Game Root Directory\SPT\user\mods\RaidRecord\(any mod files)
```

### Credits && Acknowledgments

- Thanks to the SPT team for the framework and documentation.
- Thanks to DrakiaXYZ, HiddenCirno, jbs4bmx, GhostFenix̵̮̀x̴̹̃©, Dsnyder | WTT, and all other developers in the community who share experience, code, and patiently answer questions.
- Thank you for downloading and trying this mod.

## Configuration, Database, Command System, and Localization Methods

### Configuration

1. Go to …/SPT/user/mods/RaidRecord/db and open "config.json".
2. Set `local` to the two-letter name of a translation file existing in the db/locals folder, e.g., "cn".
   > If changing the language has no effect, try clearing the local cache in the Launcher before starting the client.
3. Set `logPath` to change the output directory for some logs within the mod (to avoid cluttering the main SPT server log with mod error messages).
4. Set `autoUnloadOtherLanguages` to enable the multilingual optimization feature introduced in `0.6.4`.
5. Set `priceCacheUpdateMinTime` to change the update interval for the mod's price cache. This setting does not affect the `price` command, which always fetches the price calculated by the mod at that moment.

### Database Explanation

> …/SPT/user/mods/RaidRecord/db folder

**~0.6.1**
The 'locals' folder contains translation files.
The 'records' folder (created after running the mod) contains record files for different profiles.
config.json stores mod configuration.

For installation and database compatibility, refer to the detailed information in the mod's **Version** section.

### Command System

> All commands and parameters are case-insensitive, but it's recommended to use lowercase letters.

After entering a raid, find a contact named **Raid Record Manager** and send `help` to get help information for the commands.

The current version supports the following commands:
- help: Get help information for all commands.
- cls: Clear the dialog chat history (recommended to use frequently).
- info: Get profit/loss, kill info, etc. for a specified raid.
- items: Get item changes or brought-in/extracted item lists for a specified raid.
- list: List all existing raid records. Higher page numbers correspond to newer raids; use the `limit` parameter to adjust the number displayed per page.
- price: Get the value of a specified item, or perform a fuzzy search for multiple item values by name.

### Localization Method

**AI-Assisted Translation**
1. Make a copy of `ch.json` or `en.json`, rename it to the corresponding two-letter language code, e.g., "cz.json" (best to correspond with the names in Game Folder\SPT\SPT_Data\database\locales\global\**.json).
2. Send the translation file to an AI, instructing it to preserve the format while translating.
    - If the AI modifies any translation keys, stop it and emphasize in the prompt that the AI should only translate the JSON *values*.
    - If the AI modifies anything within `{{}}`, stop it and emphasize in the prompt that translating content inside `{{}}` is prohibited.
3. Verify the translation results are correct. The above steps should handle about 70% of the translation.
4. Check if the variables in the columns under the `"translations"` key correspond to their names, to prevent the AI from altering sentence structure (changing the order of column names along with the table names is acceptable).
5. Check for missing `\n` characters.

**Manual Translation**
1. You can find translations in `Game Folder\SPT\SPT_Data\database\locales\global\**.json`.
    - "roleNames": Search for `BotRole` and `ScavRole`.
    - "armorZone": Search for `DeathInfo`, `Collider Type` (recommended), `Armor Zone`, `HeadSegment`.
2. If you urgently need to use commands, prioritize translating the values under "translations".
3. If you urgently need to view logs output by the server, prioritize translating the values under "serverMessage".

## Schematic Diagram of Command Usage

```markdown
### Example Images

#### Load mod
![Load mod](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/modLoad_en.png?raw=true)

#### cls cmd
![Use cls cmd](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/cls_en.png?raw=true)

#### list cmd
![Use list cmd](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/list_en.png?raw=true)

#### items cmd
![Use items cmd](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/items_en.png?raw=true)

#### info cmd
![Use info cmd](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/info_en.png?raw=true)

![Use info cmd too](https://github.com/SunYanbox/RaidRecord-ImageHosting/blob/main/info_kill_en.png?raw=true)
> The previous image did not eliminate any bots; here’s an additional one as a supplement.
```