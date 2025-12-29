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
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            StaticMonitor = Monitor;
            _apiInstance = new MonstrosityApi(Monitor);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo por ID.", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga JSONs y texturas.", ReloadMonstersCommand);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            bool spaceCoreLoaded = SpaceCoreBridge.Init(Helper, Monitor);
            if (!spaceCoreLoaded)
            {
                Monitor.Log("SpaceCore no encontrado. Los monstruos no se guardarán al dormir.", LogLevel.Alert);
            }

            LoadContentPacks();

            // Carga Legacy (Content Patcher)
            try
            {
                var cpData = Helper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
                if (cpData != null && cpData.Count > 0)
                {
                    Monitor.Log($"Cargando {cpData.Count} monstruos legacy...", LogLevel.Info);
                    foreach (var kvp in cpData)
                    {
                        _apiInstance.RegisterMonster(this.ModManifest, kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Nota: No se encontraron datos legacy ({ex.Message})", LogLevel.Trace);
            }
        }

        private void LoadContentPacks()
        {
            Monitor.Log("Escaneando Content Packs...", LogLevel.Info);
            
            foreach (IContentPack pack in Helper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Leyendo pack: {pack.Manifest.Name}", LogLevel.Info);
                
                try
                {
                    var packMonsters = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");
                    
                    if (packMonsters != null)
                    {
                        foreach (var kvp in packMonsters)
                        {
                            _apiInstance.RegisterMonsterFromPack(pack, kvp.Key, kvp.Value);
                        }
                        Monitor.Log($"  > Registrados {packMonsters.Count} monstruos de '{pack.Manifest.Name}'.", LogLevel.Info);
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"  > Error leyendo pack '{pack.Manifest.Name}': {ex.Message}", LogLevel.Error);
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

        private void ReloadMonstersCommand(string command, string[] args)
        {
            Monitor.Log("--- Iniciando Recarga en Caliente ---", LogLevel.Info);
            
            MonsterRegistry.Cleanup();
            MonsterRegistry.ClearAll(); // Limpiamos la base de datos
            
            LoadContentPacks();
            Helper.GameContent.InvalidateCache(CpAssetPath);

            if (Context.IsWorldReady)
            {
                int refreshed = 0;
                foreach (var loc in Game1.locations)
                {
                    foreach (var npc in loc.characters)
                    {
                        if (npc is CustomMonster cm)
                        {
                            cm.ReloadData();
                            refreshed++;
                        }
                    }
                }
                // ERROR CORREGIDO: Usamos LogLevel.Info en lugar de .Success
                Monitor.Log($"Recarga completada. {refreshed} monstruos actualizados.", LogLevel.Info);
            }
            else
            {
                Monitor.Log("Recarga de base de datos completada.", LogLevel.Info);
            }
        }

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) return;
            string id = args[0];
            
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Monstruo '{id}' no encontrado. Usa 'monster_list' para ver IDs.", LogLevel.Error);
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