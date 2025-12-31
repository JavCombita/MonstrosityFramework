using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities.Behaviors;

namespace MonstrosityFramework.Entities
{
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        // --- SINCRONIZACIÓN DE RED ---
        public readonly NetString MonsterSourceId = new();
        
        // --- ESTADO PÚBLICO (Vital para los Behaviors) ---
        [XmlIgnore] public float StateTimer = 0f;
        [XmlIgnore] public int AIState = 0; 
        [XmlIgnore] public bool IsInvincibleOverride = false;
        
        // Variables auxiliares genéricas (por si algún behavior complejo las necesita)
        [XmlIgnore] public float GenericTimer = 0f; 
        [XmlIgnore] public bool GenericFlag = false;
        
        // --- CEREBRO ---
        private MonsterBehavior _currentBehavior; 
        private string _cachedBehaviorId = "default";
        
        // --- CONTROL INTERNO ---
        private bool _hasLoadedData = false;
        private bool _textureChecked = false;

        public CustomMonster() : base() { EnsureFallbackTexture(); }

        public CustomMonster(string uniqueId, Vector2 position) : base()
        {
            this.MonsterSourceId.Value = uniqueId;
            this.Position = position;
            this.ReloadData();
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            this.NetFields.AddField(MonsterSourceId);
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => { 
                _hasLoadedData = false; 
                ReloadData(); 
            };
        }

        public void ReloadData()
        {
            _hasLoadedData = true;
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null) { EnsureFallbackTexture(); return; }

            // 1. ASIGNAR ESTRATEGIA (Behavior)
            _cachedBehaviorId = (entry.Data.BehaviorType ?? "Default").ToLowerInvariant();
            _currentBehavior = BehaviorFactory.GetBehavior(_cachedBehaviorId);

            // 2. STATS
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            // Solo curar si es necesario (evita resetear HP en combate multiplayer si recarga)
            if (this.Health > this.MaxHealth || this.Health <= 0) this.Health = this.MaxHealth; 
            
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            this.resilience.Value = entry.Data.Defense;
            if (this.Speed != entry.Data.Speed) this.Speed = entry.Data.Speed;
            this.willDestroyObjectsUnderfoot = false;

            // 3. SPRITE
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            try
            {
                Texture2D customTex = entry.GetTexture();
                if (customTex != null && !customTex.IsDisposed) 
                {
                    this.Sprite.spriteTexture = customTex;
                }
                else 
                {
                    EnsureFallbackTexture();
                }
            }
            catch (Exception) { EnsureFallbackTexture(); }
            
            // --- FIX GRÁFICO #1: Inicialización Correcta ---
            // Esto evita que el sprite se vea "recortado" o invisible hasta que se mueva.
            this.Sprite.currentFrame = 0;
            this.Sprite.UpdateSourceRect(); 

            this.HideShadow = false;
            
            // Reiniciar estado IA
            this.AIState = 0;
            this.StateTimer = 0;
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            // Seguridad de Textura
            if (!_textureChecked)
            {
                _textureChecked = true;
                if (this.Sprite?.spriteTexture == null) EnsureFallbackTexture();
            }

            // Seguridad de Datos
            if (!_hasLoadedData) ReloadData();

            // Seguridad de Posición (NaN fix)
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y)) {
                this.Position = Game1.player.Position + new Vector2(64, 64);
                this.xVelocity = 0; this.yVelocity = 0;
            }

            // Delegación al Behavior
            if (_currentBehavior != null)
            {
                _currentBehavior.Update(this, time);
            }
            else
            {
                base.behaviorAtGameTick(time); 
            }
        }

        // --- FIX GRÁFICO #2: Dibujado Simplificado ---
        public override void draw(SpriteBatch b)
        {
            // Si es un volador (Bat, Ghost, etc.), ajustamos el LayerDepth.
            if (this.isGlider.Value && !this.IsInvisible)
            {
                // Calculamos un LayerDepth alto (+640f) para asegurar que se dibuje ENCIMA
                // de las paredes de la mina y no parezca que está "dentro" del muro.
                float layerDepth = (this.Position.Y + 640f) / 10000f;

                // Dibujamos el sprite en su posición real.
                // Eliminamos los offsets manuales (-32f) y la sombra dibujada a mano
                // para evitar conflictos visuales.
                this.Sprite.draw(b, Game1.GlobalToLocal(Game1.viewport, this.Position), layerDepth);
            }
            else
            {
                // Para monstruos terrestres, usamos el método vanilla que maneja sombras y Z-order correctamente.
                base.draw(b);
            }
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            if (_currentBehavior != null)
            {
                int modifiedDamage = _currentBehavior.OnTakeDamage(this, damage, isBomb, who);
                if (modifiedDamage <= 0 && damage > 0) return 0; // Daño anulado por Behavior
                damage = modifiedDamage;
            }

            return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
        }

        public override bool isInvincible()
        {
            return IsInvincibleOverride || base.isInvincible();
        }

        public override List<Item> getExtraDropItems()
        {
            var drops = new List<Item>();
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return drops;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry != null && entry.Data.Drops != null)
            {
                foreach (var dropData in entry.Data.Drops)
                {
                    if (Game1.random.NextDouble() <= dropData.Chance)
                        drops.Add(ItemRegistry.Create(dropData.ItemId, 1));
                }
            }
            return drops;
        }

        private void EnsureFallbackTexture()
        {
            if (this.Sprite == null) this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
            
            if (this.Sprite.spriteTexture == null || this.Sprite.spriteTexture.IsDisposed)
            {
                try { this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); } 
                catch { }
            }
        }
    }
}