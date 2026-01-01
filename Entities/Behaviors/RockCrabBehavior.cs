using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;
using System;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class RockCrabBehavior : MonsterBehavior
    {
        // NetState: 0 = Escondido, 1 = Activo
        // NetFlag: true = Caparazón roto (shellGone)
        // LocalData "shellHealth": Vida del caparazón

        public override void Initialize(CustomMonster monster)
        {
            monster.SetVar("shellHealth", GetCustomInt(monster, "ShellHealth", 5));
            monster.NetState.Value = 0; 
            monster.NetFlag.Value = false;
            monster.Sprite.currentFrame = 0; // Frame roca
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            bool shellGone = monster.NetFlag.Value;
            int state = monster.NetState.Value;

            if (!shellGone && state == 0) // ESCONDIDO
            {
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();
                monster.Sprite.currentFrame = 0; 

                // Despertar si el jugador está cerca
                if (IsPlayerWithinRange(monster, 2)) 
                {
                    monster.NetState.Value = 1;
                    monster.Sprite.currentFrame = 1;
                }
            }
            else // ACTIVO (Sin caparazón o persiguiendo)
            {
                monster.IsWalkingTowardPlayer = true;
                monster.moveTowardPlayerThreshold.Value = 15;

                if (monster.isMoving())
                {
                    if (shellGone) 
                    {
                        // Animación rápida sin concha (Frames 16+)
                        monster.Sprite.currentFrame = 16 + (int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 600) / 150);
                    }
                    else
                    {
                         monster.Sprite.AnimateDown(time);
                    }
                }
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            bool shellGone = monster.NetFlag.Value;
            int actualDamage = Math.Max(1, damage - (int)monster.resilience.Value);

            // 1. BOMBA (Instakill de caparazón)
            if (isBomb && !shellGone)
            {
                monster.NetFlag.Value = true;
                monster.NetState.Value = 1;
                monster.currentLocation.playSound("stoneCrack");
                return actualDamage;
            }

            // 2. PICO (Daña caparazón)
            if (!shellGone && who.CurrentTool is Pickaxe)
            {
                monster.currentLocation.playSound("hammer");
                monster.ModVar("shellHealth", -1);
                monster.shake(500);
                monster.NetState.Value = 0; // Esconderse para recibir golpe
                
                // Empuje
                Vector2 away = Utility.getAwayFromPlayerTrajectory(monster.GetBoundingBox(), who);
                monster.setTrajectory((int)away.X, (int)away.Y);

                if (monster.GetVar("shellHealth") <= 0)
                {
                    monster.NetFlag.Value = true; // Romper
                    monster.NetState.Value = 1;
                    monster.currentLocation.playSound("stoneCrack");
                    Game1.createRadialDebris(monster.currentLocation, 14, monster.TilePoint.X, monster.TilePoint.Y, 6, false);
                }
                return 0; // El pico golpea la roca, no al HP
            }

            // 3. GOLPE EN ROCA (Invulnerable)
            if (!shellGone && monster.Sprite.currentFrame == 0)
            {
                monster.currentLocation.playSound("crafting");
                return 0; 
            }

            monster.currentLocation.playSound("hitEnemy");
            return actualDamage;
        }
    }
}
