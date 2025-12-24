using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters; // Importante para detectar mobs vanilla
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
            // Solo actuar en Minas o Caverna Calavera
            if (e.NewLocation is not MineShaft mine) return;

            // 1. CHEQUEO ANTI-GRIND
            // Si el piso ya tiene enemigos (porque el juego lo generó así o porque ya estuvimos aquí),
            // reducimos drásticamente la probabilidad de inyectar más cosas.
            if (CheckAlreadyPopulated(mine))
            {
                // Solo 10% de chance de agregar extras si ya está poblado
                if (Game1.random.NextDouble() > 0.1) return;
            }

            AttemptSpawn(mine);
        }

        /// <summary>
        /// Verifica si ya hay monstruos (Vanilla o Custom) en el mapa.
        /// </summary>
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
            // Spawnear entre 1 y 3 grupos
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
                // Recuperar ID inverso (Optimizable en el futuro guardando el ID en RegisteredMonster)
                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    // Multiplicamos por 64f para pasar de Tile a Pixel
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    mine.characters.Add(monster);
                    
                    _monitor.Log($"[Spawn] {entry.Data.DisplayName} (Lvl {mine.mineLevel}) pos: {spawnPos.Value}", LogLevel.Trace);
                }
            }
        }

        private Vector2? FindValidSpawnPosition(MineShaft mine)
        {
            for (int i = 0; i < 15; i++)
            {
                int x = Game1.random.Next(0, mine.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, mine.Map.Layers[0].LayerHeight);
                Vector2 pos = new Vector2(x, y);

                // Validaciones de seguridad de Stardew
                if (mine.isTileLocationTotallyClearAndPlaceable(pos) && 
                    !mine.isTileOccupied(pos * 64f) &&
                    mine.isTileOnMap(pos))
                {
                    // No spawnear encima del jugador ni en la entrada
                    if (Utility.distance(x, y, mine.getTileXAt(mine.Player.Position), mine.getTileYAt(mine.Player.Position)) > 6)
                    {
                        return pos;
                    }
                }
            }
            return null;
        }
    }
}