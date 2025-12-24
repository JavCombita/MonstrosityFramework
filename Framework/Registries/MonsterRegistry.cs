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
    /// Clase contenedora que envuelve los datos crudos y gestiona los assets.
    /// </summary>
    public class RegisteredMonster
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
        /// Carga la textura bajo demanda desde la carpeta del mod propietario.
        /// </summary>
        public Texture2D GetTexture()
        {
            // 1. Si ya está en cache, retornarla.
            if (_textureCache != null) return _textureCache;
            
            // 2. Si ya intentamos cargar y falló, no reintentar infinitamente (Performance).
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // 3. Localizar la carpeta del mod hijo
                IModInfo modInfo = ModEntry.ModHelper.ModRegistry.Get(OwnerMod.UniqueID);
                if (modInfo == null)
                {
                    ModEntry.StaticMonitor.Log($"No se encuentra el mod propietario: {OwnerMod.UniqueID}", LogLevel.Error);
                    return null;
                }

                // 4. Construir ruta absoluta (Cross-Platform safe)
                string fullPath = Path.Combine(modInfo.LocalAppPath, Data.TexturePath);

                if (!File.Exists(fullPath))
                {
                    ModEntry.StaticMonitor.Log($"Textura no encontrada: {fullPath}", LogLevel.Error);
                    return null;
                }

                // 5. Cargar Texture2D desde Stream (Compatible con Android/PC)
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    _textureCache = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                }

                // Parche visual: Si es pixel art, establecer nombre para debug
                _textureCache.Name = $"{OwnerMod.UniqueID}.{Data.TexturePath}";
                
                return _textureCache;
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"Error cargando textura para {Data.DisplayName}: {ex.Message}", LogLevel.Error);
                return null; // El monstruo será invisible o usará fallback, pero no crasheará.
            }
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
                ModEntry.StaticMonitor.Log($"Sobreescribiendo definición de monstruo: {uniqueId}", LogLevel.Warn);
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
    }
}