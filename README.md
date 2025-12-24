Markdown# 🦁 Monstrosity Framework: La Guía Definitiva

> **El Motor de Élite para la Creación de Monstruos en Stardew Valley.**

**Monstrosity Framework** es una infraestructura avanzada que permite a los modders agregar nuevos enemigos al juego **sin necesidad de programar lógica compleja**.

El framework se encarga automáticamente de:
* ✅ **IA y Comportamiento:** Lógica de persecución, sigilo o tanque.
* ✅ **Persistencia (SpaceCore):** Guardado y carga de datos sin corromper partidas.
* ✅ **Spawning Procedural:** Aparición natural en las minas según tus reglas.
* ✅ **Sincronización Multijugador:** Los monstruos se ven y comportan igual para todos los jugadores.

---

## 📑 Tabla de Contenidos

1.  [Instalación y Requisitos](#-instalación-y-requisitos)
2.  [Cómo Crear tu Mod (Paso a Paso)](#-cómo-crear-tu-mod-paso-a-paso)
3.  [Documentación de monsters.json](#-documentación-de-monsters.json)
4.  [Guía de Sprites](#-guía-de-sprites-arte)
5.  [Kit de Ejemplos (Copiar y Pegar)](#-kit-de-ejemplos-listos-para-usar)
6.  [Comandos de Debug](#-comandos-de-consola)

---

## 📦 Instalación y Requisitos

### Para Jugadores (Usuarios Finales)
1.  Instalar la última versión de **[SMAPI](https://smapi.io/)**.
2.  Instalar **[SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)** (Obligatorio para guardar la partida).
3.  Instalar **Monstrosity Framework**.
4.  Instalar los Content Packs que deseen.

### Para Modders (Dependencias)
En tu archivo `manifest.json`, debes declarar la dependencia para asegurar que el framework cargue antes que tu mod.

```json
"Dependencies": [
   {
      "UniqueID": "TuNombre.MonstrosityFramework",
      "IsRequired": true
   }
]
```

---

## 🛠️ Cómo Crear tu Mod (Paso a Paso)
Para agregar monstruos, crearás un mod estándar de SMAPI que actúa como "puente" para pasarle los datos al Framework.

1. Estructura de CarpetasOrganiza tu proyecto exactamente así:

MyDungeonMod/
├── manifest.json           <-- Identidad del mod
├── MyDungeonMod.dll        <-- Tu código compilado (ver punto 2)
└── assets/
    ├── monsters.json       <-- Configuración de stats y drops
    └── sprites/            <-- Tus imágenes PNG
        ├── goblin.png
        └── ghost.png
		
2. El Código Puente (ModEntry.cs)No necesitas programar IA. Solo necesitas este código para registrar tus archivos JSON en el sistema.

```C#
using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MyDungeonMod
{
    // 1. Definimos la Interfaz para hablar con el Framework
    public interface IMonstrosityApi
    {
        void RegisterMonster(IManifest mod, string id, object data);
    }

    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            // Esperamos a que el juego arranque para registrar
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // 2. Buscamos la API
            var api = Helper.ModRegistry.GetApi<IMonstrosityApi>("TuNombre.MonstrosityFramework");
            if (api == null) return;

            // 3. Leemos nuestro archivo monsters.json
            var monsters = Helper.Data.ReadJsonFile<Dictionary<string, object>>("assets/monsters.json");
            
            if (monsters != null)
            {
                foreach (var kvp in monsters)
                {
                    // 4. Enviamos los datos al Framework
                    api.RegisterMonster(this.ModManifest, kvp.Key, kvp.Value);
                    Monitor.Log($"Monstruo registrado: {kvp.Key}", LogLevel.Info);
                }
            }
        }
    }
}
```

---

### 📜 Documentación de monsters.json

Este archivo controla todo. Es un diccionario donde la Clave es el ID interno y el Valor son sus propiedades.

Tabla de Propiedades.

| Propiedad | Tipo | Descripción | Ejemplo |
|---|---|---|---|
| DisplayName | String | El nombre visible del monstruo. | "Rey Goblin" |
| TexturePath | String | Ruta a la imagen relativa a tu carpeta de mod. | "assets/sprites/king.png" |
| SpriteWidth | Int | Ancho de un solo cuadro (frame) en píxeles. | 16 o 32 |
| SpriteHeight | Int | Alto de un solo cuadro en píxeles. | 24 o 32 |
| MaxHealth | Int | Vida total. | 150 |
| DamageToFarmer | Int | Daño que hace al tocar al jugador. | 12 |
| BehaviorType | String | Tipo de Inteligencia Artificial (ver abajo). | "Stalker" |
| Spawn | Objeto | Reglas de aparición en la mina. | Ver ejemplo |
| Drops | Lista | Lista de objetos que suelta al morir. | Ver ejemplo |

Tipos de IA (BehaviorType)
"Default": Comportamiento estándar (como murciélagos o slimes). Persigue al jugador en línea recta.
"Stalker": IA Avanzada. Solo se mueve hacia el jugador si este no lo está mirando. Se congela si lo miras.
"Tank": Movimiento lento, imparable, ignora colisiones menores. Ideal para jefes o golems.

---

### 🎨 Guía de Sprites (Arte)
El sistema usa el formato estándar de Stardew Valley. Tu PNG debe contener 4 filas de animación.

La Regla Matemática:

Ancho de Imagen = SpriteWidth x 4 Alto de Imagen = SpriteHeight x 4 Layout de Animación

        Frame 0   Frame 1   Frame 2   Frame 3
      +---------+---------+---------+---------+
Fila 0|  Abajo  |  Abajo  |  Abajo  |  Abajo  |  (Caminando hacia la cámara)
      +---------+---------+---------+---------+
Fila 1| Derecha | Derecha | Derecha | Derecha |
      +---------+---------+---------+---------+
Fila 2| Arriba  | Arriba  | Arriba  | Arriba  |  (De espaldas)
      +---------+---------+---------+---------+
Fila 3| Izq.    | Izq.    | Izq.    | Izq.    |
      +---------+---------+---------+---------+

---

### 🧪 Kit de Ejemplos (Listos para Usar)
Copia este contenido en tu assets/monsters.json para empezar inmediatamente con 3 monstruos funcionales.

```json
{
  "GoblinGrunt": {
    "DisplayName": "Recluta Goblin",
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
    "DisplayName": "Espectro del Vacío",
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
    "DisplayName": "Gólem Dorado",
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

Referencia Rápida de Items (IDs):
388: Madera
336: Lingote de Oro
337: Lingote de Iridio
768: Esencia Solar
769: Esencia del Vacio
74: Esquirla Prismática

---

### 🔧 Comandos de Consola
Usa la consola de SMAPI (la ventana negra que se abre con el juego) para probar tus monstruos sin tener que buscarlos en la mina.

1. monster_list Muestra una lista de todos los monstruos registrados correctamente.
2. monster_spawn <ID_Completo> Hace aparecer un monstruo frente a ti.

Nota: El ID completo se forma así: TuModID.NombreDelJSON.

Ejemplo: monster_spawn TuNombre.MyDungeonMod.GoblinGrunt