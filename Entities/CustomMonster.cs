using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using System.Xml.Serialization;
using MonstrosityFramework.Framework.Registries;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace MonstrosityFramework.Entities
{
    // Este atributo es CRÍTICO para que SpaceCore guarde el monstruo
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        public readonly NetString MonsterSourceId = new();
        
        // Bandera para saber si ya intentamos cargar los datos al menos una vez
        private bool _hasLoadedData = false;

        public CustomMonster() : base() 
        {
            // Constructor vacío requerido por Netcode/SaveGame
        }

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
            
            // Si el ID cambia en tiempo real (ej: edición en vivo), recargamos
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => 
            {
                _hasLoadedData = false; 
                ReloadData();
            };
        }

        /// <summary>
        /// Carga las estadísticas y texturas del monstruo desde el registro.
        /// </summary>
        public void ReloadData()
        {
            _hasLoadedData = true; // Marcamos como 'intentado' para evitar bucles infinitos

            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            
            // FALLBACK 1: El ID no existe en el registro (¿Mod borrado?)
            if (entry == null)
            {
                // Asignamos una textura de emergencia para que drawDebris NO crashee
                EnsureFallbackTexture();
                return;
            }

            // 1. Aplicar Estadísticas
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            
            // 2. Configurar Sprite
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            
            // 3. Cargar Textura con Seguridad
            try
            {
                Texture2D customTex = entry.GetTexture();
                if (customTex != null)
                {
                    this.Sprite.spriteTexture = customTex;
                }
                else
                {
                    // FALLBACK 2: La imagen del mod falló al cargar
                    EnsureFallbackTexture();
                }
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[CustomMonster] Error fatal textura: {ex.Message}", LogLevel.Error);
                EnsureFallbackTexture();
            }
            
            this.HideShadow = false;
        }

        /// <summary>
        /// Asigna la textura del Shadow Brute si no tenemos nada más.
        /// Vital para evitar NullReferenceException en GameLocation.drawDebris.
        /// </summary>
        private void EnsureFallbackTexture()
        {
            if (this.Sprite == null) 
                this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
                
            if (this.Sprite.spriteTexture == null)
                this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute");
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            // --- AUTO-CORRECCIÓN (LAZY LOADING) ---
            // Al cargar partida, el constructor vacío NO llama a ReloadData.
            // Lo detectamos aquí en el primer frame de lógica.
            if (!_hasLoadedData || this.Sprite?.spriteTexture == null)
            {
                if (!string.IsNullOrEmpty(MonsterSourceId.Value))
                {
                    ReloadData();
                }
                
                // Si tras recargar sigue sin textura, abortamos para no causar daño invisible.
                if (this.Sprite?.spriteTexture == null) return;
            }

            // Lógica Vanilla de visión
            if (!withinPlayerThreshold(16)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";

            // IA Simple
            switch (behavior)
            {
                case "Stalker":
                    if (!IsPlayerLookingAtMe()) MoveTowardPlayer(3);
                    else MoveTowardPlayer(1); 
                    break;
                    
                case "Tank": 
                    MoveTowardPlayer(1); 
                    break;

                case "Rusher":
                    MoveTowardPlayer(4);
                    break;

                case "Default":
                default:
                    base.behaviorAtGameTick(time); 
                    break;
            }
        }

        private void MoveTowardPlayer(int speed)
        {
            this.IsWalkingTowardPlayer = true;
            base.moveTowardPlayer(speed); 
        }

        private bool IsPlayerLookingAtMe()
        {
            Vector2 toMonster = this.Position - this.Player.Position;
            int faceDir = this.Player.FacingDirection; 
            
            if (Math.Abs(toMonster.X) > Math.Abs(toMonster.Y))
                return (toMonster.X > 0 && faceDir == 1) || (toMonster.X < 0 && faceDir == 3);
            else
                return (toMonster.Y > 0 && faceDir == 2) || (toMonster.Y < 0 && faceDir == 0);
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
                    {
                        // ItemRegistry.Create es la forma moderna en 1.6 de crear items
                        Item item = ItemRegistry.Create(dropData.ItemId, 1);
                        drops.Add(item);
                    }
                }
            }
            
            return drops;
        }
    }
}