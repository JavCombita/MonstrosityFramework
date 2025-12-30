using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;
using System;
using System.IO; 
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

        // Constructor para Content Packs (Existente)
        public RegisteredMonster(IContentPack pack, MonsterData data)
        {
            SourcePack = pack;
            OwnerManifest = pack.Manifest;
            Data = data;
        }

        // Constructor para Mods C# (Existente)
        public RegisteredMonster(IManifest owner, MonsterData data)
        {
            SourcePack = null;
            OwnerManifest = owner;
            Data = data;
        }

        // --- NUEVO CONSTRUCTOR (Soluciona el error CS1729) ---
        // Este es el que está llamando MonsterRegistry.cs
        public RegisteredMonster(MonsterData data, IContentPack pack, IManifest manifest)
        {
            Data = data;
            SourcePack = pack;
            // Si pasan un manifiesto explícito úsalo, si no, intenta sacar el del pack
            OwnerManifest = manifest ?? pack?.Manifest; 
        }

        public Texture2D GetTexture()
        {
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // CASO A: Content Pack (Carga física segura)
                if (SourcePack != null)
                {
                    string fullPath = Path.Combine(SourcePack.DirectoryPath, Data.TexturePath);
                    if (File.Exists(fullPath))
                    {
                        using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                        {
                            _textureCache = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                            _textureCache.Name = $"{OwnerManifest.UniqueID}/{Data.TexturePath}";
                        }
                    }
                    else
                    {
                        ModEntry.StaticMonitor.Log($"[Texture] Archivo no encontrado: {fullPath}", LogLevel.Error);
                    }
                }
                // CASO B: Legacy / Vanilla (Carga vía Pipeline)
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