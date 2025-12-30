using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities;
using MonstrosityFramework.Framework.Data;
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

            if (!IsSpawnableLocation(location)) return;

            if (CheckAlreadyPopulated(location))
            {
                if (Game1.random.NextDouble() > 0.1) return;
            }

            AttemptSpawn(location);
        }

        private bool IsSpawnableLocation(GameLocation loc)
        {
            if (loc is MineShaft || loc is VolcanoDungeon) return true;
            // Permitir en granja/bosque solo de noche
            if (loc.IsOutdoors && Game1.timeOfDay >= 1900) return true;
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
            
            // FIX: .Count() es un método de LINQ, necesita paréntesis
            if (!registry.Any()) return;

            int spawnCount = Game1.random.Next(1, 4);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2? spawnPos = FindValidSpawnPosition(location);
                if (spawnPos == null) continue;

                // FIX: .Count() con paréntesis
                string monsterId = registry.ElementAt(Game1.random.Next(registry.Count()));
                var entry = MonsterRegistry.Get(monsterId);

                if (!CanSpawnInLocation(entry.Data, location)) continue;

                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    location.characters.Add(monster);
                    _monitor.Log($"[Spawn] {entry.Data.DisplayName} en {location.Name}", LogLevel.Trace);
                }
            }
        }

        private bool CanSpawnInLocation(MonsterData data, GameLocation location)
        {
            if (data.Spawn == null) return true; 

            if (Game1.random.NextDouble() > data.Spawn.SpawnWeight) return false;

            if (location is MineShaft mine)
            {
                if (mine.mineLevel < data.Spawn.MinMineLevel || 
                    mine.mineLevel > data.Spawn.MaxMineLevel)
                {
                    return false;
                }
                return true;
            }

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

                // FIX FINAL: Usamos CanSpawnCharacterHere que encontramos en tu GameLocation.cs
                // Este método nativo ya verifica isTileOnMap, isTilePlaceable y colisiones.
                if (location.CanSpawnCharacterHere(tilePos))
                {
                    // Chequeo extra de distancia al jugador
                    if (Utility.distance(x, y, Game1.player.TilePoint.X, Game1.player.TilePoint.Y) > 8)
                    {
                        return tilePos; 
                    }
                }
            }
            return null; 
        }
    }
}