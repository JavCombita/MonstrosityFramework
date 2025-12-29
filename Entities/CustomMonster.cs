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
    // IMPORTANTE: Este nombre debe coincidir con el registro interno de SpaceCore.
    // Usamos guiones bajos para máxima compatibilidad con el serializador.
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        public readonly NetString MonsterSourceId = new();

        public CustomMonster() : base() 
        {
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
            // Si el ID cambia (ej: sincronización multiplayer), recargamos los datos.
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => ReloadData();
        }

        /// <summary>
        /// Método central que aplica los stats y texturas del Framework al monstruo vanilla.
        /// </summary>
        public void ReloadData()
        {
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) 
            {
                // Esto puede pasar durante la inicialización vacía, no es error grave.
                return;
            }

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            
            // 1. VERIFICACIÓN DE DATOS
            if (entry == null)
            {
                ModEntry.StaticMonitor.Log($"[CustomMonster] CRÍTICO: El monstruo '{MonsterSourceId.Value}' no existe en el Registry. Usando valores por defecto.", LogLevel.Error);
                this.Name = "Unknown Monster";
                // Fallback visual para evitar invisibilidad
                this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
                return;
            }

            // 2. APLICAR STATS
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            
            // Inicializar Sprite con dimensiones correctas pero sin textura aún
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            
            // 3. CARGA DE TEXTURA (EL AIRBAG ANTI-CRASH)
            try
            {
                Texture2D customTex = entry.GetTexture();
                
                if (customTex != null)
                {
                    this.Sprite.spriteTexture = customTex;
                    ModEntry.StaticMonitor.Log($"[CustomMonster] Textura cargada OK para {this.Name} ({customTex.Width}x{customTex.Height})", LogLevel.Trace);
                }
                else
                {
                    // --- PROTOCOLO DE EMERGENCIA ---
                    ModEntry.StaticMonitor.Log($"[CustomMonster] PELIGRO: La textura para '{this.Name}' es NULL. Activando textura de respaldo (Shadow Brute) para evitar crash.", LogLevel.Alert);
                    
                    // Cargamos una textura vainilla segura. 
                    // Esto evita el crash en 'shedChunks' cuando golpeas al monstruo.
                    this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute");
                }
            }
            catch (Exception ex)
            {
                ModEntry.StaticMonitor.Log($"[CustomMonster] Excepción fatal asignando textura: {ex.Message}", LogLevel.Error);
            }
            
            this.HideShadow = false;
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            // Protección: Si por alguna razón el sprite es null, no ejecutar lógica para evitar más errores.
            if (this.Sprite?.spriteTexture == null) return;

            // Lógica Vanilla: Solo actuar si el jugador está cerca
            if (!withinPlayerThreshold(16)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";

            switch (behavior)
            {
                case "Stalker":
                    // Si el jugador no me mira, me muevo rápido hacia él
                    if (!IsPlayerLookingAtMe()) MoveTowardPlayer(3);
                    else MoveTowardPlayer(1); // Si me mira, me muevo lento (o podrías hacer que se detenga)
                    break;
                    
                case "Tank": 
                    // Lento pero constante
                    MoveTowardPlayer(1); 
                    break;

                case "Rusher":
                    // Muy rápido
                    MoveTowardPlayer(4);
                    break;

                case "Default":
                default:
                    base.behaviorAtGameTick(time); 
                    break;
            }
        }

        // --- MÉTODOS AUXILIARES ---

        private void MoveTowardPlayer(int speed)
        {
            this.IsWalkingTowardPlayer = true;
            base.moveTowardPlayer(speed); 
        }

        private bool IsPlayerLookingAtMe()
        {
            Vector2 toMonster = this.Position - this.Player.Position;
            int faceDir = this.Player.FacingDirection; 
            
            // Lógica simple para determinar si el jugador encara al monstruo
            if (Math.Abs(toMonster.X) > Math.Abs(toMonster.Y))
            {
                // Eje horizontal
                return (toMonster.X > 0 && faceDir == 1) || (toMonster.X < 0 && faceDir == 3);
            }
            else
            {
                // Eje vertical
                return (toMonster.Y > 0 && faceDir == 2) || (toMonster.Y < 0 && faceDir == 0);
            }
        }
        
        public override List<Item> getExtraDropItems()
        {
            var drops = new List<Item>();
            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            
            if (entry != null && entry.Data.Drops != null)
            {
                foreach (var dropData in entry.Data.Drops)
                {
                    if (Game1.random.NextDouble() <= dropData.Chance)
                    {
                        // ItemRegistry.Create es el método seguro en 1.6
                        Item item = ItemRegistry.Create(dropData.ItemId, 1);
                        drops.Add(item);
                    }
                }
            }
            
            return drops;
        }
    }
}