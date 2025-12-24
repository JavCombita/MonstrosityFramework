# 🦁 Monstrosity Framework: The Ultimate Guide

> **The Elite Engine for Monster Creation in Stardew Valley.**

**Monstrosity Framework** is an advanced infrastructure that allows modders to add new enemies to the game **without needing to program complex logic**.

The framework automatically handles:
* ✅ **AI & Behavior:** Chasing, stealth, or tank logic.
* ✅ **Persistence (SpaceCore):** Saving and loading data without corrupting save files.
* ✅ **Procedural Spawning:** Natural spawning in the mines according to your rules.
* ✅ **Multiplayer Synchronization:** Monsters look and behave the same for all players.

---

## 📑 Table of Contents

1.  [Installation & Requirements](#-installation--requirements)
2.  [How to Create Your Mod (Step-by-Step)](#-how-to-create-your-mod-step-by-step)
3.  [monsters.json Documentation](#-monstersjson-documentation)
4.  [Sprite Guide](#-sprite-guide-art)
5.  [Example Kit (Copy & Paste)](#-example-kit-ready-to-use)
6.  [Console Commands](#-console-commands)

---

## 📦 Installation & Requirements

### For Players (End Users)
1.  Install the latest version of **[SMAPI](https://smapi.io/)**.
2.  Install **[SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)** (Mandatory for saving the game).
3.  Install **Monstrosity Framework**.
4.  Install the Content Packs you wish to use.

### For Modders (Dependencies)
In your `manifest.json` file, you must declare the dependency to ensure the framework loads before your mod.

```json
"Dependencies": [
   {
      "UniqueID": "YourName.MonstrosityFramework",
      "IsRequired": true
   }
]
```

---

## 🛠️ How to Create Your Mod (Step-by-Step)
To add monsters, you will create a standard SMAPI mod that acts as a "bridge" to pass data to the Framework.

1. Folder Structure
Organize your project exactly like this:

```text
MyDungeonMod/
├── manifest.json            <-- Mod Identity
├── MyDungeonMod.dll         <-- Your compiled code (see point 2)
└── assets/
    ├── monsters.json        <-- Stats and drops configuration
    └── sprites/             <-- Your PNG images
        ├── goblin.png
        └── ghost.png
```

2. The Bridge Code (ModEntry.cs)
You don't need to program AI. You only need this code to register your JSON files into the system.

```C#
using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MyDungeonMod
{
    // 1. Define the Interface to talk to the Framework
    public interface IMonstrosityApi
    {
        void RegisterMonster(IManifest mod, string id, object data);
    }

    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            // Wait for the game to launch to register
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // 2. Fetch the API
            var api = Helper.ModRegistry.GetApi<IMonstrosityApi>("YourName.MonstrosityFramework");
            if (api == null) return;

            // 3. Read our monsters.json file
            var monsters = Helper.Data.ReadJsonFile<Dictionary<string, object>>("assets/monsters.json");
            
            if (monsters != null)
            {
                foreach (var kvp in monsters)
                {
                    // 4. Send data to the Framework
                    api.RegisterMonster(this.ModManifest, kvp.Key, kvp.Value);
                    Monitor.Log($"Monster registered: {kvp.Key}", LogLevel.Info);
                }
            }
        }
    }
}
```

---

### 📜 monsters.json Documentation

This file controls everything. It is a dictionary where the Key is the internal ID and the Value contains its properties.

**Property Table**

| Property | Type | Description | Example |
|---|---|---|---|
| DisplayName | String | The visible name of the monster. | "Goblin King" |
| TexturePath | String | Image path relative to your mod folder. | "assets/sprites/king.png" |
| SpriteWidth | Int | Width of a single frame in pixels. | 16 or 32 |
| SpriteHeight | Int | Height of a single frame in pixels. | 24 or 32 |
| MaxHealth | Int | Total health. | 150 |
| DamageToFarmer | Int | Damage dealt on touching the player. | 12 |
| BehaviorType | String | Artificial Intelligence Type (see below). | "Stalker" |
| Spawn | Object | Spawning rules in the mine. | See example |
| Drops | List | List of items dropped upon death. | See example |

**AI Types (BehaviorType)**
* **"Default":** Standard behavior (like bats or slimes). Chases the player in a straight line.
* **"Stalker":** Advanced AI. Only moves toward the player if they are not looking at it. Freezes if you look at it.
* **"Tank":** Slow movement, unstoppable, ignores minor collisions. Ideal for bosses or golems.

---

### 🎨 Sprite Guide (Art)
The system uses the standard Stardew Valley format. Your PNG must contain 4 rows of animation.

**The Math Rule:**
* Image Width = SpriteWidth x 4
* Image Height = SpriteHeight x 4

**Animation Layout**

| | Frame 0 | Frame 1 | Frame 2 | Frame 3 |
|---|---|---|---|---|
| Row 0| Down | Down | Down | Down | (Walking toward camera)
| Row 1| Right | Right | Right | Right |
| Row 2| Up | Up | Up | Up | (Back facing)
| Row 3| Left | Left | Left | Left |

---

### 🧪 Example Kit (Ready to Use)
Copy this content into your `assets/monsters.json` to start immediately with 3 functional monsters.

```json
{
  "GoblinGrunt": {
    "DisplayName": "Goblin Grunt",
    "TexturePath": "assets/sprites/goblin_grunt.png",
    "SpriteWidth": 16,
    "SpriteHeight": 24,
    "MaxHealth": 45,
    "DamageToFarmer": 8,
    "Exp": 5,
    "BehaviorType": "Default",
    "Spawn": {
      "MinMineLevel": 10,
      "MaxMineLevel": 40,
      "SpawnWeight": 1.0
    },
    "Drops": [
      { "ItemId": "388", "Chance": 0.5 }, 
      { "ItemId": "86", "Chance": 0.05 }
    ]
  },

  "VoidWraith": {
    "DisplayName": "Void Wraith",
    "TexturePath": "assets/sprites/void_wraith.png",
    "SpriteWidth": 32,
    "SpriteHeight": 32,
    "MaxHealth": 200,
    "DamageToFarmer": 20,
    "Speed": 4,
    "BehaviorType": "Stalker",
    "Spawn": {
      "MinMineLevel": 80,
      "MaxMineLevel": 120,
      "SpawnWeight": 0.15
    },
    "Drops": [
      { "ItemId": "769", "Chance": 1.0 },
      { "ItemId": "768", "Chance": 0.5 },
      { "ItemId": "337", "Chance": 0.02 }
    ]
  },

  "GoldenGolem": {
    "DisplayName": "Golden Golem",
    "TexturePath": "assets/sprites/golden_golem.png",
    "SpriteWidth": 16,
    "SpriteHeight": 24,
    "MaxHealth": 600,
    "DamageToFarmer": 15,
    "Speed": 1,
    "BehaviorType": "Tank",
    "Spawn": {
      "MinMineLevel": 50,
      "MaxMineLevel": 120,
      "SpawnWeight": 0.02
    },
    "Drops": [
      { "ItemId": "336", "Chance": 1.0, "MinStack": 3, "MaxStack": 6 },
      { "ItemId": "74", "Chance": 0.05 }
    ]
  }
}
```

**Quick Item Reference (IDs):**
1.  **388:** Wood
2.  **336:** Gold Bar
3.  **337:** Iridium Bar
4.  **768:** Solar Essence
5.  **769:** Void Essence
6.  **74:** Prismatic Shard

---

### 🔧 Console Commands
Use the SMAPI console (the black window that opens with the game) to test your monsters without having to search for them in the mine.

1.  `monster_list` - Shows a list of all correctly registered monsters.
2.  `monster_spawn <Full_ID>` - Spawns a monster in front of you.

> **Note:** The full ID is formed like this: `YourModID.JSONName`.

Example: `monster_spawn YourName.MyDungeonMod.GoblinGrunt`