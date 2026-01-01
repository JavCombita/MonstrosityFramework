using System;
using System.Collections.Generic;
using System.Linq;
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
using SpaceCore; // Asegúrate de tener referenciado SpaceCore o usar Reflection si es soft-dependency

namespace MonstrosityFramework
{
    public class ModEntry : Mod
    {
        // Acceso global estático
        public static IModHelper ModHelper;
        public static IMonitor StaticMonitor;

        // Instancias de nuestros sistemas
        private MonstrosityApi _apiInstance;
        private SpawnInjector _spawnInjector; 
        
        // Ruta para soporte de Content Patcher (Híbrido)
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            // 1. Configuración Global
            ModHelper = helper;
            StaticMonitor = Monitor;
            
            Monitor.Log("Iniciando Monstrosity Framework (Elite Edition)...", LogLevel.Info);

            // 2. Inicializar API (Seguro en Entry)
            _apiInstance = new MonstrosityApi(this.Monitor);

            // 3. Eventos Críticos
            helper.Events.GameLoop.GameLaunched += OnGameLaunched; 
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 4. Harmony
            try 
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);
                harmony.PatchAll(); 
                Monitor.Log("Harmony: Parches aplicados correctamente.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error CRÍTICO iniciando Harmony: {ex}", LogLevel.Error);
            }

            // 5. Comandos de Consola
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo custom. Uso: monster_spawn <id>", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos registrados.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga configuración.", ReloadMonsters);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // A. Integración SpaceCore (CRÍTICO PARA GUARDAR PARTIDA)
            // Intentamos registrar el serializador del CustomMonster
            try
            {
                // Si tienes referencia directa a SpaceCore:
                // SpaceCore.Api.RegisterSerializerType(typeof(CustomMonster));
                
                // Si usas tu Bridge:
                bool spaceCoreActive = SpaceCoreBridge.Init(ModHelper, this.Monitor);
                if (spaceCoreActive)
                {
                    Monitor.Log("Integración SpaceCore: ACTIVA (Serialización habilitada)", LogLevel.Info);
                }
                else
                {
                    Monitor.Log("ADVERTENCIA: SpaceCore no encontrado. Los monstruos no se guardarán al dormir.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error en integración SpaceCore: {ex.Message}", LogLevel.Error);
            }

            // B. Sistema de Spawns
            try
            {
                _spawnInjector = new SpawnInjector(this.Monitor);
                _spawnInjector.Register(ModHelper); 
                Monitor.Log("Sistema de Spawns: ACTIVO", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error iniciando SpawnInjector: {ex}", LogLevel.Error);
            }
        }

        public override object GetApi()
        {
            return _apiInstance ?? new MonstrosityApi(this.Monitor);
        }

        // --- CARGA DE DATOS ---

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(CpAssetPath))
            {
                e.LoadFrom(() => new Dictionary<string, MonsterData>(), AssetLoadPriority.Exclusive);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            ReloadMonsters("auto", null);
        }

        private void ReloadMonsters(string command, string[] args)
        {
            Monitor.Log("Iniciando carga de monstruos...", LogLevel.Info);
            MonsterRegistry.Clear(); 

            int totalLoaded = 0;

            // MODO 1: Content Packs
            foreach (IContentPack pack in ModHelper.ContentPacks.GetOwned())
            {
                try
                {
                    var packMonsters = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");
                    if (packMonsters != null)
                    {
                        foreach (var kvp in packMonsters)
                        {
                            MonsterRegistry.Register(kvp.Key, kvp.Value, pack, pack.Manifest);
                        }
                        totalLoaded += packMonsters.Count;
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"[PACK] Error leyendo {pack.Manifest.Name}: {ex.Message}", LogLevel.Error);
                }
            }

            // MODO 2: Content Patcher
            try 
            {
                var cpData = ModHelper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
                if (cpData != null && cpData.Count > 0)
                {
                    foreach (var kvp in cpData)
                    {
                        MonsterRegistry.Register($"CP.{kvp.Key}", kvp.Value, null, ModManifest);
                    }
                    totalLoaded += cpData.Count;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"[CP] Error cargando datos inyectados: {ex.Message}", LogLevel.Warn);
            }

            if (command != "auto") 
                Monitor.Log($"Recarga completada. Total: {totalLoaded}", LogLevel.Alert);
        }

        // --- COMANDOS DEBUG ---

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) 
            {
                Monitor.Log("Uso: monster_spawn <id>", LogLevel.Warn);
                return;
            }

            string id = args[0];
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Error: El ID '{id}' no existe.", LogLevel.Error);
                return;
            }

            try 
            {
                // Spawnear ligeramente a la derecha del jugador
                Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
                var monster = new CustomMonster(id, pos);
                Game1.currentLocation.characters.Add(monster);
                Monitor.Log($"Éxito: Spawneado '{id}'.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Excepción al spawnear: {ex}", LogLevel.Error);
            }
        }

        private void ListMonsters(string command, string[] args)
        {
            Monitor.Log("=== Registro de Monstruos ===", LogLevel.Info);
            var ids = MonsterRegistry.GetAllIds();
            
            if (!ids.Any()) 
            {
                Monitor.Log("(Registro vacío)", LogLevel.Warn);
                return;
            }

            foreach (var id in ids)
            {
                var monster = MonsterRegistry.Get(id);
                string source = monster.SourcePack != null ? $"[Pack: {monster.SourcePack.Manifest.Name}]" : "[CP/Global]";
                string behavior = monster.Data?.BehaviorType ?? "Default";
                Monitor.Log($"- {id} | {source} | IA: {behavior}", LogLevel.Info);
            }
        }
    }
}
