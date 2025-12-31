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

            // Filtro 1: ¿Es un lugar válido para intentar spawns?
            if (!IsSpawnableLocation(location)) return;

            // Filtro 2: ¿Está muy lleno ya? (Evita sobrepoblación)
            if (CheckAlreadyPopulated(location)) return;

            AttemptSpawn(location);
        }

        private bool IsSpawnableLocation(GameLocation loc)
        {
            // Minas y Volcán siempre activos
            if (loc is MineShaft || loc is VolcanoDungeon) return true;
            
            // Lugares con monstruos activados (ej: Granja de noche si el jugador lo activó)
            if (loc.spawnMonstersOnThisFarm) return true;

            // Mapas específicos de monstruos
            if (loc.Name.Contains("Sewer") || loc.Name.Contains("BugLand")) return true;

            // Zonas exteriores de noche (opcional, para dar más vida al mundo)
            if (loc.IsOutdoors && Game1.timeOfDay >= 1900) return true;

            return false;
        }

        private bool CheckAlreadyPopulated(GameLocation location)
        {
            // Si hay más de 6 monstruos, no spawneamos más por ahora para no saturar
            return location.characters.Count(c => c is Monster) > 6;
        }

        private void AttemptSpawn(GameLocation location)
        {
            var allIds = MonsterRegistry.GetAllIds();
            if (!allIds.Any()) return;

            // RECORRIDO COMPLETO: Verificamos CADA monstruo registrado
            foreach (string monsterId in allIds)
            {
                var entry = MonsterRegistry.Get(monsterId);
                if (entry == null) continue;

                // Verificamos si ESTE monstruo específico debe ir en ESTE mapa
                if (CanSpawnInLocation(entry.Data, location))
                {
                    // Calculamos intentos basados en el peso (SpawnWeight)
                    // SpawnWeight 1.0 = 5 intentos. SpawnWeight 0.2 = 1 intento.
                    int attempts = Math.Max(1, (int)(5 * entry.Data.Spawn.SpawnWeight));

                    for (int i = 0; i < attempts; i++)
                    {
                        Vector2? spawnPos = FindValidSpawnPosition(location);
                        if (spawnPos.HasValue)
                        {
                            var monster = new CustomMonster(monsterId, spawnPos.Value);
                            location.characters.Add(monster);
                            // _monitor.Log($"[Spawn] {entry.Data.DisplayName} spawneado en {location.Name}", LogLevel.Trace);
                            
                            // Break aquí para no llenar el mapa con el mismo monstruo en un solo frame
                            // (Si quieres enjambres, quita este break)
                            break; 
                        }
                    }
                }
            }
        }

        private bool CanSpawnInLocation(MonsterData data, GameLocation location)
        {
            if (data.Spawn == null) return false; 

            // 1. Lugares Específicos (Prioridad Alta)
            if (data.Spawn.SpecificLocations != null && data.Spawn.SpecificLocations.Count > 0)
            {
                if (data.Spawn.SpecificLocations.Contains(location.Name)) return true;
                // Si tiene lugares específicos definidos y NO estamos en uno de ellos, retornamos false inmediatamente
                // a menos que quieras que también aparezca en minas por nivel.
                // Asumiremos que SpecificLocations es aditivo a la lógica de minas.
            }

            // 2. Lógica de Minas
            if (location is MineShaft mine)
            {
                // Si el JSON define niveles de mina (-1 significa que no aplica)
                if (data.Spawn.MinMineLevel > -1 && data.Spawn.MaxMineLevel > -1)
                {
                    if (mine.mineLevel >= data.Spawn.MinMineLevel && mine.mineLevel <= data.Spawn.MaxMineLevel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector2? FindValidSpawnPosition(GameLocation location)
        {
            // Intentar 20 veces encontrar un tile libre
            for (int i = 0; i < 20; i++)
            {
                int x = Game1.random.Next(0, location.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, location.Map.Layers[0].LayerHeight);
                Vector2 tilePos = new Vector2(x, y);

                // MÉTODO NATIVO ROBUSTO: Verifica colisiones, bordes y si se puede construir/pisar
                if (location.CanSpawnCharacterHere(tilePos))
                {
                    // Evitar spawnear encima del jugador (mínimo 6 tiles de distancia)
                    if (Utility.distance(x, y, Game1.player.TilePoint.X, Game1.player.TilePoint.Y) > 6)
                    {
                        // Convertir coordenadas de tile a pixeles (centrado)
                        return new Vector2(x * 64f, y * 64f - 32f); 
                    }
                }
            }
            return null; 
        }
    }
}