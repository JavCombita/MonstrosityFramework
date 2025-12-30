using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities;
using MonstrosityFramework.Framework.Data; // <--- NECESARIO
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonstrosityFramework.Patches
{
    public class SpawnInjector
    {
        private readonly IMonitor _monitor;

        public SpawnInjector(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void Register(IModHelper helper)
        {
            helper.Events.Player.Warped += OnPlayerWarped;
        }

        private void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            if (!Context.IsMainPlayer) return;
            
            GameLocation location = e.NewLocation;

            // 1. Filtro Global: ¿Es un lugar lógico para monstruos?
            if (!IsSpawnableLocation(location)) return;

            // 2. Anti-Grind: No saturar el mapa
            if (CheckAlreadyPopulated(location))
            {
                if (Game1.random.NextDouble() > 0.1) return;
            }

            AttemptSpawn(location);
        }

        private bool IsSpawnableLocation(GameLocation loc)
        {
            // Siempre permitir en mazmorras
            if (loc is MineShaft || loc is VolcanoDungeon) return true;

            // En exteriores (Granja, Bosque), solo de noche
            if (loc.IsOutdoors && Game1.timeOfDay >= 1900) return true;
            
            // Lugares especiales
            if (loc.Name.Contains("Sewer") || loc.Name.Contains("BugLand")) return true;

            return false;
        }

        private bool CheckAlreadyPopulated(GameLocation location)
        {
            return location.characters.Any(c => c is CustomMonster) || location.characters.Count(c => c is Monster) > 5;
        }

        private void AttemptSpawn(GameLocation location)
        {
            var registry = MonsterRegistry.GetAllIds();
            if (!registry.Any()) return;

            int spawnCount = Game1.random.Next(1, 4);

            for (int i = 0; i < spawnCount; i++)
            {
                // A. Validar posición
                Vector2? spawnPos = FindValidSpawnPosition(location);
                if (spawnPos == null) continue;

                // B. Elegir candidato aleatorio
                string monsterId = registry.ElementAt(Game1.random.Next(registry.Count));
                var entry = MonsterRegistry.Get(monsterId);

                // C. Verificar si este monstruo específico puede vivir aquí
                // FIX: Pasamos entry.Data en lugar de 'entry' para evitar errores de tipo
                if (!CanSpawnInLocation(entry.Data, location)) continue;

                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    // Instanciación directa en píxeles (Tile * 64)
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    
                    location.characters.Add(monster);
                    _monitor.Log($"[Spawn] {entry.Data.DisplayName} spawneado en {location.Name} ({spawnPos.Value})", LogLevel.Trace);
                }
            }
        }

        // FIX: Cambiado el tipo del parámetro de 'RegistryEntry' a 'MonsterData'
        private bool CanSpawnInLocation(MonsterData data, GameLocation location)
        {
            if (data.Spawn == null) return true; 

            if (Game1.random.NextDouble() > data.Spawn.SpawnWeight) return false;

            // Reglas para Minas
            if (location is MineShaft mine)
            {
                if (mine.mineLevel < data.Spawn.MinMineLevel || 
                    mine.mineLevel > data.Spawn.MaxMineLevel)
                {
                    return false;
                }
                return true;
            }

            // Reglas para Otros Lugares (Granja, Volcán, etc)
            if (data.Spawn.SpecificLocations != null && data.Spawn.SpecificLocations.Count > 0)
            {
                if (data.Spawn.SpecificLocations.Contains(location.Name)) return true;
                return false; 
            }

            return false;
        }

        private Vector2? FindValidSpawnPosition(GameLocation location)
        {
            for (int i = 0; i < 50; i++)
            {
                int x = Game1.random.Next(0, location.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, location.Map.Layers[0].LayerHeight);
                Vector2 tilePos = new Vector2(x, y);

                if (!location.isTileOnMap(tilePos)) continue;
                if (location.isWaterTile(x, y)) continue;
                if (!location.isTileLocationTotallyClearAndPlaceable(tilePos)) continue;

                if (Utility.distance(x, y, Game1.player.TilePoint.X, Game1.player.TilePoint.Y) > 8)
                {
                    return tilePos; 
                }
            }
            return null; 
        }
    }
}