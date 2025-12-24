using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using MonstrosityFramework.API;
using MonstrosityFramework.Framework;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities;
using MonstrosityFramework.Integrations;
using MonstrosityFramework.Patches;

namespace MonstrosityFramework
{
    public class ModEntry : Mod
    {
        // Acceso estático global para el resto del framework
        public static IModHelper ModHelper;
        public static IMonitor StaticMonitor;

        // Instancia de nuestra API
        private MonstrosityApi _apiInstance;

        public override void Entry(IModHelper helper)
        {
            // 1. Inicialización Global
            ModHelper = helper;
            StaticMonitor = Monitor;
            _apiInstance = new MonstrosityApi(Monitor);

            // 2. Eventos del Ciclo de Vida
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            
            // 3. Inicializar Sistema de Spawns (Inyección en Minas)
            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            // 4. Comandos de Consola (Para pruebas rápidas)
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo registrado.\nUso: monster_spawn <UniqueID>", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos registrados.", ListMonsters);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // 5. Integración con SpaceCore (Vital para guardar partida)
            // Esto registra el tipo 'CustomMonster' en el serializador XML.
            bool spaceCoreLoaded = SpaceCoreBridge.Init(Helper, Monitor);
            
            if (!spaceCoreLoaded)
            {
                Monitor.Log("ADVERTENCIA: SpaceCore no está instalado o falló al cargar.", LogLevel.Alert);
                Monitor.Log("Podrás invocar monstruos, pero si guardas la partida con ellos vivos, el juego crasheará.", LogLevel.Alert);
            }
        }

        // --- EXPOSICIÓN DE LA API ---
        /// <summary>
        /// Permite que otros mods llamen a: Helper.ModRegistry.GetApi<IMonstrosityApi>(...)
        /// </summary>
        public override object GetApi()
        {
            return _apiInstance;
        }

        // --- COMANDOS DE DEBUG ---
        
        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Debes cargar una partida primero.", LogLevel.Warn);
                return;
            }

            if (args.Length < 1)
            {
                Monitor.Log("Uso: monster_spawn <Author.Mod.Id>", LogLevel.Error);
                return;
            }

            string id = args[0];
            
            // Verificamos si existe en nuestro registro
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Error: El monstruo '{id}' no existe en el registro.", LogLevel.Error);
                Monitor.Log("Usa 'monster_list' para ver los IDs disponibles.", LogLevel.Info);
                return;
            }

            // Spawnear a 1 casilla del jugador
            Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
            
            // Crear la entidad
            var monster = new CustomMonster(id, pos);
            
            // Añadir al mapa actual
            Game1.currentLocation.characters.Add(monster);
            
            Monitor.Log($"¡Éxito! Spawneado {id} en {Game1.currentLocation.Name}.", LogLevel.Info);
        }

        private void ListMonsters(string command, string[] args)
        {
            var ids = MonsterRegistry.GetAllIds();
            if (!ids.Any())
            {
                Monitor.Log("No hay monstruos registrados actualmente.", LogLevel.Info);
                return;
            }

            Monitor.Log("=== Monstruos Registrados ===", LogLevel.Info);
            foreach (var id in ids)
            {
                var monster = MonsterRegistry.Get(id);
                Monitor.Log($"- ID: {id} | Name: {monster.Data.DisplayName} | Mod: {monster.OwnerMod.Name}", LogLevel.Info);
            }
        }
    }
}