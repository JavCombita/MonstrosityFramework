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
        
        // Propiedad pública para saber de dónde vino (útil para debug)
        public IContentPack SourcePack { get; }
        
        // El manifest del mod que lo creó (sea un pack o el framework mismo)
        public IManifest OwnerManifest { get; }

        private Texture2D _textureCache;
        private bool _hasTriedLoading = false;

        /// <summary>
        /// Constructor para el sistema moderno de Content Packs.
        /// </summary>
        public RegisteredMonster(IContentPack pack, MonsterData data)
        {
            SourcePack = pack;
            OwnerManifest = pack.Manifest;
            Data = data;
        }

        /// <summary>
        /// Constructor legacy/fallback para Content Patcher o inyecciones directas.
        /// </summary>
        public RegisteredMonster(IManifest owner, MonsterData data)
        {
            SourcePack = null;
            OwnerManifest = owner;
            Data = data;
        }

        public Texture2D GetTexture()
        {
            // 1. Cache hit
            if (_textureCache != null && !_textureCache.IsDisposed) return _textureCache;
            
            // Si ya intentamos cargar y falló, no spammeamos logs de error, devolvemos null y dejamos que CustomMonster use el fallback.
            if (_hasTriedLoading) return null; 

            _hasTriedLoading = true;

            try
            {
                // CASO A: Viene de un Content Pack (Sistema Nuevo)
                if (SourcePack != null)
                {
                    // LoadAsset maneja automáticamente rutas relativas y extensiones.
                    // Data.TexturePath debe ser relativo a la carpeta del pack (ej: "assets/sprites/ghost.png")
                    ModEntry.StaticMonitor.Log($"[Texture] Cargando '{Data.TexturePath}' desde pack '{SourcePack.Manifest.Name}'", LogLevel.Trace);
                    _textureCache = SourcePack.LoadAsset<Texture2D>(Data.TexturePath);
                }
                // CASO B: Sistema Legacy (Content Patcher o XNB Vanilla)
                else
                {
                    // Asumimos que es un asset del juego o una ruta virtual de CP
                    ModEntry.StaticMonitor.Log($"[Texture] Cargando '{Data.TexturePath}' via Game Content Pipeline", LogLevel.Trace);
                    _textureCache = Game1.content.Load<Texture2D>(Data.TexturePath);
                }

                if (_textureCache != null)
                {
                    // Ponerle nombre ayuda al debug visual
                    _textureCache.Name = $"{OwnerManifest.UniqueID}/{Data.TexturePath}";
                }

                return _textureCache;
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[Texture] Error CRÍTICO cargando textura '{Data.TexturePath}' para '{Data.DisplayName}': {ex.Message}", LogLevel.Error);
                return null; // CustomMonster manejará esto poniéndole sprite de Shadow Brute
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