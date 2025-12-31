using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class RockCrabBehavior : MonsterBehavior
    {
        // 0=Escondido, 1=Despertando, 2=Caminando, 3=Sin Caparazón (Huyendo)

        public override void Update(CustomMonster monster, GameTime time)
        {
            // Mapeo de Facing a Row del sprite
            int baseRowStart = 0;
            switch(monster.FacingDirection)
            {
                case 2: baseRowStart = 0; break;  // Sur
                case 1: baseRowStart = 4; break;  // Este
                case 0: baseRowStart = 8; break;  // Norte
                case 3: baseRowStart = 12; break; // Oeste
            }

            // ESTADO 0: ESCONDIDO
            if (monster.AIState == 0) 
            {
                monster.IsInvincibleOverride = true; 
                monster.DamageToFarmer = 0;
                
                monster.Sprite.currentFrame = baseRowStart; // Frame Idle (0, 4, 8, 12)
                monster.Sprite.StopAnimation(); 
                monster.HideShadow = true; 
                monster.IsWalkingTowardPlayer = false;

                if (IsPlayerWithinRange(monster, 3)) WakeUp(monster);
            }
            // ESTADO 1: DESPERTANDO (1-3)
            else if (monster.AIState == 1) 
            {
                monster.HideShadow = false; 
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Usamos la animación de caminar Sur (1-3) como "despertar" genérico
                monster.Sprite.Animate(time, 1, 3, 150f);
                
                if (monster.StateTimer <= 0) 
                { 
                    monster.AIState = 2; // Activo
                    monster.IsInvincibleOverride = false; 
                    var data = GetData(monster);
                    monster.DamageToFarmer = data?.DamageToFarmer ?? 10;
                }
            }
            // ESTADO 2: CAMINANDO
            else if (monster.AIState == 2)
            {
                monster.HideShadow = false;
                monster.IsInvincibleOverride = false;

                if (!IsPlayerWithinRange(monster, 10)) { 
                    monster.AIState = 0; 
                    Game1.playSound("stoneStep");
                    return; 
                }
                
                MoveTowardPlayer(monster, monster.Speed);
                
                // Animación: BaseRow + frames 1, 2, 3
                if (monster.IsWalkingTowardPlayer)
                {
                    monster.Sprite.Animate(time, baseRowStart + 1, 3, 150f);
                }
            }
            // ESTADO 3: SIN CAPARAZÓN (Huyendo)
            else if (monster.AIState == 3)
            {
                monster.DamageToFarmer = 0; // Inofensivo
                monster.HideShadow = false;
                
                // HUIDA: Correr en dirección opuesta al jugador
                Vector2 away = monster.Position - monster.Player.Position;
                if (away != Vector2.Zero) away.Normalize();
                
                int fleeSpeed = monster.Speed + 2; // Más rápido
                monster.xVelocity = away.X * fleeSpeed;
                monster.yVelocity = away.Y * fleeSpeed;
                
                monster.faceGeneralDirection(monster.Position + away * 10);
                monster.MovePosition(time, Game1.viewport, Game1.currentLocation);

                // Animación: 16-19
                monster.Sprite.Animate(time, 16, 4, 100f);
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // ROMPER CAPARAZÓN CON BOMBA
            if (isBomb && monster.AIState != 3)
            {
                Game1.playSound("breakingGlass");
                monster.AIState = 3; // Huye
                monster.IsInvincibleOverride = false;
                monster.resilience.Value = 0; // Sin defensa
                return damage; // Recibe el daño de la bomba
            }

            // Si está escondido y le pegan con pico (no bomba)
            if (monster.AIState == 0)
            {
                Game1.playSound("hitRock");
                WakeUp(monster);
                return 0; // Bloquea daño inicial
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