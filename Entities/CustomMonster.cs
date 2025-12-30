using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities.Behaviors; // Asegúrate de que este namespace exista

namespace MonstrosityFramework.Entities
{
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        // --- SINCRONIZACIÓN DE RED ---
        public readonly NetString MonsterSourceId = new();
        
        // --- ESTADO PÚBLICO (La memoria del monstruo) ---
        // Estos campos son manipulados por las clases "Behavior" externas.
        // Al ser públicos, evitamos tener mil variables privadas para cada tipo de monstruo.
        
        [XmlIgnore] // No guardamos esto en XML, el estado se resetea o recalcula al cargar
        public float StateTimer = 0f;
        
        [XmlIgnore]
        public int AIState = 0; 
        
        [XmlIgnore]
        public bool IsInvincibleOverride = false;

        // Variables genéricas extra por si un comportamiento complejo necesita más memoria
        [XmlIgnore] public float GenericTimer = 0f; 
        [XmlIgnore] public bool GenericFlag = false;
        
        // --- CEREBRO ---
        private MonsterBehavior _currentBehavior; 
        private string _cachedBehaviorId = "default";
        
        // --- CONTROL INTERNO ---
        private bool _hasLoadedData = false;
        private bool _textureChecked = false;

        // Constructor vacío requerido por Netcode/XML
        public CustomMonster() : base() 
        { 
            EnsureFallbackTexture(); 
        }

        // Constructor principal
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
            
            // Recargar datos automáticamente si el ID cambia (Sincronización Multijugador)
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => { 
                _hasLoadedData = false; 
                ReloadData(); 
            };
        }

        /// <summary>
        /// Carga estadísticas, texturas y ASIGNA EL COMPORTAMIENTO (Cerebro).
        /// </summary>
        public void ReloadData()
        {
            _hasLoadedData = true;
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null) 
            { 
                ModEntry.StaticMonitor.Log($"[CustomMonster] No se encontró data para ID: {MonsterSourceId.Value}", StardewModdingAPI.LogLevel.Warn);
                EnsureFallbackTexture(); 
                return; 
            }

            // 1. ASIGNACIÓN DE ESTRATEGIA (PATRÓN STRATEGY)
            // Aquí ocurre la magia: El monstruo se convierte en Slime, Momia, etc.
            _cachedBehaviorId = (entry.Data.BehaviorType ?? "Default").ToLowerInvariant();
            _currentBehavior = BehaviorFactory.GetBehavior(_cachedBehaviorId);

            // 2. ESTADÍSTICAS DE COMBATE
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            // Solo curar al máximo si es la primera carga (evitar curar en mitad de combate al resincronizar)
            if (this.Health > this.MaxHealth || this.Health <= 0) this.Health = this.MaxHealth; 
            
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            this.resilience.Value = entry.Data.Defense;
            
            // Optimización: Solo cambiar velocidad si es diferente (evita stuttering)
            if (this.Speed != entry.Data.Speed) this.Speed = entry.Data.Speed;
            
            // Importante: La mayoría de monstruos custom no deben romper rocas al pisarlas
            this.willDestroyObjectsUnderfoot = false;

            // 3. APARIENCIA
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
            
            this.HideShadow = false;
            
            // Reiniciar estado IA al recargar
            this.AIState = 0;
            this.StateTimer = 0;
        }

        // --- BUCLE DE JUEGO (GAME LOOP) ---
        public override void behaviorAtGameTick(GameTime time)
        {
            // 1. CHEQUEOS DE SEGURIDAD
            if (!_textureChecked)
            {
                _textureChecked = true;
                if (this.Sprite?.spriteTexture == null) EnsureFallbackTexture();
            }

            if (!_hasLoadedData) ReloadData();

            // Fix Anti-Crash para coordenadas corruptas (NaN)
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y)) {
                this.Position = Game1.player.Position + new Vector2(64, 64);
                this.xVelocity = 0; this.yVelocity = 0;
            }

            // 2. EJECUCIÓN DEL COMPORTAMIENTO
            if (_currentBehavior != null)
            {
                // Delegamos toda la lógica al comportamiento actual
                _currentBehavior.Update(this, time);
            }
            else
            {
                // Fallback de emergencia si falló la fábrica
                base.behaviorAtGameTick(time); 
            }
        }

        // --- SISTEMA DE DAÑO ---
        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            // 1. INTERCEPCIÓN DEL COMPORTAMIENTO
            // El comportamiento puede modificar el daño (ej: RockCrab = 0) o reaccionar (ej: Momia cae)
            if (_currentBehavior != null)
            {
                int modifiedDamage = _currentBehavior.OnTakeDamage(this, damage, isBomb, who);
                
                // Si el comportamiento anuló el daño (<= 0), terminamos aquí.
                // Esto evita sonidos de golpe en monstruos invulnerables.
                if (modifiedDamage <= 0 && damage > 0) return 0;
                
                damage = modifiedDamage;
            }

            // 2. APLICACIÓN VANILLA
            return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
        }

        // --- SOBRECARGAS DE UTILIDAD ---

        public override bool isInvincible()
        {
            // Permite que un Behavior active invencibilidad temporal (ej: Duggy bajo tierra)
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
                        drops.Add(ItemRegistry.Create(dropData.ItemId, 1)); // Usa ItemRegistry de SMAPI/Vanilla 1.6
                }
            }
            return drops;
        }

        // --- HELPERS PRIVADOS ---

        private void EnsureFallbackTexture()
        {
            // Carga la textura del Shadow Brute como fallback seguro si algo falla
            if (this.Sprite == null) this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
            
            if (this.Sprite.spriteTexture == null || this.Sprite.spriteTexture.IsDisposed)
            {
                try { this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); } 
                catch { /* Si falla esto, el juego probablemente ya crasheó por falta de archivos vanilla */ }
            }
        }
    }
}