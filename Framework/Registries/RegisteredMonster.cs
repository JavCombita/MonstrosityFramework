using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using StardewValley;

namespace MonstrosityFramework.Framework.Registries
{
    /// <summary>
    /// Representa un monstruo cargado en memoria.
    /// Gestiona la carga segura de texturas para evitar cuadros invisibles.
    /// </summary>
    public class RegisteredMonster : IDisposable
    {
        public MonsterData Data { get; }
        public IContentPack SourcePack { get; }
        public IManifest OwnerManifest { get; }

        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        // Constructor Universal (usado por MonsterRegistry)
        public RegisteredMonster(MonsterData data, IContentPack pack, IManifest manifest)
        {
            Data = data;
            SourcePack = pack;
            OwnerManifest = manifest ?? pack?.Manifest; 
        }

        /// <summary>
        /// Obtiene la textura. Usa el método nativo de SMAPI para máxima compatibilidad.
        /// </summary>
        public Texture2D GetTexture()
        {
            // 1. Si ya está en caché, devolverla
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            
            // 2. Si ya falló antes, no insistir (ahorra lag)
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // CASO A: Content Pack (La forma correcta en SMAPI)
                if (SourcePack != null)
                {
                    // ModContent.Load maneja automáticamente rutas, mayúsculas y GPU.
                    _textureCache = SourcePack.ModContent.Load<Texture2D>(Data.TexturePath);
                }
                // CASO B: Legacy / Vanilla / Mods C# puros
                else
                {
                    _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                }

                return _textureCache;
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[Texture] ERROR FATAL: No se pudo cargar '{Data.TexturePath}'. Detalles: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public void Dispose()
        {
            // Liberamos la referencia. SMAPI gestiona la memoria real de las texturas cargadas.
            _textureCache = null;
            _hasTriedLoading = false;
        }
    }
}