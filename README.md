# 🧟 Monstrosity Framework

**Monstrosity Framework** is an elite architecture tool for Stardew Valley that allows modders to create, customize, and inject new monsters into the game without writing complex C# code.

It supports **Native Content Packs**, Content Patcher integration, and C# AI extensions.

## 🚀 For Content Creators (No Code)

Want to add a new monster? You only need a sprite sheet and a JSON file.

### Step 1: Create your Content Pack
Create a folder inside `Mods/` with the following structure:

```
MyNewMonster/
├── manifest.json
├── monsters.json
└── assets/
    └── my_sprite.png
```

### Step 2: The `manifest.json`
Declare your mod as a content pack for Monstrosity.

```json
{
  "Name": "My Monster Pack",
  "Author": "YourName",
  "Version": "1.0.0",
  "UniqueID": "YourName.MyMonster",
  "ContentPackFor": {
    "UniqueID": "JavCombita.MonstrosityFramework"
  }
}
```

### Step 3: Configure `monsters.json`
Define stats, behavior, and drops. (Check the included `examples/monsters.json` for a full reference).

---

## 🧠 Behavior Types (AI)

The framework includes these built-in behaviors. Use the ID in the `"BehaviorType"` field of your JSON.

| Behavior ID | Description | CustomFields Options |
| :--- | :--- | :--- |
| **`Default`** / **`Stalker`** | Chases the player if within range. Similar to Shadow Brutes. | `"DetectionRange": "16"` (Sight radius) |
| **`Slime`** | Jumps, charges attacks, and moves erratically. | N/A |
| **`RockCrab`** | Disguises as a rock. Invulnerable until moving or hit with a pickaxe. | N/A |
| **`Mummy`** | Collapses on death. Revives after 10s unless blown up. | N/A |
| **`Bat`** / **`Ghost`** | Flies through walls. Accelerates towards the player. | N/A |
| **`Shooter`** | Keeps distance and fires projectiles (Shadow Shaman style). | `"ProjectileType": "fire"` or `"ice"` |
| **`Duggy`** | Untouchable underground. Pops up when the player is near. | N/A |

---

## 🛠️ For Developers (C# API)

Monstrosity exposes a powerful API allowing other mods to register their own custom AI logic.

### How to use the API
In your `ModEntry.cs`:

```csharp
public override void Entry(IModHelper helper)
{
    var api = helper.ModRegistry.GetApi<IMonstrosityApi>("JavCombita.MonstrosityFramework");
    
    // Register a new custom AI
    api.RegisterBehavior("CyborgNinja", new NinjaBehavior());
}
```

### Creating a Custom Behavior
Create a class inheriting from `MonsterBehavior`:

```csharp
using MonstrosityFramework.Entities.Behaviors;

public class NinjaBehavior : MonsterBehavior
{
    public override void Update(CustomMonster monster, GameTime time)
    {
        // Your movement logic here...
        // Example: Teleport if too far
        if (Vector2.Distance(monster.Position, monster.Player.Position) > 500)
        {
            monster.Position = monster.Player.Position;
        }
    }
}
```

Now, users can set `"BehaviorType": "CyborgNinja"` in their JSON files.

---

## 📥 Installation

1. Install [SMAPI](https://smapi.io).
2. Install **Monstrosity Framework** into your `Mods` folder.
3. (Optional) Install **SpaceCore** for custom skills and advanced serialization support.

## 🐛 Console Commands
* `monster_list`: Lists all registered monsters and their source.
* `monster_spawn <id>`: Spawns a monster at your position for testing.
* `monster_reload`: Reloads all Content Packs without restarting the game.

---
*Developed by JavCombita - Elite Modding Architect*
