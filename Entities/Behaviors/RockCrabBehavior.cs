using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class RockCrabBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            int baseRowStart = 0;
            switch(monster.FacingDirection)
            {
                case 2: baseRowStart = 0; break; 
                case 1: baseRowStart = 4; break; 
                case 0: baseRowStart = 8; break; 
                case 3: baseRowStart = 12; break;
            }

            if (monster.AIState == 0) // ESCONDIDO
            {
                monster.IsInvincibleOverride = true; 
                monster.DamageToFarmer = 0;
                monster.Sprite.currentFrame = baseRowStart; 
                monster.Sprite.StopAnimation(); 
                monster.HideShadow = true; 
                monster.IsWalkingTowardPlayer = false;

                if (IsPlayerWithinRange(monster, 3)) WakeUp(monster);
            }
            else if (monster.AIState == 1) // DESPERTANDO
            {
                monster.HideShadow = false; 
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                monster.Sprite.Animate(time, 1, 3, 150f);
                if (monster.StateTimer <= 0) 
                { 
                    monster.AIState = 2; 
                    monster.IsInvincibleOverride = false; 
                    var data = GetData(monster);
                    monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                }
            }
            else if (monster.AIState == 2) // CAMINANDO
            {
                monster.HideShadow = false;
                monster.IsInvincibleOverride = false;
                if (!IsPlayerWithinRange(monster, 10)) { monster.AIState = 0; Game1.playSound("stoneStep"); return; }
                MoveTowardPlayer(monster, monster.Speed);
                if (monster.IsWalkingTowardPlayer) monster.Sprite.Animate(time, baseRowStart + 1, 3, 150f);
            }
            else if (monster.AIState == 3) // SIN CAPARAZÓN
            {
                monster.DamageToFarmer = 0;
                monster.HideShadow = false;
                Vector2 away = monster.Position - monster.Player.Position;
                if (away != Vector2.Zero) away.Normalize();
                int fleeSpeed = monster.Speed + 2; 
                monster.xVelocity = away.X * fleeSpeed;
                monster.yVelocity = away.Y * fleeSpeed;
                monster.faceGeneralDirection(monster.Position + away * 10);
                monster.MovePosition(time, Game1.viewport, Game1.currentLocation);
                monster.Sprite.Animate(time, 16, 4, 100f);
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            if (isBomb && monster.AIState != 3)
            {
                Game1.playSound("breakingGlass");
                monster.AIState = 3; 
                monster.IsInvincibleOverride = false;
                monster.resilience.Value = 0; // Fix: resilience
                return damage; 
            }
            if (monster.AIState == 0)
            {
                Game1.playSound("hitRock");
                WakeUp(monster);
                return 0; 
            }
            return damage;
        }

        private void WakeUp(CustomMonster monster)
        {
            if (monster.AIState == 0)
            {
                monster.AIState = 1;
                monster.StateTimer = 500f; 
                Game1.playSound("stoneCrack");
                monster.shake(Game1.random.Next(2, 4));
            }
        }
    }
}