using Microsoft.Xna.Framework;
using StardewValley;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class StalkerBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            // Usamos el Helper GetVisionRange que lee "DetectionRange" del JSON
            float range = GetVisionRange(monster, 16); 

            if (monster.Player != null && IsPlayerWithinRange(monster, range))
            {
                monster.IsWalkingTowardPlayer = true;
                
                // Forzar umbral de movimiento
                monster.moveTowardPlayerThreshold.Value = (int)range; 
                
                if (monster.isMoving()) 
                {
                    // Animación estándar (Arriba, Abajo, Izq, Der automáticos por Stardew)
                    monster.Sprite.AnimateDown(time);
                }
            }
            else
            {
                // Idle
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();
                monster.Sprite.StopAnimation();
            }
        }
    }
}
