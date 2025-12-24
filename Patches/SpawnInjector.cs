using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities;
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
            if (e.NewLocation is not MineShaft mine) return;

            if (CheckAlreadyPopulated(mine))
            {
                if (Game1.random.NextDouble() > 0.1) return;
            }

            AttemptSpawn(mine);
        }

        private bool CheckAlreadyPopulated(MineShaft mine)
        {
            return mine.characters.Any(c => c is Monster);
        }

        private void AttemptSpawn(MineShaft mine)
        {
            int currentFloor = mine.mineLevel;
            var candidates = GetCandidatesForFloor(currentFloor);

            if (!candidates.Any()) return;

            Random rng = Game1.random;
            int attempts = rng.Next(1, 4); 

            for (int i = 0; i < attempts; i++)
            {
                foreach (var candidate in candidates)
                {
                    if (rng.NextDouble() < candidate.Data.Spawn.SpawnWeight)
                    {
                        SpawnMonster(mine, candidate);
                    }
                }
            }
        }

        private List<RegisteredMonster> GetCandidatesForFloor(int floor)
        {
            var list = new List<RegisteredMonster>();
            foreach (var monster in MonsterRegistry.GetAll())
            {
                var rules = monster.Data.Spawn;
                if (floor >= rules.MinMineLevel && floor <= rules.MaxMineLevel)
                {
                    list.Add(monster);
                }
            }
            return list;
        }

        private void SpawnMonster(MineShaft mine, RegisteredMonster entry)
        {
            Vector2? spawnPos = FindValidSpawnPosition(mine);
            
            if (spawnPos.HasValue)
            {
                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    mine.characters.Add(monster);
                    _monitor.Log($"[Spawn] {entry.Data.DisplayName} en piso {mine.mineLevel}", LogLevel.Trace);
                }
            }
        }

        private Vector2? FindValidSpawnPosition(MineShaft mine)
        {
            for (int i = 0; i < 15; i++)
            {
                int x = Game1.random.Next(0, mine.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, mine.Map.Layers[0].LayerHeight);
                Vector2 pos = new Vector2(x, y); // Coordenada Tile

                // CORRECCIONES 1.6:
                // 1. Reemplazamos 'isTileLocationTotallyClearAndPlaceable' con 'isTileClear' que es más estándar ahora.
                // 2. Usamos 'Game1.player' en lugar de 'mine.Player'.
                // 3. Calculamos la distancia manualmente sin 'getTileXAt'.
                
                if (mine.isTileClear(pos)) // Verifica colisiones y ocupación básica
                {
                    // Cálculo manual de distancia en tiles
                    float playerTileX = Game1.player.Position.X / 64f;
                    float playerTileY = Game1.player.Position.Y / 64f;
                    
                    if (Utility.distance(x, y, playerTileX, playerTileY) > 6)
                    {
                        return pos;
                    }
                }
            }
            return null;
        }
    }
}