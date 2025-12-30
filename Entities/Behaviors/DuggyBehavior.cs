using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class DuggyBehavior : MonsterBehavior
    {
        // Estados: 0=Bajo tierra, 1=Saliendo/Atacando

        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.AIState == 0) // BAJO TIERRA
            {
                monster.IsInvisible = true;
                monster.HideShadow = true;
                monster.DamageToFarmer = 0;
                monster.IsInvincibleOverride = true;
                monster.Halt(); // No se mueve

                // Detectar jugador sobre él (o muy cerca)
                if (IsPlayerWithinRange(monster, 3))
                {
                    monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                    if (monster.StateTimer <= 0)
                    {
                        // SALIR
                        Game1.playSound("dig");
                        monster.AIState = 1;
                        monster.StateTimer = 2000f; // Tiempo arriba
                        monster.IsInvisible = false;
                        monster.HideShadow = false;
                        monster.IsInvincibleOverride = false;
                        
                        var data = GetData(monster);
                        monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                        monster.Sprite.currentFrame = 0;
                    }
                }
                else
                {
                    // Reset timer si el jugador se aleja
                    monster.StateTimer = 250f;
                }
            }
            else // ARRIBA (Estado 1)
            {
                // Animación (asumiendo frames 0-3 son la animación de salir/idle)
                monster.Sprite.Animate(time, 0, 4, 150f);
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    // VOLVER ABAJO
                    monster.AIState = 0;
                    monster.StateTimer = 1000f; // Cooldown para volver a salir
                }
            }
        }
    }
}