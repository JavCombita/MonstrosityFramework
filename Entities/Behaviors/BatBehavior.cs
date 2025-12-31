using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class BatBehavior : MonsterBehavior
    {
        // Estados: 0=Reposo (Techo), 1=Despertando, 2=Volando
        
        public override void Update(CustomMonster monster, GameTime time)
        {
            // ESTADO 0: REPOSO (Frame 0)
            if (monster.AIState == 0)
            {
                monster.isGlider.Value = false; 
                monster.Sprite.currentFrame = 0; 
                monster.Sprite.StopAnimation();

                // Si el jugador entra en rango (6 tiles), despertar
                if (IsPlayerWithinRange(monster, 6))
                {
                    monster.AIState = 1; 
                    monster.StateTimer = 500f; // Tiempo para abrir alas
                    Game1.playSound("batScreech");
                }
            }
            // ESTADO 1: DESPERTANDO (1 -> 3)
            else if (monster.AIState == 1)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Animación manual simple: 1 -> 2 -> 3
                if (monster.StateTimer > 333) monster.Sprite.currentFrame = 1;
                else if (monster.StateTimer > 166) monster.Sprite.currentFrame = 2;
                else monster.Sprite.currentFrame = 3;

                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // Pasar a vuelo
                    monster.isGlider.Value = true;
                }
            }
            // ESTADO 2: VUELO (4 -> 7)
            else
            {
                // Bucle de vuelo estándar
                monster.Sprite.Animate(time, 4, 12, 120f); 
                
                // Física de vuelo (Inercia y persecución)
                if (IsPlayerWithinRange(monster, 18)) 
                {
                    Vector2 trajectory = monster.Player.Position - monster.Position;
                    if (trajectory.LengthSquared() > 0.0001f) trajectory.Normalize();

                    float acceleration = 0.15f;
                    float maxSpeed = monster.Speed * 2f;

                    if (monster.xVelocity < trajectory.X * maxSpeed) monster.xVelocity += acceleration;
                    else if (monster.xVelocity > trajectory.X * maxSpeed) monster.xVelocity -= acceleration;
                    
                    if (monster.yVelocity < trajectory.Y * maxSpeed) monster.yVelocity += acceleration;
                    else if (monster.yVelocity > trajectory.Y * maxSpeed) monster.yVelocity -= acceleration;
                }
                
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                
                // Rebote al chocar con jugador
                if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
                {
                    monster.xVelocity = -monster.xVelocity * 0.5f;
                    monster.yVelocity = -monster.yVelocity * 0.5f;
                }
            }
        }
    }
}