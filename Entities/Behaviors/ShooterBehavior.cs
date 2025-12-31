using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Projectiles;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class ShooterBehavior : MonsterBehavior
    {
        // AIState: 0=Moverse/Kiting, 1=Disparando

        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds; // Cooldown disparo

            // --- ANIMACIÓN DE ATAQUE (16-19) ---
            if (monster.AIState == 1) 
            {
                monster.Halt();
                monster.Sprite.Animate(time, 16, 4, 100f);
                
                monster.GenericTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.GenericTimer <= 0)
                {
                    Shoot(monster);
                    monster.AIState = 0; // Volver a moverse
                    monster.StateTimer = 3000f; // Cooldown 3s
                }
                return;
            }

            // --- LÓGICA DE MOVIMIENTO ---
            float distance = Vector2.Distance(monster.Position, monster.Player.Position);
            bool isMoving = false;

            if (distance > 350f)
            {
                monster.IsWalkingTowardPlayer = true;
                monster.moveTowardPlayer(monster.Speed);
                isMoving = true;
            }
            else if (distance < 150f) // Kiting (Alejarse)
            {
                monster.IsWalkingTowardPlayer = false;
                Vector2 away = monster.Position - monster.Player.Position;
                if (away != Vector2.Zero) away.Normalize();
                
                monster.xVelocity = away.X * monster.Speed;
                monster.yVelocity = away.Y * monster.Speed;
                monster.faceGeneralDirection(monster.Position + away * 10); // Mirar hacia donde huye
                monster.MovePosition(time, Game1.viewport, Game1.currentLocation);
                isMoving = true;
            }
            else // Rango de disparo
            {
                monster.Halt();
                monster.faceGeneralDirection(monster.Player.Position);
                monster.Sprite.StopAnimation();
                monster.Sprite.currentFrame = GetIdleFrame(monster.FacingDirection);

                // Iniciar ataque
                if (monster.StateTimer <= 0 && monster.withinPlayerThreshold(10))
                {
                    monster.AIState = 1; 
                    monster.GenericTimer = 400f; // Tiempo de cast
                }
            }

            // ANIMACIÓN DIRECCIONAL (0-15)
            if (isMoving && monster.AIState == 0)
            {
                int startFrame = GetBaseFrame(monster.FacingDirection);
                monster.Sprite.Animate(time, startFrame, 4, 150f); 
            }
        }

        private int GetBaseFrame(int facing)
        {
            // 0=Sur (0-3), 1=Este (4-7), 2=Norte (8-11), 3=Oeste (12-15)
            switch(facing)
            {
                case 2: return 0; // Sur
                case 1: return 4; // Este
                case 0: return 8; // Norte
                case 3: return 12; // Oeste
                default: return 0;
            }
        }

        private int GetIdleFrame(int facing)
        {
            // Frame inicial de cada dirección
            return GetBaseFrame(facing);
        }

        private void Shoot(CustomMonster monster)
        {
            var data = GetData(monster);
            string projectileType = data?.CustomFields.ContainsKey("ProjectileType") == true ? data.CustomFields["ProjectileType"] : "fire";
            
            Vector2 shotVelocity = Utility.getVelocityTowardPlayer(new Point((int)monster.Position.X, (int)monster.Position.Y), 10f, monster.Player);

            BasicProjectile projectile = new BasicProjectile(
                monster.DamageToFarmer,           
                BasicProjectile.shadowBall, 
                0, 0, 0f, shotVelocity.X, shotVelocity.Y, monster.Position,                 
                "flameSpell_hit", "flameSpell", null, false, false, Game1.currentLocation, monster
            );
            
            if (projectileType == "ice") projectile.debuff.Value = "19"; 
            Game1.currentLocation.projectiles.Add(projectile);
        }
    }
}