using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class SlimeBehavior : MonsterBehavior
    {
        // 0=Idle/Drift, 1=Cargando, 2=Saltando, 3=Cooldown

        public override void Update(CustomMonster monster, GameTime time)
        {
            float vision = GetVisionRange(monster, 8f);

            // --- ESTADO 0: IDLE / DRIFT ---
            if (monster.AIState == 0)
            {
                monster.isGlider.Value = false;
                monster.rotation = 0f;

                monster.GenericTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.GenericTimer <= 0)
                {
                    monster.GenericTimer = Game1.random.Next(2000, 5000); 
                    monster.FacingDirection = Game1.random.Next(0, 4); 
                }
                monster.Sprite.Animate(time, monster.FacingDirection * 4, 4, 200f); 

                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.StateTimer = Game1.random.Next(500, 1500);
                    
                    if (IsPlayerWithinRange(monster, vision))
                    {
                        Vector2 trajectory = monster.Player.Position - monster.Position;
                        if (trajectory != Vector2.Zero) trajectory.Normalize();
                        monster.xVelocity = trajectory.X * (monster.Speed * 0.5f); 
                        monster.yVelocity = trajectory.Y * (monster.Speed * 0.5f);
                    }
                    else
                    {
                        monster.xVelocity = (float)(Game1.random.NextDouble() - 0.5) * monster.Speed;
                        monster.yVelocity = (float)(Game1.random.NextDouble() - 0.5) * monster.Speed;
                    }
                }

                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                CheckMapBounds(monster);

                if (IsPlayerWithinRange(monster, vision) && Game1.random.NextDouble() < 0.03)
                {
                    monster.AIState = 1; 
                    monster.StateTimer = 600f; 
                    monster.Halt(); 
                    monster.xVelocity = 0; monster.yVelocity = 0;
                    Game1.playSound("slimeHit");
                }
            }
            
            // --- ESTADO 1: CARGANDO (16-17) ---
            else if (monster.AIState == 1)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                monster.Sprite.Animate(time, 16, 2, 100f); 
                monster.shake(Game1.random.Next(1, 3));    
                
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 2; 
                    monster.StateTimer = 1000f; 
                    monster.isGlider.Value = true; 
                    Game1.playSound("slimeJump");
                    
                    Vector2 target = monster.Player.Position;
                    Vector2 trajectory = target - monster.Position;
                    if (trajectory != Vector2.Zero) trajectory.Normalize();
                    
                    float jumpSpeed = monster.Speed * 6f; 
                    monster.xVelocity = trajectory.X * jumpSpeed;
                    monster.yVelocity = trajectory.Y * jumpSpeed;
                }
            }
            
            // --- ESTADO 2: EN EL AIRE (18-19) ---
            else if (monster.AIState == 2)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // FIX CS1612: Modificar Position asignando un nuevo Vector2
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                
                monster.Sprite.Animate(time, 18, 2, 100f); 

                // Colisión Jugador
                if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
                {
                    monster.Player.takeDamage(monster.DamageToFarmer, false, null);
                    Land(monster, 1000f); 
                    return;
                }

                // FIX CS1501: Detección de Colisión con Paredes usando método estándar
                // Si isCollidingPosition devuelve true, chocó contra algo
                if (Game1.currentLocation.isCollidingPosition(monster.GetBoundingBox(), Game1.viewport, false, 0, false, monster))
                {
                    Land(monster, 500f);
                }

                if (monster.StateTimer <= 0) Land(monster, 1500f); 
            }
            
            // --- ESTADO 3: ATERRIZAJE ---
            else if (monster.AIState == 3)
            {
                monster.xVelocity = 0; monster.yVelocity = 0;
                monster.Sprite.Animate(time, 0, 4, 300f); 
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 0; 
                }
            }
        }

        private void CheckMapBounds(CustomMonster m)
        {
             if (m.Position.X < 0) m.xVelocity = Math.Abs(m.xVelocity);
             if (m.Position.X > Game1.currentLocation.Map.DisplayWidth) m.xVelocity = -Math.Abs(m.xVelocity);
             if (m.Position.Y < 0) m.yVelocity = Math.Abs(m.yVelocity);
             if (m.Position.Y > Game1.currentLocation.Map.DisplayHeight) m.yVelocity = -Math.Abs(m.yVelocity);
        }

        private void Land(CustomMonster monster, float cooldown)
        {
            monster.AIState = 3; 
            monster.StateTimer = cooldown;
            monster.isGlider.Value = false; 
            monster.xVelocity = 0;
            monster.yVelocity = 0;
            if (Game1.currentLocation != null) Game1.playSound("slimeHit");
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            if (monster.AIState == 1 && Game1.random.NextDouble() < 0.4) 
            {
                monster.AIState = 3; 
                monster.StateTimer = 500f;
                Vector2 knockback = monster.Position - who.Position;
                if (knockback != Vector2.Zero) knockback.Normalize();
                monster.xVelocity = knockback.X * 5f;
                monster.yVelocity = knockback.Y * 5f;
            }
            return damage;
        }
    }
}