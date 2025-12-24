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
        
        // RUTA MÁGICA: Los usuarios de Content Patcher usarán esto en "Target"
        private const string CpAssetPath = "Mods/JavCombita/Monstrosity/Data";

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            StaticMonitor = Monitor;
            _apiInstance = new MonstrosityApi(Monitor);

            // 1. Eventos Core
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            
            // 2. Sistema de Assets Virtuales (Content Patcher Bridge)
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // 3. Spawner
            var spawner = new SpawnInjector(Monitor);
            spawner.Register(helper);

            // 4. Comandos
            helper.ConsoleCommands.Add("monster_spawn", "Spawnea un monstruo por ID.", SpawnDebugMonster);
            helper.ConsoleCommands.Add("monster_list", "Lista todos los monstruos.", ListMonsters);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Inicializar SpaceCore
            bool spaceCoreLoaded = SpaceCoreBridge.Init(Helper, Monitor);
            if (!spaceCoreLoaded)
            {
                Monitor.Log("SpaceCore no encontrado. El guardado de monstruos custom fallará.", LogLevel.Alert);
            }

            // CARGAR DATOS DE CONTENT PATCHER
            // Esto fuerza a SMAPI a cargar nuestro asset virtual. Si hay Content Packs, se aplican aquí.
            try
            {
                var cpData = Helper.GameContent.Load<Dictionary<string, MonsterData>>(CpAssetPath);
                
                if (cpData != null && cpData.Count > 0)
                {
                    Monitor.Log($"Cargando {cpData.Count} monstruos desde Content Patcher...", LogLevel.Info);
                    foreach (var kvp in cpData)
                    {
                        // Registramos usando el Manifest de ESTE mod framework, ya que técnicamente
                        // es el framework quien carga los datos, pero mantenemos el ID del JSON.
                        // Nota: Para trazabilidad perfecta, Content Patcher no nos da el ID del mod origen fácilmente
                        // en este punto, así que usaremos "ContentPatcher" como prefijo implícito o el ID del framework.
                        _apiInstance.RegisterMonster(this.ModManifest, kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error cargando datos de Content Patcher: {ex.Message}", LogLevel.Error);
            }
        }

        // Proveedor del Asset Virtual
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(CpAssetPath))
            {
                // Entregamos un diccionario vacío listo para ser parcheado por CP
                e.LoadFrom(() => new Dictionary<string, MonsterData>(), AssetLoadPriority.Exclusive);
            }
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            // Limpieza de memoria (VRAM)
            MonsterRegistry.Cleanup();
        }

        public override object GetApi() => _apiInstance;

        // --- COMANDOS (Sin cambios mayores, solo referencias) ---
        private void SpawnDebugMonster(string command, string[] args)
        {
            if (!Context.IsWorldReady || args.Length < 1) return;
            string id = args[0];
            
            if (!MonsterRegistry.IsRegistered(id))
            {
                Monitor.Log($"Monstruo '{id}' no encontrado.", LogLevel.Error);
                return;
            }

            Vector2 pos = Game1.player.Position + new Vector2(64, 0); 
            Game1.currentLocation.characters.Add(new CustomMonster(id, pos));
            Monitor.Log($"Spawneado {id}.", LogLevel.Info);
        }

        private void ListMonsters(string command, string[] args)
        {
            foreach (var id in MonsterRegistry.GetAllIds())
            {
                Monitor.Log($"- {id}", LogLevel.Info);
            }
        }
    }
}