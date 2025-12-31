using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class DuggyBehavior : MonsterBehavior
    {
        // 0=Bajo Tierra, 1=Saliendo, 2=Arriba, 3=Entrando

        public override void Update(CustomMonster monster, GameTime time)
        {
            float detection = GetVisionRange(monster, 4f); 

            // ESTADO 0: BAJO TIERRA
            if (monster.AIState == 0) 
            {
                monster.IsInvisible = true; 
                monster.HideShadow = true;
                monster.DamageToFarmer = 0;
                monster.IsInvincibleOverride = true;
                monster.Halt(); 

                // DETECCIÓN Y EMBOSCADA
                if (IsPlayerWithinRange(monster, detection))
                {
                    Vector2 targetPos = monster.Player.Position;
                    
                    // Verificar suelo válido antes de moverse
                    if (Game1.currentLocation.isTileLocationTotallyClearAndPlaceable(targetPos))
                    {
                        monster.Position = targetPos; // Teleport bajo el jugador
                        
                        Game1.playSound("dig");
                        monster.AIState = 1; 
                        monster.StateTimer = 400f; 
                        monster.IsInvisible = false;
                        monster.HideShadow = false;
                        monster.IsInvincibleOverride = false;
                        
                        var data = GetData(monster);
                        monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                        monster.Sprite.currentFrame = 0;
                    }
                }
            }
            // ESTADO 1: SALIENDO
            else if (monster.AIState == 1)
            {
                monster.Sprite.Animate(time, 0, 4, 100f); 
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // Arriba
                    monster.StateTimer = 2000f; 
                }
            }
            // ESTADO 2: ARRIBA
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
            // ESTADO 3: ENTERRÁNDOSE
            else if (monster.AIState == 3)
            {
                monster.IsInvincibleOverride = true; 
                monster.Sprite.Animate(time, 8, 4, 100f); 

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    // Crear hoyo
                    Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                        monster.Sprite.textureName.Value, 
                        new Rectangle(11 * monster.Sprite.SpriteWidth, 0, monster.Sprite.SpriteWidth, monster.Sprite.SpriteHeight), 
                        monster.Position,
                        false, 0f, Color.White)
                    {
                        layerDepth = 0.0001f, 
                        interval = 2000f, 
                        animationLength = 1,
                        id = monster.GetHashCode() // Fix: int (GetHashCode devuelve int)
                    });

                    monster.AIState = 0; 
                    monster.StateTimer = 1000f; 
                }
            }
        }
    }
}