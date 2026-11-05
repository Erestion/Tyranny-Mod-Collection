# Tyranny Gameplay & QoL Mod Collection by Enryu

A modular collection of Quality-of-Life (QoL), combat rebalance, and gameplay enhancement mods for *Tyranny*. Each module works independently and can be configured or toggled individually.

## Included Modules

### 1. AutoLooter
- **Description:** Automatically loots containers and fallen enemies near your party directly into your stash without opening loot windows or interrupting exploration.
- **Features:**
  * Configurable loot radius around your character.
  * Ignores trapped containers until safely disarmed.
  * Pauses during combat, cutscenes, and loading screens.
  * Cleans up empty bodies by removing their highlight clutter.

### 2. FloatingXP
- **Description:** Displays animated yellow floating text above character models whenever experience (XP) is gained.
- **Features:**
  * Real-time visual feedback for party progression during combat and questing.
  * Fully customizable text format (e.g., `+50 XP`).

### 3. GlobalSpeedRework
- **Description:** Overhauls combat pacing by dynamically scaling attack and spell casting speeds based on character attributes and skills.
- **Features:**
  * **Skill Scaling:** Grants **+2.5% attack/casting speed** per 10 points in the matching weapon or magic skill.
  * **Mage Scaling:** Spells and Songs scale speed off elemental magic skills or **50% of Lore** (whichever is higher).
  * **Quickness Bonus:** Adds **+1% speed** per point of Quickness above 10 (total speed multiplier capped at 1.0x–2.2x).
  * Enforces a safe recovery time floor to prevent instant attack loops.

### 4. NoPerRestAbilities
- **Description:** Converts all rest-restricted abilities into per-encounter skills.
- **Features:**
  * Automatically resets ability charges and cooldowns at the end of every combat encounter.
  * Eliminates the need for frequent resting during dungeon exploration.

### 5. SmallPartyXp
- **Description:** Rebalances experience gains when playing with solo or small party compositions.
- **Features:**
  * Grants bonus XP for completing quest objectives and registering bestiary kills when running with fewer than 4 companions.
  * Default bonus is **+10% XP per missing slot** (up to +30% for solo play, configurable up to 50%).
  * Prints green notifications in the developer console when bonus XP is awarded.

### 6. SpeedOptions
- **Description:** Provides hotkeys and an interactive on-screen menu to adjust game speed on the fly.
- **Features:**
  * Draggable OSD window with a real-time speed slider and quick presets (`1.0x`, `1.5x`, `2.0x`, `Max`).
  * Automatically preserves game pause so combat pausing functions normally.
- **Default Hotkeys:**
  * `Keypad +` : Increase game speed (by 0.25x steps)
  * `Keypad -` : Decrease game speed (by 0.25x steps)
  * `Keypad *` : Toggle instantly between normal speed (`1.0x`) and maximum speed (`4.0x`)
  * `Keypad /` : Show or hide the speed control menu
- **Changing Hotkeys:**
  * Edit `BepInEx/config/tyranny.mod.timespeedcontrol.cfg` in any text editor.
  * Modify key names under the `[Hotkeys]` section using standard Unity key names (e.g., `KeypadPlus`, `F5`, `LeftControl+KeypadPlus`).

### 7. Weapon Mastery & Universal Riposte Rework
- **Description:** Rebalances weapon mastery damage passives, fixes multi-hit probabilities, and removes weapon restrictions from Duelist passives to allow Riposte counter-attacks with any setup.
- **Features:**
  * **Mastery Damage Scaling:** Standardizes damage bonuses for all weapon types:
    * **Tier 1:** +10% Damage
    * **Tier 2:** +20% Damage
    * **Tier 3:** +30% Damage
  * **Multi-Hit Scaling:** Corrects multi-hit attack chances across all mastery levels:
    * **2-Hit Chain:**
      * Tier 1: **30% Chance**
      * Tier 2: **40% Chance**
      * Tier 3: **50% Chance**
    * **3-Hit Chain:**
      * Tier 1: **10% Chance**
      * Tier 2: **20% Chance**
      * Tier 3: **30% Chance**
  * **Universal Riposte:** Enables Duelist passives and Riposte counter-attacks when dodging or grazing enemy melee attacks, regardless of equipped weapon (Works with Two-Handed, Dual Wield, Shields, Bows, Staffs, and Unarmed).

## Requirements
- **Tyranny** (Latest official x64 version)
- **BepInEx v5.x** (x64 release)

## Installation
1. Download and extract **BepInEx v5.x** into your main *Tyranny* game directory (where `Tyranny.exe` is located).
2. Download the pre-built mod DLL files from [Releases](../../releases) or Nexus Mods.
3. Place the desired `.dll` files into `Tyranny/BepInEx/plugins/`.
4. Launch the game (*Note: ensure you are launching the x64 executable*). Configuration files (`.cfg`) will automatically generate in `Tyranny/BepInEx/config/`.

## Configuration
Modules that support custom settings create their configuration files upon first launch:
- `AutoLooter.cfg` (or `Settings.xml` inside `plugins/AutoLoot/`)
- `tyranny.mod.floatingxpnumbers.cfg`
- `tyranny.mod.noperrestabilities.cfg`
- `tyranny.mod.smallpartyxp.cfg`
- `tyranny.mod.timespeedcontrol.cfg`
- `tyranny.mod.weaponmasteryrework.cfg`

*(Note: `GlobalSpeedRework` operates with hardcoded balance values and does not generate a `.cfg` file).*

## Developer Info & Building from Source
This repository is structured as a single Visual Studio mono-repository (`TyrannyMods.sln`).

1. Clone the repo:
   ```bash
   git clone https://github.com/Enryu/Tyranny-Mod-Collection.git
   ```
2. Open `TyrannyMods.sln` in **Visual Studio 2022 or newer** (or JetBrains Rider).
3. Target **Release | x64** and run **Build Solution** (`Ctrl + Shift + B`).
4. Output binaries will automatically populate in the root `builds/` directory.
5. Alternatively, you can download ready-to-use compiled binaries directly from the [Releases](../../releases) section.

## License
Distributed under the **MIT License**. See `LICENSE` for details.