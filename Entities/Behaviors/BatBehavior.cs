using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class BatBehavior : MonsterBehavior
    {
        // 0=Reposo, 1=Despertando, 2=Vuelo
        
        public override void Update(CustomMonster monster, GameTime time)
        {
            float vision = GetVisionRange(monster, 6f); 

            // ESTADO 0: REPOSO
            if (monster.AIState == 0)
            {
                monster.isGlider.Value = false; 
                monster.Sprite.currentFrame = 0; 
                monster.Sprite.StopAnimation();

                if (IsPlayerWithinRange(monster, vision))
                {
                    monster.AIState = 1; 
                    monster.StateTimer = 500f; 
                    Game1.playSound("batScreech");
                }
            }
            // ESTADO 1: DESPERTANDO
            else if (monster.AIState == 1)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer > 333) monster.Sprite.currentFrame = 1;
                else if (monster.StateTimer > 166) monster.Sprite.currentFrame = 2;
                else monster.Sprite.currentFrame = 3;

                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; 
                    monster.isGlider.Value = true;
                }
            }
            // ESTADO 2: VUELO
            else
            {
                monster.Sprite.Animate(time, 4, 15, 120f);
                
                // Persecución directa
                if (IsPlayerWithinRange(monster, vision * 2)) 
                {
                    Vector2 trajectory = monster.Player.Position - monster.Position;
                    if (trajectory != Vector2.Zero) 
                    {
                        trajectory.Normalize();
                        // Velocidad directa del JSON
                        monster.xVelocity = trajectory.X * monster.Speed;
                        monster.yVelocity = trajectory.Y * monster.Speed;
                    }
                }
                else
                {
                    monster.xVelocity *= 0.95f;
                    monster.yVelocity *= 0.95f;
                }
                
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                
                if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
                {
                    monster.xVelocity = -monster.xVelocity;
                    monster.yVelocity = -monster.yVelocity;
                }
            }
        }
    }
}