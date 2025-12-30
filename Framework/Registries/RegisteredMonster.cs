using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using StardewValley;

namespace MonstrosityFramework.Framework.Registries
{
    /// <summary>
    /// Wrapper que mantiene en memoria los datos y la textura cacheada de un monstruo.
    /// </summary>
    public class RegisteredMonster : IDisposable
    {
        public MonsterData Data { get; }
        public IContentPack SourcePack { get; }
        public IManifest OwnerManifest { get; }

        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        public RegisteredMonster(MonsterData data, IContentPack pack, IManifest manifest)
        {
            Data = data;
            SourcePack = pack;
            // Si no hay manifest explícito ni pack, asumimos que es huérfano (legacy CP)
            OwnerManifest = manifest ?? pack?.Manifest; 
        }

        public Texture2D GetTexture()
        {
            // 1. Caché
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            if (_hasTriedLoading) return null; // Ya falló antes, no reintentar (evita lag)

            _hasTriedLoading = true;

            // Validación básica
            if (string.IsNullOrEmpty(Data.TexturePath))
            {
                ModEntry.StaticMonitor.Log($"[Texture] Advertencia: El monstruo '{Data.DisplayName}' no tiene 'TexturePath' definido.", LogLevel.Warn);
                return null;
            }

            try
            {
                // CASO A: Content Pack (Mod propio)
                if (SourcePack != null)
                {
                    _textureCache = SourcePack.ModContent.Load<Texture2D>(Data.TexturePath);
                }
                // CASO B: Content Patcher / API Externa
                else
                {
                    // Intentamos cargar como asset del juego
                    _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                }

                return _textureCache;
            }
            catch (Exception ex)
            {
                string source = SourcePack != null ? SourcePack.Manifest.Name : "Content Patcher / Global";
                ModEntry.StaticMonitor.Log($"[Texture] Error cargando '{Data.TexturePath}' (Fuente: {source}).\nDetalles: {ex.Message}", LogLevel.Error);
                return null; // CustomMonster manejará el fallback
            }
        }

        public void Dispose()
        {
            _textureCache = null;
            _hasTriedLoading = false;
        }
    }
}