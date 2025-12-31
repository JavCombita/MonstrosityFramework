using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Projectiles;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class ShooterBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
            float detection = GetVisionRange(monster, 16f);

            if (monster.AIState == 1) 
            {
                monster.Halt();
                monster.Sprite.Animate(time, 16, 4, 100f);
                monster.GenericTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.GenericTimer <= 0)
                {
                    Shoot(monster);
                    monster.AIState = 0; 
                    monster.StateTimer = 3000f; 
                }
                return;
            }

            float distance = Vector2.Distance(monster.Position, monster.Player.Position);
            bool isMoving = false;

            if (distance > detection * 64f) // Fuera de rango
            {
                monster.Halt();
                monster.Sprite.currentFrame = monster.FacingDirection * 4;
                return; 
            }
            
            if (distance > 350f)
            {
                monster.IsWalkingTowardPlayer = true;
                monster.moveTowardPlayer(monster.Speed);
                isMoving = true;
            }
            else if (distance < 150f)
            {
                monster.IsWalkingTowardPlayer = false;
                Vector2 away = monster.Position - monster.Player.Position;
                if (away != Vector2.Zero) away.Normalize();
                monster.xVelocity = away.X * monster.Speed;
                monster.yVelocity = away.Y * monster.Speed;
                monster.faceGeneralDirection(monster.Position + away * 10);
                monster.MovePosition(time, Game1.viewport, Game1.currentLocation);
                isMoving = true;
            }
            else
            {
                monster.Halt();
                monster.faceGeneralDirection(monster.Player.Position);
                monster.Sprite.StopAnimation();
                monster.Sprite.currentFrame = monster.FacingDirection * 4;

                if (monster.StateTimer <= 0 && monster.withinPlayerThreshold(10))
                {
                    monster.AIState = 1; 
                    monster.GenericTimer = 400f; 
                }
            }

            if (isMoving)
            {
                int startFrame = monster.FacingDirection * 4;
                monster.Sprite.Animate(time, startFrame, 4, 150f); 
            }
        }

        private void Shoot(CustomMonster monster)
        {
            var data = GetData(monster);
            string projectileType = data?.CustomFields.ContainsKey("ProjectileType") == true ? data.CustomFields["ProjectileType"] : "fire";
            Vector2 shotVelocity = Utility.getVelocityTowardPlayer(new Point((int)monster.Position.X, (int)monster.Position.Y), 10f, monster.Player);
            BasicProjectile p = new BasicProjectile(monster.DamageToFarmer, BasicProjectile.shadowBall, 0, 0, 0f, shotVelocity.X, shotVelocity.Y, monster.Position, "flameSpell_hit", "flameSpell", null, false, false, Game1.currentLocation, monster);
            if (projectileType == "ice") p.debuff.Value = "19"; 
            Game1.currentLocation.projectiles.Add(p);
        }
    }
}