using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class MummyBehavior : MonsterBehavior
    {
        // ESTADOS: 
        // 0 = Normal (Caminar)
        // 1 = Derrumbándose (Animación 16->19)
        // 2 = En el suelo (Frame 19 estático)
        // 3 = Reviviendo (Animación 19->16)

        public override void Update(CustomMonster monster, GameTime time)
        {
            float detection = GetVisionRange(monster, 10f); 

            // --- ESTADO 0: NORMAL ---
            if (monster.AIState == 0)
            {
                monster.IsInvincibleOverride = false;
                
                if (IsPlayerWithinRange(monster, detection))
                {
                    MoveTowardPlayer(monster, Math.Max(1, monster.Speed - 1));
                    
                    // Animación direccional standard (0-15)
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
                else
                {
                    monster.Halt();
                    monster.Sprite.currentFrame = monster.FacingDirection * 4;
                }
            }
            
            // --- ESTADO 1: DERRUMBÁNDOSE (16 -> 19) ---
            else if (monster.AIState == 1)
            {
                monster.Halt();
                monster.IsInvincibleOverride = true; // Invulnerable mientras cae
                
                // Calcular frame basado en el tiempo restante
                // Duración caída: 400ms
                float totalTime = 400f;
                float progress = 1f - (monster.StateTimer / totalTime); // 0.0 a 1.0
                
                int frame = 16 + (int)(progress * 3); // 16, 17, 18, 19
                monster.Sprite.currentFrame = Math.Min(19, frame);

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // Pasa a estar en el suelo
                    monster.StateTimer = 10000f; // 10 segundos para revivir
                    monster.Sprite.currentFrame = 19; 
                }
            }
            
            // --- ESTADO 2: EN EL SUELO (Frame 19) ---
            else if (monster.AIState == 2)
            {
                monster.IsInvincibleOverride = true; // Solo muere con bombas
                monster.Sprite.currentFrame = 19;
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Vibrar un poco antes de revivir
                if (monster.StateTimer < 2000) monster.shake(1);

                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 3; // Empieza a revivir
                    monster.StateTimer = 400f;
                    Game1.playSound("shadowDie");
                }
            }
            
            // --- ESTADO 3: REVIVIENDO (19 -> 16) ---
            else if (monster.AIState == 3)
            {
                float progress = monster.StateTimer / 400f; // 1.0 a 0.0
                
                // Invertimos la animación: de 19 bajamos a 16
                int frame = 16 + (int)(progress * 3); 
                monster.Sprite.currentFrame = Math.Max(16, frame);

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 0; // Listo para pelear
                    monster.Health = monster.MaxHealth;
                }
            }
        }
        
        public override int OnTakeDamage(CustomMonster m, int d, bool b, Farmer w)
        {
            // Si está en el suelo (Estados 1, 2, 3)
            if (m.AIState >= 1) 
            { 
                if (b) // Solo muere con bombas
                { 
                    m.Health = 0; 
                    return 999; 
                } 
                return 0; 
            }
            
            // Si está de pie
            int actualDamage = Math.Max(1, d - m.resilience.Value);
            if (m.Health - actualDamage <= 0)
            {
                // INICIAR DERRUMBE
                m.Health = m.MaxHealth; // Recuperar vida (falsa muerte)
                m.AIState = 1; // Estado Caída
                m.StateTimer = 400f; // Duración de la animación de caída
                Game1.playSound("rockGolemHit");
                return 0; 
            }
            return d;
        }
    }
}