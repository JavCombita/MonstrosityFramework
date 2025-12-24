using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using System.Collections.Generic;
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

            try
            {
                // CORRECCIÓN MAYOR: Ya no usamos FileStream ni LocalAppPath.
                // Usamos el Content Pipeline del juego.
                // Content Patcher mapea archivos a rutas virtuales, así que esto funcionará
                // si el TexturePath es una ruta válida del juego (ej: "Mods/MyMod/Sprite").
                
                // Si el path no tiene extensión o parece un Asset Key, probamos cargarlo.
                // Nota: Si el usuario pone "assets/sprite.png", esto fallará a menos que el mod hijo
                // use Content Patcher para cargar ese archivo como Asset.
                
                _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                _textureCache.Name = Data.TexturePath;
                
                return _textureCache;
            }
            catch (Exception ex)
            {
                // Fallback para C# Mods (Si pasan ruta local):
                // En el futuro, deberíamos pedirles el Texture2D directamente en la API.
                ModEntry.StaticMonitor.Log($"No se pudo cargar textura '{Data.TexturePath}'. Asegúrate de que sea un Asset Key válido o cargado por Content Patcher. Error: {ex.Message}", LogLevel.Warn);
                return null;
            }
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