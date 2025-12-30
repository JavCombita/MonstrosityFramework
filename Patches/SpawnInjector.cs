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
            if (!Context.IsMainPlayer) return;
            
            // CAMBIO 1: Ya no restringimos solo a MineShaft. Usamos GameLocation genérico.
            GameLocation location = e.NewLocation;

            // Filtro de seguridad: No spawnear en casas de NPCs, tiendas, etc. salvo que se especifique.
            // (Opcional) Si quieres spawnear en CUALQUIER lado, quita este if o ajústalo.
            if (!IsSpawnableLocation(location)) return;

            // Lógica Anti-Grind
            if (CheckAlreadyPopulated(location))
            {
                if (Game1.random.NextDouble() > 0.1) return;
            }

            AttemptSpawn(location);
        }

        // Define qué lugares permiten spawn general
        private bool IsSpawnableLocation(GameLocation loc)
        {
            // Permitir siempre en Minas, Volcán y Skull Cavern
            if (loc is MineShaft || loc is VolcanoDungeon) return true;

            // En exteriores (Granja, Bosque, Pueblo), solo permitir de noche (después de las 7 PM)
            // para evitar caos con los aldeanos durante el día.
            if (loc.IsOutdoors && Game1.timeOfDay >= 1900) return true;
            
            // Permitir alcantarillas y cuevas específicas
            if (loc.Name.Contains("Sewer") || loc.Name.Contains("BugLand")) return true;

            return false;
        }

        private bool CheckAlreadyPopulated(GameLocation location)
        {
            // Contamos monstruos custom existentes
            return location.characters.Any(c => c is CustomMonster) || location.characters.Count(c => c is Monster) > 5;
        }

        private void AttemptSpawn(GameLocation location)
        {
            var registry = MonsterRegistry.GetAllIds();
            if (!registry.Any()) return;

            // Intentar spawnear de 1 a 3 monstruos
            int spawnCount = Game1.random.Next(1, 4);

            for (int i = 0; i < spawnCount; i++)
            {
                // 1. Validar posición en CUALQUIER mapa
                Vector2? spawnPos = FindValidSpawnPosition(location);
                if (spawnPos == null) continue;

                // 2. Elegir monstruo candidato
                string monsterId = registry.ElementAt(Game1.random.Next(registry.Count));
                var entry = MonsterRegistry.Get(monsterId);

                // --- LÓGICA DE FILTRADO AVANZADA ---
                if (!CanSpawnInLocation(entry, location)) continue;

                // 3. Crear Monstruo
                string fullId = MonsterRegistry.GetAllIds().FirstOrDefault(x => MonsterRegistry.Get(x) == entry);
                
                if (fullId != null)
                {
                    var monster = new CustomMonster(fullId, spawnPos.Value * 64f);
                    location.characters.Add(monster);
                    _monitor.Log($"[Spawn] {entry.Data.DisplayName} en {location.Name} ({spawnPos.Value})", LogLevel.Trace);
                }
            }
        }

        // Valida si el monstruo Específico puede vivir en este Mapa Específico
        private bool CanSpawnInLocation(MonsterRegistry.RegistryEntry entry, GameLocation location)
        {
            if (entry.Data.Spawn == null) return true; // Si no tiene reglas, spawnea donde sea (Cuidado)

            // Probabilidad global
            if (Game1.random.NextDouble() > entry.Data.Spawn.SpawnWeight) return false;

            // CASO A: Es una Mina o Mazmorra (Tiene Niveles)
            if (location is MineShaft mine)
            {
                // Verificar rango de pisos
                if (mine.mineLevel < entry.Data.Spawn.MinMineLevel || 
                    mine.mineLevel > entry.Data.Spawn.MaxMineLevel)
                {
                    return false;
                }
                return true;
            }
            else if (location is VolcanoDungeon volcano)
            {
                // El volcán suele usar niveles relativos, podemos usar logic similar o específica
                // Para simplificar, asumimos que si está en SpecificLocations funciona
            }

            // CASO B: Lugares Específicos por Nombre (Granja, Bosque, etc)
            // Verifica si el JSON tiene "SpecificLocations": ["Farm", "Forest", "VolcanoDungeon"]
            if (entry.Data.Spawn.SpecificLocations != null && entry.Data.Spawn.SpecificLocations.Count > 0)
            {
                // Comparamos el Nombre del mapa actual con la lista del JSON
                // Ej: location.Name es "Farm", el JSON tiene "Farm" -> True
                if (entry.Data.Spawn.SpecificLocations.Contains(location.Name))
                {
                    return true;
                }
                
                // Si la lista existe pero no estamos en ese lugar -> False
                return false; 
            }

            // Si no es mina y no está en la lista de permitidos -> No spawnear
            return false;
        }

        private Vector2? FindValidSpawnPosition(GameLocation location)
        {
            for (int i = 0; i < 50; i++)
            {
                int x = Game1.random.Next(0, location.Map.Layers[0].LayerWidth);
                int y = Game1.random.Next(0, location.Map.Layers[0].LayerHeight);
                Vector2 tilePos = new Vector2(x, y);

                // Chequeos universales (funcionan en Farm, Town, Mine, etc)
                if (!location.isTileOnMap(tilePos)) continue;
                if (location.isWaterTile(x, y)) continue;

                // Evitar spawnear dentro de la casa de la granja si estamos afuera
                // (isTileLocationTotallyClearAndPlaceable se encarga de edificios y muebles)
                if (!location.isTileLocationTotallyClearAndPlaceable(tilePos)) continue;

                // Distancia segura al jugador
                if (Utility.distance(x, y, Game1.player.TilePoint.X, Game1.player.TilePoint.Y) > 8)
                {
                    return tilePos; 
                }
            }
            return null; 
        }
    }
}