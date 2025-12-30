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
            
            Monitor.Log("Iniciando Monstrosity Framework (Elite Architecture v1.0)...", LogLevel.Info);

            // 2. Inicializar API (Seguro en Entry)
            _apiInstance = new MonstrosityApi(this.Monitor);

            // 3. Eventos Críticos
            // GameLaunched: Para integraciones con otros mods (evita el error "Tried to access API before initialized")
            helper.Events.GameLoop.GameLaunched += OnGameLaunched; 
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 4. Harmony (Parches de seguridad y lógica)
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
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo custom.\nUso: monster_spawn <id>", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos registrados y su origen.", ListMonsters);
            helper.ConsoleCommands.Add("monster_reload", "Fuerza la recarga de Content Packs y datos.", ReloadMonsters);
        }

        // --- INICIALIZACIÓN DIFERIDA (Fix del Error Rojo) ---
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // A. Integración con SpaceCore (Skills / Serialización)
            // SpaceCoreBridge maneja internamente la verificación de si el mod existe.
            bool spaceCoreActive = SpaceCoreBridge.Init(ModHelper, this.Monitor);
            if (spaceCoreActive)
            {
                Monitor.Log("Integración SpaceCore: ACTIVA", LogLevel.Info);
            }

            // B. Sistema de Spawns (SpawnInjector)
            try
            {
                _spawnInjector = new SpawnInjector(this.Monitor);
                _spawnInjector.Register(ModHelper); // Se suscribe a Player.Warped
                Monitor.Log("Sistema de Spawns: ACTIVO (Monitoreando minas)", LogLevel.Info);
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
            // Provee el diccionario vacío para que Content Patcher tenga qué editar
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
            MonsterRegistry.Clear(); // Limpia caché anterior para recarga limpia

            int totalLoaded = 0;

            // ---------------------------------------------------------
            // MODO 1: CARGA NATIVA (ContentPackFor) - Para tu "Demo"
            // ---------------------------------------------------------
            // Busca carpetas en Mods/ que digan "ContentPackFor": { "UniqueID": "TuMod" }
            foreach (IContentPack pack in ModHelper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Leyendo Content Pack: {pack.Manifest.Name}", LogLevel.Trace);
                
                try
                {
                    // Intenta leer 'monsters.json'
                    var packMonsters = pack.ReadJsonFile<Dictionary<string, MonsterData>>("monsters.json");

                    if (packMonsters != null)
                    {
                        foreach (var kvp in packMonsters)
                        {
                            // IMPORTANTE: Pasamos 'pack' para que RegisteredMonster pueda cargar 
                            // las texturas (assets/xyz.png) desde dentro de ESE mod.
                            MonsterRegistry.Register(kvp.Key, kvp.Value, pack, pack.Manifest);
                        }
                        Monitor.Log($"[PACK] {pack.Manifest.Name}: +{packMonsters.Count} monstruos.", LogLevel.Info);
                        totalLoaded += packMonsters.Count;
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"[PACK] Error leyendo {pack.Manifest.Name}: {ex.Message}", LogLevel.Error);
                }
            }

            // ---------------------------------------------------------
            // MODO 2: CONTENT PATCHER (EditData) - Para usuarios externos
            // ---------------------------------------------------------
            try 
            {
                var cpData = ModHelper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
                if (cpData != null && cpData.Count > 0)
                {
                    foreach (var kvp in cpData)
                    {
                        // "CP." prefijo técnico, pack = null porque las texturas vienen por ruta global
                        MonsterRegistry.Register($"CP.{kvp.Key}", kvp.Value, null, ModManifest);
                    }
                    Monitor.Log($"[CP] Content Patcher: +{cpData.Count} monstruos.", LogLevel.Info);
                    totalLoaded += cpData.Count;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"[CP] Error cargando datos inyectados: {ex.Message}", LogLevel.Warn);
            }

            // Resumen
            if (command != "auto") 
            {
                Monitor.Log($"Recarga completada. Total monstruos activos: {totalLoaded}", LogLevel.Alert);
            }
            else if (totalLoaded == 0)
            {
                Monitor.Log("No se encontraron monstruos. Asegúrate de tener instalado 'Monstrosity Demo' o configurar Content Patcher.", LogLevel.Warn);
            }
        }

        // --- COMANDOS DEBUG ---

        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) 
            {
                Monitor.Log("Error: Debes estar en partida. Uso: monster_spawn <id>", LogLevel.Warn);
                return;
            }

            string id = args[0];
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Error: El ID '{id}' no existe en el registro.", LogLevel.Error);
                return;
            }

            try 
            {
                Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
                var monster = new CustomMonster(id, pos);
                Game1.currentLocation.characters.Add(monster);
                Monitor.Log($"Éxito: Spawneado '{id}' en {pos}.", LogLevel.Info);
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
                // Determinar fuente para mostrar al usuario
                string source = monster.SourcePack != null 
                    ? $"[Pack: {monster.SourcePack.Manifest.Name}]" 
                    : "[CP/Global]";
                
                string behavior = monster.Data?.BehaviorType ?? "Default";
                
                Monitor.Log($"- {id} | {source} | IA: {behavior}", LogLevel.Info);
            }
        }
    }
}