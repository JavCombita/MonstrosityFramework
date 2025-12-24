using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using System.Collections.Generic;
using System.IO;
using StardewValley;

namespace MonstrosityFramework.Framework.Registries
{
    /// <summary>
    /// Wrapper que gestiona los assets y la memoria de cada monstruo.
    /// Implementa IDisposable para limpieza manual de la VRAM.
    /// </summary>
    public class RegisteredMonster : IDisposable
    {
        public MonsterData Data { get; }
        public IManifest OwnerMod { get; }
        
        // Cache de la textura para no cargarla 60 veces por segundo
        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        public RegisteredMonster(IManifest owner, MonsterData data)
        {
            OwnerMod = owner;
            Data = data;
        }

        /// <summary>
        /// Carga la textura bajo demanda. 
        /// Soporta tanto mods C# (usando OwnerMod) como Content Packs (usando Data.ContentPackID).
        /// </summary>
        public Texture2D GetTexture()
        {
            // 1. Si ya está en cache y es válida, retornarla.
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            
            // 2. Si ya intentamos cargar y falló anteriormente, no reintentar (Ahorro de CPU).
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // 3. Determinar el ID del mod que contiene la textura.
                // Si Data.ContentPackID tiene valor (viene de Content Patcher), usamos ese.
                // Si no, usamos el ID del mod que registró el monstruo (OwnerMod).
                string targetModId = !string.IsNullOrEmpty(Data.ContentPackID) 
                                     ? Data.ContentPackID 
                                     : OwnerMod.UniqueID;

                IModInfo modInfo = ModEntry.ModHelper.ModRegistry.Get(targetModId);
                
                if (modInfo == null)
                {
                    ModEntry.StaticMonitor.Log($"[MonsterRegistry] No se encuentra el mod origen: {targetModId}", LogLevel.Error);
                    return null;
                }

                // 4. Construir ruta absoluta
                string fullPath = Path.Combine(modInfo.LocalAppPath, Data.TexturePath);

                if (!File.Exists(fullPath))
                {
                    ModEntry.StaticMonitor.Log($"[MonsterRegistry] Textura no encontrada: {fullPath}", LogLevel.Warn);
                    return null;
                }

                // 5. Cargar Texture2D desde Stream
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    _textureCache = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                }

                // Parche visual: Nombre para debug
                _textureCache.Name = $"{targetModId}.{Data.TexturePath}";
                
                return _textureCache;
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"Error cargando textura para {Data.DisplayName}: {ex.Message}", LogLevel.Error);
                return null; // El monstruo será invisible o usará fallback.
            }
        }

        /// <summary>
        /// Libera la memoria de la GPU inmediatamente.
        /// </summary>
        public void Dispose()
        {
            if (_textureCache != null && !_textureCache.IsDisposed)
            {
                _textureCache.Dispose();
                _textureCache = null;
            }
            // Reseteamos el flag para permitir recarga si el jugador vuelve a entrar al mundo
            _hasTriedLoading = false;
        }
    }

    /// <summary>
    /// Almacén estático de todos los monstruos del juego.
    /// </summary>
    public static class MonsterRegistry
    {
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Sobreescribiendo definición: {uniqueId}", LogLevel.Warn);
            }
            _registry[uniqueId] = monster;
        }

        public static RegisteredMonster Get(string uniqueId)
        {
            return _registry.TryGetValue(uniqueId, out var monster) ? monster : null;
        }

        public static bool IsRegistered(string uniqueId) => _registry.ContainsKey(uniqueId);
        
        public static IEnumerable<string> GetAllIds() => _registry.Keys;
        
        public static IEnumerable<RegisteredMonster> GetAll() => _registry.Values;

        /// <summary>
        /// Limpieza masiva de texturas para evitar Memory Leaks.
        /// SE DEBE LLAMAR EN: GameLoop.ReturnedToTitle
        /// </summary>
        public static void Cleanup()
        {
            int count = 0;
            foreach (var monster in _registry.Values)
            {
                if (monster != null)
                {
                    monster.Dispose();
                    count++;
                }
            }
            ModEntry.StaticMonitor.Log($"[MonsterRegistry] Memoria liberada de {count} texturas de monstruos.", LogLevel.Info);
        }
    }
}