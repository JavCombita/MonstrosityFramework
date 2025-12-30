using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class RockCrabBehavior : MonsterBehavior
    {
        // Estados: 0=Escondido, 1=Despertando, 2=Persiguiendo

        public override void Update(CustomMonster monster, GameTime time)
        {
            int baseFrame = monster.FacingDirection * 4;

            if (monster.AIState == 0) // ESCONDIDO
            {
                monster.IsInvincibleOverride = true; // Invulnerable
                monster.DamageToFarmer = 0; // No hace daño al tocar
                monster.Sprite.currentFrame = baseFrame; 
                monster.Sprite.StopAnimation(); 
                monster.HideShadow = true; 
                monster.IsWalkingTowardPlayer = false;

                // Despertar si el jugador se acerca mucho
                if (IsPlayerWithinRange(monster, 3)) // ~192 pixels
                {
                    WakeUp(monster);
                }
            }
            else if (monster.AIState == 1) // DESPERTANDO
            {
                monster.HideShadow = false; 
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Animación de sacar las patas
                monster.Sprite.currentFrame = baseFrame + 1;
                monster.Sprite.StopAnimation();
                
                if (monster.StateTimer <= 0) 
                { 
                    monster.AIState = 2; // Activo
                    monster.IsInvincibleOverride = false; 
                    
                    // Restaurar daño original desde datos
                    var data = GetData(monster);
                    monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                }
            }
            else // PERSECUCIÓN (Estado 2)
            {
                monster.HideShadow = false;
                monster.IsInvincibleOverride = false;
                
                // Si el jugador se aleja mucho, volver a esconderse
                if (!IsPlayerWithinRange(monster, 10)) 
                { 
                    monster.AIState = 0; 
                    Game1.playSound("stoneStep");
                    return; 
                }
                
                MoveTowardPlayer(monster, monster.Speed);
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // Si está escondido (Estado 0)
            if (monster.AIState == 0)
            {
                // Solo despierta con pico o bomba
                Game1.playSound("hitRock");
                WakeUp(monster);
                return 0; // Bloquea el daño inicial
            }
            return damage;
        }

        private void WakeUp(CustomMonster monster)
        {
            if (monster.AIState == 0)
            {
                monster.AIState = 1;
                monster.StateTimer = 500f; // Tiempo de animación
                Game1.playSound("stoneCrack");
                monster.shake(Game1.random.Next(2, 4));
            }
        }
    }
}