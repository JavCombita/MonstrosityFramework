using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using HarmonyLib;
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
            
            // --- FIX: CS1503 (Monitor -> ModManifest) ---
            _apiInstance = new MonstrosityApi(this.ModManifest);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo por ID.", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista monstruos registrados.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga registros (experimental).", ReloadMonsters);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Integración SpaceCore
            var spaceCore = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            if (spaceCore != null)
            {
                spaceCore.RegisterSerializerType(typeof(CustomMonster));
                Monitor.Log("SpaceCore API encontrada. Serialización activada.", LogLevel.Info);
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

        public override object GetApi()
        {
            return _apiInstance;
        }

        // --- COMANDOS ---

        private void ReloadMonsters(string command, string[] args)
        {
            MonsterRegistry.ClearAll();
            int refreshed = 0;

            // Recargar Content Packs propios
            foreach (var pack in Helper.ContentPacks.GetOwned())
            {
                var data = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        MonsterRegistry.Register($"{pack.Manifest.UniqueID}.{kvp.Key}", kvp.Value, pack);
                        refreshed++;
                    }
                }
            }

            // Recargar datos vía Content Pipeline (si existen)
            var cpData = Helper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
            if (cpData != null)
            {
                foreach (var kvp in cpData)
                {
                    MonsterRegistry.Register($"CP.{kvp.Key}", kvp.Value, null, ModManifest);
                    refreshed++;
                }
            }
            
            Monitor.Log($"Recarga completada. {refreshed} monstruos actualizados.", LogLevel.Info);
        }

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) return;
            string id = args[0];
            
            // FIX: Ahora esto funcionará porque agregamos el método en MonsterRegistry
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