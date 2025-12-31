using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class MummyBehavior : MonsterBehavior
    {
        // 0=Normal, 1=Derrumbándose, 2=Muerto(Suelo), 3=Reviviendo

        public override void Update(CustomMonster monster, GameTime time)
        {
            // ESTADO 0: NORMAL
            if (monster.AIState == 0)
            {
                monster.IsInvincibleOverride = false;
                if (IsPlayerWithinRange(monster, 16))
                {
                    MoveTowardPlayer(monster, Math.Max(1, monster.Speed - 1));
                    
                    // Animación direccional standard
                    int baseRowStart = 0;
                    switch(monster.FacingDirection)
                    {
                        case 2: baseRowStart = 0; break; 
                        case 1: baseRowStart = 4; break; 
                        case 0: baseRowStart = 8; break; 
                        case 3: baseRowStart = 12; break; 
                    }
                    monster.Sprite.Animate(time, baseRowStart, 4, 150f);
                }
            }
            // ESTADO 1: DERRUMBÁNDOSE (16 -> 19)
            else if (monster.AIState == 1)
            {
                monster.Halt();
                // Interpolación manual de frames basada en el tiempo restante
                float totalTime = 400f;
                float progress = 1f - (monster.StateTimer / totalTime); // 0.0 a 1.0
                
                int frame = 16 + (int)(progress * 3); // 16, 17, 18, 19
                monster.Sprite.currentFrame = Math.Min(19, frame);

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // Muerto en el suelo
                    monster.StateTimer = 10000f; // 10s para revivir
                    monster.Sprite.currentFrame = 19; 
                }
            }
            // ESTADO 2: EN EL SUELO (Frame 19)
            else if (monster.AIState == 2)
            {
                monster.IsInvincibleOverride = true; 
                monster.Sprite.currentFrame = 19;
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer < 2000) monster.shake(1); // Avisar que revive

                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 3; // Reviviendo
                    monster.StateTimer = 400f;
                    Game1.playSound("shadowDie");
                }
            }
            // ESTADO 3: REVIVIENDO (19 -> 16)
            else if (monster.AIState == 3)
            {
                float totalTime = 400f;
                float progress = monster.StateTimer / totalTime; // 1.0 a 0.0
                
                int frame = 16 + (int)(progress * 3); // De 19 baja a 16
                monster.Sprite.currentFrame = Math.Max(16, frame);

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 0; // Listo
                    monster.Health = monster.MaxHealth;
                }
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            if (monster.AIState == 2 || monster.AIState == 3) 
            {
                if (isBomb)
                {
                    monster.Health = 0;
                    return 9999; // Muerte definitiva
                }
                return 0;
            }
            else // De pie
            {
                int actualDamage = Math.Max(1, damage - monster.resilience.Value);
                if (monster.Health - actualDamage <= 0)
                {
                    monster.Health = monster.MaxHealth; 
                    monster.AIState = 1; // Iniciar derrumbe
                    monster.StateTimer = 400f; 
                    Game1.playSound("rockGolemHit");
                    return 0; // No muere, se cae
                }
            }
            return damage;
        }
    }
}