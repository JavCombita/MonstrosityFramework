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
        // --- SINCRONIZACIÓN DE RED (Vital para Multiplayer) ---
        public readonly NetString MonsterSourceId = new();
        
        // Variables genéricas sincronizadas para estados visuales (Ej: Cangrejo escondido)
        public readonly NetInt NetState = new NetInt(0); 
        public readonly NetBool NetFlag = new NetBool(false);
        public readonly NetFloat NetTimer = new NetFloat(0f);

        // --- MEMORIA LOCAL (Cerebro del Behavior) ---
        // Variables matemáticas (contadores, timers, flags internos)
        [XmlIgnore] public Dictionary<string, float> LocalData = new Dictionary<string, float>();
        
        // Referencias a objetos (Pareja, Luz, Target) - NO se guardan en el XML
        [XmlIgnore] public Dictionary<string, object> LocalObjects = new Dictionary<string, object>();

        // --- CEREBRO ---
        private MonsterBehavior _currentBehavior; 
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
            this.NetFields.AddField(MonsterSourceId)
                          .AddField(NetState)
                          .AddField(NetFlag)
                          .AddField(NetTimer);
            
            // Si el ID cambia (ej: sync inicial), recargamos datos
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => { _hasLoadedData = false; ReloadData(); };
        }

        // --- MÉTODOS DE MEMORIA (Sugar Syntax) ---
        public float GetVar(string key, float def = 0f) => LocalData.ContainsKey(key) ? LocalData[key] : def;
        public void SetVar(string key, float val) => LocalData[key] = val;
        public void ModVar(string key, float delta) { if (!LocalData.ContainsKey(key)) LocalData[key] = 0; LocalData[key] += delta; }
        public bool HasVar(string key) => LocalData.ContainsKey(key);

        public T GetObj<T>(string key) where T : class 
        {
            if (LocalObjects.TryGetValue(key, out object val) && val is T tVal) return tVal;
            return null;
        }
        public void SetObj(string key, object val) => LocalObjects[key] = val;
        public void RemoveObj(string key) => LocalObjects.Remove(key);

        // --- CARGA DE DATOS ---
        public void ReloadData()
        {
            _hasLoadedData = true;
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null) { EnsureFallbackTexture(); return; }

            // 1. Configurar Behavior
            string bId = (entry.Data.BehaviorType ?? "Default").ToLowerInvariant();
            _currentBehavior = BehaviorFactory.GetBehavior(bId);

            // 2. Stats
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            // Solo curar si está corrupto o es nuevo spawn
            if (this.Health > this.MaxHealth || this.Health <= 0) this.Health = this.MaxHealth; 
            
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            this.resilience.Value = entry.Data.Defense;
            this.Speed = entry.Data.Speed;
            
            // 3. Sprite
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            try
            {
                Texture2D customTex = entry.GetTexture();
                if (customTex != null && !customTex.IsDisposed) this.Sprite.spriteTexture = customTex;
                else EnsureFallbackTexture();
            }
            catch { EnsureFallbackTexture(); }
            
            this.Sprite.UpdateSourceRect(); 
            this.HideShadow = false;

            // 4. Inicializar Behavior Específico
            _currentBehavior?.Initialize(this);
        }

        // --- CICLO DE JUEGO ---

        public override void behaviorAtGameTick(GameTime time)
        {
            if (!_textureChecked) { _textureChecked = true; if (this.Sprite?.spriteTexture == null) EnsureFallbackTexture(); }
            if (!_hasLoadedData) ReloadData();
            
            // Fix: Posición NaN causa crash
            if (float.IsNaN(this.Position.X)) { this.Position = Game1.player.Position; }

            if (_currentBehavior != null)
                _currentBehavior.Update(this, time);
            else
                base.behaviorAtGameTick(time); 
        }

        protected override void updateAnimation(GameTime time)
        {
            if (_currentBehavior != null) 
                 _currentBehavior.OnUpdateAnimation(this, time);
            base.updateAnimation(time);
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            int finalDamage = damage;
            if (_currentBehavior != null)
                finalDamage = _currentBehavior.OnTakeDamage(this, damage, isBomb, who);
            
            // Si el behavior devuelve < 0, significa "cancelar daño/invulnerable"
            if (finalDamage < 0) return -1;
            
            return base.takeDamage(finalDamage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
        }

        public override void deathAnimation()
        {
            if (_currentBehavior != null)
                _currentBehavior.OnDeath(this);
            base.deathAnimation();
        }

        // --- RENDERIZADO (Con soporte para Tint) ---
        public override void draw(SpriteBatch b)
        {
            if (this.IsInvisible) return;

            // Recuperar color si existe (Asignado en Behavior.Initialize), sino Blanco
            Color tint = Color.White;
            if (HasVar("TintR"))
            {
                tint = new Color((int)GetVar("TintR"), (int)GetVar("TintG"), (int)GetVar("TintB"));
            }

            // Lógica de dibujo similar a Monster.cs pero aplicando el tint
            Rectangle sourceRect = this.Sprite.SourceRect;
            Vector2 position = Game1.GlobalToLocal(Game1.viewport, this.Position) + new Vector2(this.GetBoundingBox().Width / 2, this.GetBoundingBox().Height + this.yOffset);
            
            position.X += this.drawOffset.X;
            position.Y += this.drawOffset.Y;

            float layerDepth = Math.Max(0f, this.drawOnTop ? 0.991f : ((float)this.GetBoundingBox().Bottom / 10000f));
            
            // Ajuste para voladores (Gliders)
            if (this.isGlider.Value)
            {
                position.Y -= 64f; // Flotar visualmente
                layerDepth = (this.Position.Y + 640f) / 10000f; // Layer depth distinta
            }

            // Sombra
            if (!this.HideShadow)
            {
                this.Sprite.drawShadow(b, Game1.GlobalToLocal(Game1.viewport, this.Position), this.Scale);
            }

            // Sprite principal con TINT
            b.Draw(
                this.Sprite.spriteTexture, 
                position, 
                sourceRect, 
                tint, 
                this.rotation, 
                new Vector2(sourceRect.Width / 2, sourceRect.Height), 
                Math.Max(0.2f, this.Scale) * 4f, 
                (this.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 
                layerDepth
            );
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
                        drops.Add(ItemRegistry.Create(dropData.ItemId, Game1.random.Next(dropData.MinStack, dropData.MaxStack + 1)));
                }
            }
            return drops;
        }

        private void EnsureFallbackTexture()
        {
            if (this.Sprite == null) this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
            try { 
                if (this.Sprite.spriteTexture == null) 
                    this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); 
            } catch { }
        }
    }
}
