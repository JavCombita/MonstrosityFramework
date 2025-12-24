using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using System.Xml.Serialization;
using MonstrosityFramework.Framework.Registries;
using StardewModdingAPI;

namespace MonstrosityFramework.Entities
{
    // CRÍTICO: Este atributo le dice a SpaceCore cómo guardar esta clase en el XML del save.
    // Si cambias este string, rompes los saves de tus usuarios.
    [XmlType("Mods.Monstrosity.CustomMonster")]
    public class CustomMonster : Monster
    {
        // Sincronización Multiplayer: El ID viaja del Host a los Farmhands
        public readonly NetString MonsterSourceId = new();

        // Constructor vacío requerido por SpaceCore/Netcode
        public CustomMonster() : base() 
        {
        }

        // Constructor principal para Spawning
        public CustomMonster(string uniqueId, Vector2 position) : base()
        {
            this.MonsterSourceId.Value = uniqueId;
            this.Position = position;
            this.ReloadData();
        }

        // Se llama automáticamente cuando el objeto se deserializa (carga de save o multiplayer)
        protected override void initNetFields()
        {
            base.initNetFields();
            this.NetFields.AddField(MonsterSourceId);
            
            // Cuando el ID cambie (ej: cliente conectándose), recargar datos
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => ReloadData();
        }

        /// <summary>
        /// Hidrata el monstruo con los datos del JSON y la Textura externa.
        /// </summary>
        public void ReloadData()
        {
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null)
            {
                // Fallback: Si el mod hijo fue borrado, convertimos esto en un Green Slime genérico
                // para evitar crashes, o simplemente lo hacemos invisible.
                this.Name = "Unknown Monster";
                return;
            }

            // 1. Aplicar Stats
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            
            // 2. Inyección de Sprite (Magia Negra)
            // Creamos un AnimatedSprite vacío y luego le inyectamos la textura cruda manualmente.
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            
            Texture2D customTex = entry.GetTexture();
            if (customTex != null)
            {
                this.Sprite.spriteTexture = customTex;
            }
            
            // Forzar hitbox
            this.HideShadow = false;
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            // Si el jugador está muy lejos, no procesar IA (Optimización)
            if (!withinPlayerThreshold(16)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";

            // IA Pluggable
            switch (behavior)
            {
                case "Stalker": // Solo se mueve si no lo miras
                    if (!IsPlayerLookingAtMe()) MoveTowardPlayer(2);
                    break;
                    
                case "Tank": // Lento pero imparable
                    MoveTowardPlayer(1); 
                    break;

                case "Default":
                default:
                    base.behaviorAtGameTick(time); // Comportamiento vanilla (perseguir)
                    break;
            }
        }

        // Helper para lógica de movimiento custom
        private void MoveTowardPlayer(int speed)
        {
            this.IsWalkingTowardPlayer = true;
            this.moveTowardPlayer(speed);
        }

        private bool IsPlayerLookingAtMe()
        {
            // Lógica vectorial simple para saber si el player encara al monstruo
            Vector2 toMonster = this.Position - this.Player.Position;
            int faceDir = this.Player.FacingDirection; // 0=Up, 1=Right, 2=Down, 3=Left
            
            if (Math.Abs(toMonster.X) > Math.Abs(toMonster.Y))
            {
                // Eje X dominante
                return (toMonster.X > 0 && faceDir == 1) || (toMonster.X < 0 && faceDir == 3);
            }
            else
            {
                // Eje Y dominante
                return (toMonster.Y > 0 && faceDir == 2) || (toMonster.Y < 0 && faceDir == 0);
            }
        }
        
        // Sobreescribir Drops
        public override List<Item> getExtraDropItems()
        {
            var drops = new List<Item>();
            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            
            if (entry != null)
            {
                foreach (var dropData in entry.Data.Drops)
                {
                    if (Game1.random.NextDouble() <= dropData.Chance)
                    {
                        // ItemRegistry.Create es compatible con 1.6
                        Item item = ItemRegistry.Create(dropData.ItemId, 1);
                        drops.Add(item);
                    }
                }
            }
            
            return drops;
        }
    }
}