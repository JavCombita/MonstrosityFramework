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

namespace MonstrosityFramework
{
    public class ModEntry : Mod
    {
        // Acceso global estático (útil para debug y parches)
        public static IModHelper ModHelper;
        public static IMonitor StaticMonitor;

        // Instancias de nuestros sistemas
        private MonstrosityApi _apiInstance;
        private SpawnInjector _spawnInjector; 
        
        // Ruta para Content Patcher
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            // 1. Configuración Inicial
            ModHelper = helper;
            StaticMonitor = Monitor;
            
            Monitor.Log("Iniciando Monstrosity Framework (Elite Architecture)...", LogLevel.Info);

            // 2. Inicializar API (Pasando Monitor como pide tu constructor)
            _apiInstance = new MonstrosityApi(this.Monitor);

            // 3. HARMONY (DebrisSafetyPatch y otros parches)
            try 
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);
                harmony.PatchAll(); 
                Monitor.Log("Harmony: Parches aplicados correctamente.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Harmony Error: {ex}", LogLevel.Error);
            }

            // 4. SPAWN INJECTOR (Lógica basada en tu archivo subido)
            try
            {
                // Instanciamos pasando el Monitor
                _spawnInjector = new SpawnInjector(this.Monitor);
                // Registramos los eventos (Player.Warped, etc.)
                _spawnInjector.Register(helper);
                
                Monitor.Log("Sistema de Spawns: ACTIVO (Monitoreando Warps)", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error iniciando SpawnInjector: {ex}", LogLevel.Error);
            }

            // 5. SPACECORE BRIDGE (Lógica basada en tu archivo subido)
            // SpaceCoreBridge.Init ya verifica internamente si el mod está cargado.
            bool spaceCoreLoaded = SpaceCoreBridge.Init(helper, this.Monitor);
            if (spaceCoreLoaded)
            {
                Monitor.Log("Integración SpaceCore: ACTIVA (Serialización habilitada)", LogLevel.Info);
            }

            // 6. EVENTOS DEL FRAMEWORK
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 7. COMANDOS DE CONSOLA (Herramientas de Debug)
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo custom.\nUso: monster_spawn <id>", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos registrados.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Recarga los datos de monstruos desde Content Patcher.", ReloadMonsters);
        }

        // --- EXPOSICIÓN DE API PÚBLICA ---
        public override object GetApi()
        {
            return _apiInstance ?? new MonstrosityApi(this.Monitor);
        }

        // --- EVENTOS ---

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Proveer diccionario vacío para que Content Patcher pueda editarlo sin errores
            if (e.Name.IsEquivalentTo(CpAssetPath))
            {
                e.LoadFrom(() => new Dictionary<string, MonsterData>(), AssetLoadPriority.Exclusive);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Cargar monstruos al entrar al save
            ReloadMonsters("auto", null);
        }

        // --- LÓGICA DE RECARGA Y DEBUG ---

        private void ReloadMonsters(string command, string[] args)
        {
            Monitor.Log("Recargando registro de monstruos...", LogLevel.Info);
            MonsterRegistry.Clear();

            // Cargar datos inyectados por Content Patcher
            var cpData = ModHelper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
            if (cpData != null)
            {
                foreach (var kvp in cpData)
                {
                    // Registramos usando el Manifest del framework como "owner" técnico
                    MonsterRegistry.Register($"CP.{kvp.Key}", kvp.Value, null, ModManifest);
                }
                Monitor.Log($"Se cargaron {cpData.Count} monstruos desde Content Patcher.", LogLevel.Info);
            }
            
            if (command != "auto") Monitor.Log("Recarga manual finalizada.", LogLevel.Alert);
        }

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) 
            {
                Monitor.Log("Debes estar en una partida. Uso: monster_spawn <id>", LogLevel.Warn);
                return;
            }

            string id = args[0];
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Error: El monstruo '{id}' no existe en el registro.", LogLevel.Error);
                return;
            }

            try 
            {
                Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
                // Usamos CustomMonster que ahora delega su lógica al BehaviorFactory
                var monster = new CustomMonster(id, pos);
                Game1.currentLocation.characters.Add(monster);
                Monitor.Log($"Spawneado '{id}' en {pos}.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Excepción al spawnear: {ex}", LogLevel.Error);
            }
        }

        private void ListMonsters(string command, string[] args)
        {
            Monitor.Log("=== Monstruos Registrados ===", LogLevel.Info);
            var ids = MonsterRegistry.GetAllIds();
            
            if (!ids.Any()) 
            {
                Monitor.Log("(Ninguno)", LogLevel.Warn);
                return;
            }

            foreach (var id in ids)
            {
                var monster = MonsterRegistry.Get(id);
                string source = monster.SourcePack != null ? $"[Pack: {monster.SourcePack.Manifest.Name}]" : "[CP/API]";
                // Añadido null-check seguro para BehaviorType
                string behavior = monster.Data?.BehaviorType ?? "Default";
                Monitor.Log($"- {id} | Fuente: {source} | IA: {behavior}", LogLevel.Info);
            }
        }
    }
}