using System;
using System.Collections.Generic;
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
using MonstrosityFramework.Framework.Data;

namespace MonstrosityFramework
{
    public class ModEntry : Mod
    {
        public static IModHelper ModHelper;
        public static IMonitor StaticMonitor;

        private MonstrosityApi _apiInstance;
        
        // Mantenemos el soporte legacy para Content Patcher por si acaso
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            StaticMonitor = Monitor;
            _apiInstance = new MonstrosityApi(Monitor);

            // 1. Eventos Core
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            
            // 2. Sistema Legacy (Content Patcher Bridge)
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 3. Spawner (Recuerda aplicar el fix de !Context.IsMainPlayer en SpawnInjector.cs)
            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            // 4. Comandos
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo por ID.", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos registrados.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga JSONs y texturas sin reiniciar.", ReloadMonstersCommand);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Inicializar SpaceCore (Vital para guardar)
            bool spaceCoreLoaded = SpaceCoreBridge.Init(Helper, Monitor);
            if (!spaceCoreLoaded)
            {
                Monitor.Log("SpaceCore no encontrado. Los monstruos no se guardarán al dormir.", LogLevel.Alert);
            }

            // CARGAR CONTENT PACKS (Nuevo Sistema Elite)
            LoadContentPacks();

            // CARGAR CONTENT PATCHER (Sistema Legacy/Híbrido)
            // Esto es útil si alguien quiere editar un monstruo existente vía CP
            try
            {
                var cpData = Helper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
                if (cpData != null && cpData.Count > 0)
                {
                    Monitor.Log($"Cargando {cpData.Count} monstruos legacy (Content Patcher)...", LogLevel.Info);
                    foreach (var kvp in cpData)
                    {
                        // Usamos el manifest del framework como "dueño" de estos datos huérfanos
                        _apiInstance.RegisterMonster(this.ModManifest, kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Nota: No se encontraron datos legacy de Content Patcher ({ex.Message})", LogLevel.Trace);
            }
        }

        private void LoadContentPacks()
        {
            Monitor.Log("Escaneando Content Packs...", LogLevel.Info);
            
            // SMAPI busca carpetas con "ContentPackFor": { "UniqueID": "Tu.Mod.ID" }
            foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Leyendo pack: {pack.Manifest.Name} ({pack.Manifest.Version})", LogLevel.Info);
                
                try
                {
                    // Leemos el archivo monsters.json en la raíz del pack
                    var packMonsters = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");
                    
                    if (packMonsters != null)
                    {
                        foreach (var kvp in packMonsters)
                        {
                            // Registramos pasando el 'pack' para que pueda cargar sus propias texturas luego
                            _apiInstance.RegisterMonsterFromPack(pack, kvp.Key, kvp.Value);
                        }
                        Monitor.Log($"  > Registrados {packMonsters.Count} monstruos de '{pack.Manifest.Name}'.", LogLevel.Info);
                    }
                    else
                    {
                        Monitor.Log($"  > El archivo 'monsters.json' no existe o está vacío en {pack.Manifest.Name}.", LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"  > Error fatal leyendo pack '{pack.Manifest.Name}': {ex.Message}", LogLevel.Error);
                }
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(CpAssetPath))
            {
                e.LoadFrom(() => new Dictionary<string, MonsterData>(), AssetLoadPriority.Exclusive);
            }
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            MonsterRegistry.Cleanup();
        }

        public override object GetApi() => _apiInstance;

        // --- COMANDOS ---

        private void ReloadMonstersCommand(string command, string[] args)
        {
            Monitor.Log("--- Iniciando Recarga en Caliente ---", LogLevel.Info);
            
            // 1. Limpieza
            MonsterRegistry.Cleanup(); // Libera texturas de la RAM
            
            // 2. Recarga de Packs
            LoadContentPacks();
            
            // 3. Recarga de CP (Invalidar caché)
            Helper.GameContent.InvalidateCache(CpAssetPath);
            // (La recarga de CP ocurrirá automáticamente la próxima vez que se pida el asset, 
            //  o podemos forzarla si tenemos lógica para ello, pero con Packs es suficiente).

            // 4. Actualizar monstruos vivos en el juego
            if (Context.IsWorldReady)
            {
                int refreshed = 0;
                foreach (var loc in Game1.locations)
                {
                    foreach (var npc in loc.characters)
                    {
                        if (npc is CustomMonster cm)
                        {
                            cm.ReloadData(); // Re-aplica stats y busca la nueva textura
                            refreshed++;
                        }
                    }
                }
                Monitor.Log($"Recarga completada. {refreshed} monstruos actualizados en el mundo.", LogLevel.Success);
            }
            else
            {
                Monitor.Log("Recarga de base de datos completada (No hay mundo cargado).", LogLevel.Success);
            }
        }

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) return;
            string id = args[0];
            
            // Nota: Ahora los IDs de packs suelen ser "Autor.Mod.Monstruo", hay que escribirlo completo
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Monstruo '{id}' no encontrado. Usa 'monster_list' para ver los IDs reales.", LogLevel.Error);
                return;
            }

            Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
            Game1.currentLocation.characters.Add(new CustomMonster(id, pos));
            Monitor.Log($"Spawneado {id}.", LogLevel.Info);
        }

        private void ListMonsters(string command, string[] args)
        {
            Monitor.Log("--- Monstruos Registrados ---", LogLevel.Info);
            foreach (var id in MonsterRegistry.GetAllIds())
            {
                var monster = MonsterRegistry.Get(id);
                string source = monster.SourcePack != null ? $"[Pack: {monster.SourcePack.Manifest.Name}]" : "[Legacy/CP]";
                Monitor.Log($"- {id} {source}", LogLevel.Info);
            }
        }
    }
}