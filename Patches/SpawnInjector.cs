using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
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
            // Solo nos interesa si el jugador entra a una Mina o al Volcán
            // Nota: Para simplificar, nos enfocamos en MineShaft (Minas normales y Skull Cavern)
            if (e.NewLocation is not MineShaft mine) return;

            // Evitar spawnear si ya visitamos este piso hoy y ya tiene enemigos (opcional)
            // if (mine.characters.Count > 0) return; 

            AttemptSpawn(mine);
        }

        private void AttemptSpawn(MineShaft mine)
        {
            int currentFloor = mine.mineLevel;
            var candidates = GetCandidatesForFloor(currentFloor);

            if (!candidates.Any()) return;

            // Intentamos spawnear algunos monstruos
            Random rng = Game1.random;
            
            // Lógica simple de densidad: Intentar spawnear entre 1 y 3 monstruos custom extra
            // (Esto podría configurarse en un config.json general)
            int attempts = rng.Next(1, 4); 

            for (int i = 0; i < attempts; i++)
            {
                foreach (var candidate in candidates)
                {
                    // Chequeo de probabilidad global del monstruo (SpawnWeight)
                    // Asumimos que SpawnWeight es una probabilidad simple 0.0 - 1.0 para este ejemplo
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
                
                // Verificar rango de pisos
                if (floor >= rules.MinMineLevel && floor <= rules.MaxMineLevel)
                {
                    list.Add(monster);
                }
                // Aquí podrías agregar lógica para "SpecificLocations" (ej: "Farm", "Forest")
            }
            return list;
        }

        private void SpawnMonster(MineShaft mine, RegisteredMonster entry)
        {
            Vector2? spawnPos = FindValidSpawnPosition(mine);
            
            if (spawnPos.HasValue)
            {
                // Crear la entidad usando el ID Único (ej: "Author.Mod.Goblin")
                // Recuperamos el ID inverso buscando en el registro (un poco ineficiente, mejor pasar el ID)
                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    mine.characters.Add(monster);
                    
                    _monitor.Log($"Spawneado {entry.Data.DisplayName} en piso {mine.mineLevel} en ({spawnPos.Value.X}, {spawnPos.Value.Y})", LogLevel.Trace);
                }
            }
        }

        /// <summary>
        /// Busca una baldosa válida que no sea pared, agua, ni esté ocupada.
        /// </summary>
        private Vector2? FindValidSpawnPosition(MineShaft mine)
        {
            for (int i = 0; i < 15; i++) // 15 intentos para no congelar el juego
            {
                // Obtener una baldosa aleatoria dentro de los límites del mapa
                int x = Game1.random.Next(0, mine.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, mine.Map.Layers[0].LayerHeight);
                Vector2 pos = new Vector2(x, y);

                // Verificaciones de seguridad de Stardew
                if (mine.isTileLocationTotallyClearAndPlaceable(pos) && 
                    !mine.isTileOccupied(pos * 64f) &&
                    mine.isTileOnMap(pos))
                {
                    // Verificar que no sea la entrada (escalera arriba)
                    if (Utility.distance(x, y, mine.getTileXAt(mine.Player.Position), mine.getTileYAt(mine.Player.Position)) > 5)
                    {
                        return pos;
                    }
                }
            }
            return null; // No se encontró lugar seguro
        }
    }
}