using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using HarmonyLib; // <--- VITAL PARA EL DEBRIS PATCH
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
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            StaticMonitor = Monitor;
            
            // 1. Inicializar API
            _apiInstance = new MonstrosityApi(this.ModManifest);

            // 2. ACTIVAR HARMONY (Esto arregla el crash del Debris)
            try 
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);
                harmony.PatchAll(); 
                Monitor.Log("Harmony patcheado correctamente (DebrisSafety activo).", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error fatal iniciando Harmony: {ex}", LogLevel.Error);
            }

            // 3. Eventos
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 4. Inyector de Spawns
            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            // 5. Comandos
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo por ID.", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista monstruos registrados.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga registros.", ReloadMonsters);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // A. Integración SpaceCore
            var spaceCore = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            if (spaceCore != null)
            {
                spaceCore.RegisterSerializerType(typeof(CustomMonster));
                Monitor.Log("SpaceCore API encontrada. Serialización activada.", LogLevel.Info);
            }

            // B. CARGA DE MONSTRUOS (Movido aquí para evitar lista vacía al inicio)
            LoadContentPacks();
        }

        private void LoadContentPacks()
        {
            int count = 0;
            Monitor.Log("Iniciando carga de Content Packs...", LogLevel.Info);

            foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
            {
                try
                {
                    // Leemos el JSON como Diccionario
                    var dict = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");
                    
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            // ID Global = UniqueID del mod + ID local del json
                            string fullId = $"{pack.Manifest.UniqueID}.{kvp.Key}";
                            
                            // Registramos usando la nueva sobrecarga que arreglamos
                            MonsterRegistry.Register(fullId, kvp.Value, pack);
                            count++;
                        }
                        Monitor.Log($"Cargado pack: {pack.Manifest.Name} ({dict.Count} monstruos)", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error leyendo 'monsters.json' en {pack.Manifest.Name}: {ex.Message}", LogLevel.Warn);
                }
            }
            Monitor.Log($"Total monstruos cargados: {count}", LogLevel.Info);
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

        public override object GetApi()
        {
            return _apiInstance;
        }

        // --- COMANDOS ---

        private void ReloadMonsters(string command, string[] args)
        {
            MonsterRegistry.ClearAll();
            
            // Reutilizamos la lógica de carga
            LoadContentPacks();

            // Recargar datos vía Content Pipeline (Legacy/CP)
            var cpData = Helper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
            if (cpData != null)
            {
                foreach (var kvp in cpData)
                {
                    MonsterRegistry.Register($"CP.{kvp.Key}", kvp.Value, null, ModManifest);
                }
            }
            
            Monitor.Log("Recarga manual completada.", LogLevel.Alert);
        }

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) return;
            string id = args[0];
            
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Monstruo '{id}' no encontrado. Usa 'monster_list'.", LogLevel.Error);
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