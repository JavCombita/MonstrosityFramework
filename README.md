# 🧟 Monstrosity Framework

**Monstrosity Framework** is an advanced architecture tool for Stardew Valley that empowers modders to create, customize, and inject complex custom monsters into the game without writing a single line of C# code.

It features **1:1 Vanilla Behavior replication**, advanced AI customization via JSON, and a powerful API for developers.

## ✨ Key Features

* **No-Code Creation:** Add monsters using only a sprite sheet and a JSON file.
* **Advanced AI Library:** Includes complex behaviors like **Shooters** (projectiles), **Slimes** (reproduction), **Mummies** (resurrection), and **Bats** (spirals/lunges).
* **High Customization:** Configure attack speeds, projectile types, colors, mating logic, and more via simple text fields.
* **SpaceCore Integration:** Full serialization support (monsters save/load correctly).
* **Developer API:** Register your own C# AI classes to extend the framework.

---

## 🚀 For Content Creators (No Code)

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

```
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

Use the `BehaviorType` and `CustomFields` to define how your monster acts.

#### **Global CustomFields**

These options work for **all** behavior types:
| Field | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `DetectionRange` | Float | Radius (in tiles) to spot the player. | `8` |
| `Tint` | Hex | Color overlay for the sprite (e.g., `#FF0000` for red). | `#FFFFFF` |

---

## 🧠 Behavior Types & Options

Choose a `BehaviorType` ID and customize it with specific fields.

### 🏹 **`Shooter`** (Shadow Sniper Style)

Maintains distance and fires projectiles.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `BurstCount` | Int | Number of shots fired per attack volley. | `1` |
| `ProjectileSprite` | Int | Sprite ID from `Projectiles.xnb` (12=Shadow, 14=Fire). | `12` |
| `ProjectileSpeed` | Float | Speed of the projectile. | `12` |
| `ProjectileDebuff` | String | Buff/Debuff ID applied on hit (e.g. "19" for Frozen). | `26` |
| `ShootSound` | String | Audio cue ID when firing. | `Cowboy_gunshot` |
| `AimTime` | Float | Seconds spent aiming before firing. | `0.25` |

### 💧 **`Slime`** (Slime Style)

Jumps, slides, and can reproduce.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `JumpChance` | Float | Chance (0.0-1.0) per tick to initiate a jump. | `0.01` |
| `JumpChargeTime` | Int | Milliseconds to "squish" before jumping. | `800` |
| `CanMate` | Bool | `1` = Can reproduce with other slimes. | `0` |

### 🦇 **`Bat`** (Flying Style)

Flies through walls and sleeps when idle. Can spiral or lunge.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `CanLunge` | Bool | `1` = Enables "Magma Sprite" charge attacks. | `0` |
| `LungeSpeed` | Float | Speed during the charge attack. | `25` |

### 👻 **`Ghost`** (Ethereal Style)

Floats, oscillates vertically, and ignores walls.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `HasLight` | Bool | `1` = Emits light (Carbon Ghost style). | `0` |
| `LightColor` | Hex | Hex color of the emitted light. | `#40FF40` |

### 🤕 **`Mummy`** (Undead Style)

Chases player. Collapses on "death" and revives unless destroyed by a Bomb or Crusader enchantment.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `RevivalTime` | Int | Milliseconds before reviving from the pile of rags. | `10000` |

### 🦀 **`RockCrab`** (Ambush Style)

Disguises as a rock (Sprite frame 0). Invulnerable until moving or hit by Pickaxe/Bomb.
| CustomField | Type | Description | Default |
| :--- | :--- | :--- | :--- |
| `ShellHealth` | Int | Number of Pickaxe hits required to break the shell. | `5` |

### 🐛 **`Duggy`** (Trap Style)

Invulnerable underground. Pops up when the player steps on a nearby tile.

* **Note:** Use `DetectionRange` to control trigger distance.

### 🧟 **`Stalker`** / **`Default`**

Basic chase AI. Similar to Golems or Shadow Brutes.

---

## 🛠️ For Developers (C# API)

Monstrosity exposes a robust API allowing other mods to register their own custom AI logic.

### 1. Access the API

In your `ModEntry.cs`:

```
public override void Entry(IModHelper helper)
{
    var api = helper.ModRegistry.GetApi<IMonstrosityApi>("JavCombita.MonstrosityFramework");
    
    // Register a new custom AI
    api.RegisterBehavior("MyCustomAI", new MyNinjaBehavior());
}

```

### 2. Create Custom Behavior

Inherit from `MonsterBehavior` and override the lifecycle methods:

```
using MonstrosityFramework.Entities.Behaviors;

public class MyNinjaBehavior : MonsterBehavior
{
    public override void Initialize(CustomMonster monster)
    {
        monster.Speed = 6;
        // Access custom fields defined in JSON
        int stealth = GetCustomInt(monster, "StealthLevel", 1);
        monster.SetVar("Stealth", stealth);
    }

    public override void Update(CustomMonster monster, GameTime time)
    {
        // Custom Logic
        if (IsPlayerWithinRange(monster, 10)) {
             monster.IsWalkingTowardPlayer = true;
        }
    }
}

```

---

## 📥 Installation

1. Install [SMAPI](https://smapi.io).
2. Install **SpaceCore** (Required for saving custom monsters).
3. Install **Monstrosity Framework** into your `Mods` folder.

## 🐛 Console Commands

* `monster_list`: Lists all registered monsters and their source.
* `monster_spawn <id>`: Spawns a specific monster at your position (e.g., `monster_spawn JavCombita.VoidStalker`).
* `monster_reload`: Reloads all `monsters.json` files and assets without restarting.

---

*Developed by JavCombita*
