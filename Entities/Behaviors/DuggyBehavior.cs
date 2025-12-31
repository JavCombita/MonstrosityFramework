using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class DuggyBehavior : MonsterBehavior
    {
        // 0=Bajo Tierra (Invisible/Ojos), 1=Saliendo, 2=Arriba/Atacando, 3=Entrando

        public override void Update(CustomMonster monster, GameTime time)
        {
            // ESTADO 0: BAJO TIERRA (Frame 12)
            if (monster.AIState == 0) 
            {
                monster.IsInvisible = false; 
                monster.Sprite.currentFrame = 12; // Ojos/Tierra
                monster.Sprite.StopAnimation();
                
                monster.DamageToFarmer = 0;
                monster.IsInvincibleOverride = true;
                monster.Halt(); 

                // Detectar jugador
                if (IsPlayerWithinRange(monster, 3))
                {
                    monster.AIState = 1; // Saliendo
                    monster.StateTimer = 400f; 
                    Game1.playSound("dig");
                }
            }
            // ESTADO 1: SALIENDO (0 -> 3)
            else if (monster.AIState == 1)
            {
                monster.IsInvincibleOverride = false;
                monster.Sprite.Animate(time, 0, 4, 100f); 
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // Arriba
                    monster.StateTimer = 2000f; // Tiempo arriba
                    var data = GetData(monster);
                    monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                }
            }
            // ESTADO 2: ARRIBA / ATACANDO (4 -> 7)
            else if (monster.AIState == 2)
            {
                monster.Sprite.Animate(time, 4, 4, 150f); 
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 3; // Entrando
                    monster.StateTimer = 400f;
                }
            }
            // ESTADO 3: ENTERRÁNDOSE (8 -> 11) + HOYO
            else if (monster.AIState == 3)
            {
                monster.IsInvincibleOverride = true; 
                monster.Sprite.Animate(time, 8, 3, 100f); 

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    // FIN DE ANIMACIÓN: CREAR HOYO PERMANENTE
                    // Usamos el frame 11 como textura del hoyo
                    Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                        monster.Sprite.textureName.Value, 
                        new Rectangle(11 * monster.Sprite.SpriteWidth, 0, monster.Sprite.SpriteWidth, monster.Sprite.SpriteHeight), 
                        monster.Position,
                        false, 
                        0f, 
                        Color.White)
                    {
                        layerDepth = 0.0001f, // Muy al fondo
                        interval = 1000f,     // Estático
                        animationLength = 1,
                        totalNumberOfLoops = 9999, // Dura "para siempre" hasta que se borre o tape
                        id = (float)monster.GetHashCode() // ID único
                    });

                    monster.AIState = 0; 
                    monster.StateTimer = 1000f; // Cooldown
                }
            }
        }
    }
}