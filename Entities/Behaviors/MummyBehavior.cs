using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class MummyBehavior : MonsterBehavior
    {
        // Estados: 0=Normal, 1=Derrumbado (Montón de vendas)

        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.AIState == 1) // DERRUMBADO
            {
                monster.Halt();
                monster.IsWalkingTowardPlayer = false;
                monster.isGlider.Value = false;
                monster.IsInvincibleOverride = true; // No recibe daño de espada/armas
                monster.Sprite.currentFrame = 16; // Frame de "montón de ropa" (ajustar según sprite)
                monster.Sprite.StopAnimation();

                // Temporizador de reanimación (10 segundos)
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Vibración antes de revivir
                if (monster.StateTimer < 2000) monster.shake(1);

                if (monster.StateTimer <= 0)
                {
                    Revive(monster);
                }
            }
            else // NORMAL (Estado 0)
            {
                monster.IsInvincibleOverride = false;
                if (IsPlayerWithinRange(monster, 16))
                {
                    MoveTowardPlayer(monster, Math.Max(1, monster.Speed - 1)); // Las momias son lentas
                }
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            if (monster.AIState == 1) // Si ya está en el suelo
            {
                if (isBomb)
                {
                    // MUERTE REAL
                    Game1.playSound("rockGolemHit"); // Sonido de impacto fuerte
                    // Permitimos daño masivo para matarlo de verdad
                    monster.Health = 0;
                    return 9999; 
                }
                return 0; // Inmune a todo lo que no sea bombas
            }
            else // Si está de pie
            {
                int actualDamage = Math.Max(1, damage - monster.resilience.Value);
                if (monster.Health - actualDamage <= 0)
                {
                    // CAER AL SUELO (Muerte falsa)
                    monster.Health = monster.MaxHealth; // Recupera salud para cuando reviva
                    monster.AIState = 1;
                    monster.StateTimer = 10000f; // 10s para revivir
                    Game1.playSound("rockGolemHit");
                    monster.Sprite.currentFrame = 16;
                    monster.Sprite.StopAnimation();
                    return 0; // Evita que muera por el sistema vanilla
                }
            }
            return damage;
        }

        private void Revive(CustomMonster monster)
        {
            monster.AIState = 0;
            Game1.playSound("shadowDie"); // Sonido tétrico al levantarse
            monster.Sprite.currentFrame = 0;
        }
    }
}