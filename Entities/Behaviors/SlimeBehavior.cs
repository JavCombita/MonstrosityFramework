using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class SlimeBehavior : MonsterBehavior
    {
        // Estados: 0=Idle, 1=Cargando, 2=Saltando
        
        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.AIState == 0) // IDLE
            {
                monster.isGlider.Value = false;
                
                // Temporizador para movimiento aleatorio o ataque
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.StateTimer = Game1.random.Next(1000, 2000); // Reset timer
                    
                    if (IsPlayerWithinRange(monster, 12) && Game1.random.NextDouble() < 0.05) // 5% chance de atacar
                    {
                        monster.AIState = 1; // Iniciar Carga
                        monster.StateTimer = 600f; // Tiempo de carga
                        Game1.playSound("slimeHit");
                        monster.Halt();
                    }
                    else
                    {
                        // Movimiento errático "marshmallow" vanilla
                        monster.moveTowardPlayer(1); 
                    }
                }
            }
            else if (monster.AIState == 1) // CARGANDO
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                monster.shake(Game1.random.Next(1, 3)); 
                monster.Sprite.currentFrame = 0; // Frame "aplastado"
                monster.Sprite.StopAnimation();

                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; // SALTO
                    monster.StateTimer = 500f; // Duración del salto
                    
                    // Calcular vector de lanzamiento
                    Vector2 target = monster.Player.Position;
                    Vector2 velocity = Utility.getVelocityTowardPlayer(
                        new Point((int)monster.Position.X, (int)monster.Position.Y), 
                        monster.Speed * 4f, 
                        monster.Player
                    );
                    
                    monster.xVelocity = velocity.X;
                    monster.yVelocity = velocity.Y;
                    monster.isGlider.Value = true; // Volar sobre obstáculos
                    Game1.playSound("slimeJump");
                }
            }
            else if (monster.AIState == 2) // EN EL AIRE
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);

                // Colisión con jugador
                if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
                {
                    monster.Player.takeDamage(monster.DamageToFarmer, false, null);
                    // Rebote
                    monster.xVelocity = -monster.xVelocity * 0.6f;
                    monster.yVelocity = -monster.yVelocity * 0.6f;
                    monster.AIState = 0;
                }

                // Aterrizaje
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 0;
                    monster.StateTimer = Game1.random.Next(800, 1500);
                    monster.xVelocity = 0;
                    monster.yVelocity = 0;
                }
            }
        }
    }
}