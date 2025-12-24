using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection; // <--- Necesario para el truco de Reflection
using StardewValley;

namespace MonstrosityFramework.Framework.Registries
{
    public class RegisteredMonster : IDisposable
    {
        public MonsterData Data { get; }
        public IManifest OwnerMod { get; }
        
        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        public RegisteredMonster(IManifest owner, MonsterData data)
        {
            OwnerMod = owner;
            Data = data;
        }

        public Texture2D GetTexture()
        {
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            // 1. INTENTO: Carga vía Content Pipeline (Ideal para Content Patcher)
            try
            {
                // Si el path no tiene extensión (.png) o es una ruta virtual, esto funcionará.
                _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                _textureCache.Name = Data.TexturePath;
                return _textureCache;
            }
            catch 
            {
                // Si falla (ej: es un archivo físico local no registrado), pasamos al intento 2.
            }

            // 2. INTENTO: Carga física directa (Para Mods C# como el DemoPack)
            try
            {
                string targetModId = !string.IsNullOrEmpty(Data.ContentPackID) 
                                     ? Data.ContentPackID 
                                     : OwnerMod.UniqueID;

                IModInfo modInfo = ModEntry.ModHelper.ModRegistry.Get(targetModId);
                
                if (modInfo != null)
                {
                    // --- TRUCO DE REFLECTION ---
                    // IModInfo oculta la ruta física por seguridad, pero la implementación interna la tiene.
                    // Usamos Reflection para obtener la propiedad "DirectoryPath".
                    PropertyInfo pathProp = modInfo.GetType().GetProperty("DirectoryPath");
                    string modPath = pathProp?.GetValue(modInfo) as string;

                    if (!string.IsNullOrEmpty(modPath))
                    {
                        string fullPath = Path.Combine(modPath, Data.TexturePath);
                        if (File.Exists(fullPath))
                        {
                            using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                            {
                                _textureCache = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                                _textureCache.Name = $"{targetModId}/{Data.TexturePath}";
                                return _textureCache;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Fallo total cargando textura '{Data.TexturePath}': {ex.Message}", LogLevel.Error);
            }

            return null;
        }

        public void Dispose()
        {
            if (_textureCache != null && !_textureCache.IsDisposed)
            {
                _textureCache.Dispose();
                _textureCache = null;
            }
            _hasTriedLoading = false;
        }
    }

    public static class MonsterRegistry
    {
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Sobreescribiendo ID: {uniqueId}", LogLevel.Warn);
            }
            _registry[uniqueId] = monster;
        }

        public static RegisteredMonster Get(string uniqueId) => _registry.TryGetValue(uniqueId, out var m) ? m : null;
        public static bool IsRegistered(string uniqueId) => _registry.ContainsKey(uniqueId);
        public static IEnumerable<string> GetAllIds() => _registry.Keys;
        public static IEnumerable<RegisteredMonster> GetAll() => _registry.Values;

        public static void Cleanup()
        {
            foreach (var monster in _registry.Values)
            {
                monster?.Dispose();
            }
        }
    }
}
