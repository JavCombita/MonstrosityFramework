using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using StardewValley;

namespace MonstrosityFramework.Framework.Registries
{
    public class RegisteredMonster : IDisposable
    {
        public MonsterData Data { get; }
        public IContentPack SourcePack { get; }
        public IManifest OwnerManifest { get; }

        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        public RegisteredMonster(IContentPack pack, MonsterData data)
        {
            SourcePack = pack;
            OwnerManifest = pack.Manifest;
            Data = data;
        }

        public RegisteredMonster(IManifest owner, MonsterData data)
        {
            SourcePack = null;
            OwnerManifest = owner;
            Data = data;
        }

        public Texture2D GetTexture()
        {
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // CASO A: Content Pack (Sistema SMAPI 4.0)
                if (SourcePack != null)
                {
                    [cite_start]// CORRECCIÓN: Usamos ModContent.Load<T> como indica la documentación 
                    ModEntry.StaticMonitor.Log($"[Texture] Cargando '{Data.TexturePath}' desde pack '{SourcePack.Manifest.Name}'", LogLevel.Trace);
                    
                    _textureCache = SourcePack.ModContent.Load<Texture2D>(Data.TexturePath);
                    
                    if (_textureCache != null)
                        _textureCache.Name = $"{OwnerManifest.UniqueID}/{Data.TexturePath}";
                }
                // CASO B: Legacy / Vanilla (Carga vía Game Content)
                else
                {
                    ModEntry.StaticMonitor.Log($"[Texture] Cargando '{Data.TexturePath}' via Game Content", LogLevel.Trace);
                    _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                }

                return _textureCache;
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[Texture] Error cargando textura '{Data.TexturePath}': {ex.Message}", LogLevel.Error);
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
}