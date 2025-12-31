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

            if (CheckAlreadyPopulated(location)) return;

            AttemptSpawn(location);
        }

        private bool IsSpawnableLocation(GameLocation loc)
        {
            if (loc is MineShaft || loc is VolcanoDungeon) return true;
            
            // FIX: Verificar si es una Granja antes de acceder a la propiedad
            if (loc is Farm farm && farm.spawnMonstersOnThisFarm) return true;
            
            if (loc.Name.Contains("Sewer") || loc.Name.Contains("BugLand")) return true;
            
            if (loc.IsOutdoors && Game1.timeOfDay >= 1900) return true;
            
            return false;
        }

        private bool CheckAlreadyPopulated(GameLocation location)
        {
            return location.characters.Count(c => c is Monster) > 6;
        }

        private void AttemptSpawn(GameLocation location)
        {
            var allIds = MonsterRegistry.GetAllIds();
            if (!allIds.Any()) return;

            foreach (string monsterId in allIds)
            {
                var entry = MonsterRegistry.Get(monsterId);
                if (entry == null) continue;

                if (CanSpawnInLocation(entry.Data, location))
                {
                    int attempts = Math.Max(1, (int)(5 * entry.Data.Spawn.SpawnWeight));
                    
                    for (int i = 0; i < attempts; i++)
                    {
                        Vector2? spawnPos = FindValidSpawnPosition(location);
                        if (spawnPos.HasValue)
                        {
                            var monster = new CustomMonster(monsterId, spawnPos.Value);
                            location.characters.Add(monster);
                            break; 
                        }
                    }
                }
            }
        }

        private bool CanSpawnInLocation(MonsterData data, GameLocation location)
        {
            if (data.Spawn == null) return false; 

            if (data.Spawn.SpecificLocations != null && data.Spawn.SpecificLocations.Count > 0)
            {
                if (data.Spawn.SpecificLocations.Contains(location.Name)) return true;
            }

            if (location is MineShaft mine)
            {
                if (data.Spawn.MinMineLevel > -1 && data.Spawn.MaxMineLevel > -1)
                {
                    if (mine.mineLevel >= data.Spawn.MinMineLevel && mine.mineLevel <= data.Spawn.MaxMineLevel)
                        return true;
                }
            }

            return false;
        }

        private Vector2? FindValidSpawnPosition(GameLocation location)
        {
            for (int i = 0; i < 25; i++)
            {
                int x = Game1.random.Next(0, location.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, location.Map.Layers[0].LayerHeight);
                Vector2 tilePos = new Vector2(x, y);

                if (location.CanSpawnCharacterHere(tilePos))
                {
                    if (Utility.distance(x, y, Game1.player.TilePoint.X, Game1.player.TilePoint.Y) > 8)
                    {
                        return new Vector2(x * 64f, y * 64f - 32f); 
                    }
                }
            }
            return null; 
        }
    }
}