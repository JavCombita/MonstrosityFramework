using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Projectiles;
using System;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class ShooterBehavior : MonsterBehavior
    {
        // NetFlag: true = Disparando (Shooting state)
        // LocalData "shotsLeft": Disparos restantes en la ráfaga
        // LocalData "nextShot": Timer para la siguiente acción

        public override void Initialize(CustomMonster monster)
        {
            monster.SetVar("shotsLeft", 0);
            monster.SetVar("nextShot", 2f); 
            monster.NetFlag.Value = false; 
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.Player == null) return;
            
            bool shooting = monster.NetFlag.Value;
            float nextShot = monster.GetVar("nextShot");
            int shotsLeft = (int)monster.GetVar("shotsLeft");

            // --- ESTADO: MOVERSE ---
            if (!shooting)
            {
                if (nextShot > 0f) monster.ModVar("nextShot", -(float)time.ElapsedGameTime.TotalSeconds);
                else
                {
                    // Check rango para iniciar disparo
                    if (IsPlayerWithinRange(monster, 8))
                    {
                        monster.Halt();
                        monster.IsWalkingTowardPlayer = false;
                        monster.faceGeneralDirection(monster.Player.getStandingPosition());
                        
                        monster.NetFlag.Value = true; // Entrar modo disparo
                        
                        // Configuración del disparo desde JSON
                        monster.SetVar("nextShot", GetCustomFloat(monster, "AimTime", 0.25f)); 
                        monster.SetVar("shotsLeft", GetCustomInt(monster, "BurstCount", 1));
                    }
                    else
                    {
                        monster.IsWalkingTowardPlayer = true; // Perseguir
                    }
                }
            }
            // --- ESTADO: DISPARANDO ---
            else
            {
                monster.xVelocity = 0f; monster.yVelocity = 0f; // Quieto

                if (shotsLeft > 0)
                {
                    monster.ModVar("nextShot", -(float)time.ElapsedGameTime.TotalSeconds);
                    if (monster.GetVar("nextShot") <= 0f)
                    {
                        // ¡FUEGO!
                        Fire(monster);
                        monster.ModVar("shotsLeft", -1);
                        
                        // Si quedan disparos, usar BurstTime, si no, AimEndTime
                        if (monster.GetVar("shotsLeft") == 0)
                            monster.SetVar("nextShot", 1f); // End time
                        else
                            monster.SetVar("nextShot", 0.25f); // Burst interval default
                    }
                }
                else // Terminó ráfaga
                {
                    if (monster.GetVar("nextShot") > 0f)
                        monster.ModVar("nextShot", -(float)time.ElapsedGameTime.TotalSeconds);
                    else
                    {
                        monster.NetFlag.Value = false; // Volver a moverse
                        monster.SetVar("nextShot", 2f); // Cooldown global
                        monster.IsWalkingTowardPlayer = true;
                    }
                }
            }

            // Animación (Frames de disparo 16-19)
            if (monster.NetFlag.Value)
            {
                switch (monster.FacingDirection)
                {
                    case 2: monster.Sprite.currentFrame = 16; break;
                    case 1: monster.Sprite.currentFrame = 17; break;
                    case 0: monster.Sprite.currentFrame = 18; break;
                    case 3: monster.Sprite.currentFrame = 19; break;
                }
            }
            else if (monster.isMoving())
            {
                monster.Sprite.Animate(time, 0, 4, 150f);
            }
        }

        private void Fire(CustomMonster monster)
        {
            Vector2 velocity = Vector2.Zero;
            switch (monster.FacingDirection)
            {
                case 0: velocity = new Vector2(0f, -1f); break;
                case 3: velocity = new Vector2(-1f, 0f); break;
                case 1: velocity = new Vector2(1f, 0f); break;
                case 2: velocity = new Vector2(0f, 1f); break;
            }
            
            // JSON: Velocidad
            velocity *= GetCustomFloat(monster, "ProjectileSpeed", 12f);

            // JSON: Sprite ID (12 = Shadow Ball)
            int spriteId = GetCustomInt(monster, "ProjectileSprite", 12);

            BasicProjectile p = new BasicProjectile(
                monster.DamageToFarmer, 
                spriteId, 
                0, 0, 0f, 
                velocity.X, velocity.Y, 
                monster.Position, 
                null, null, null, false, false, 
                monster.currentLocation, monster);
            
            p.height.Value = 24f;
            p.ignoreTravelGracePeriod.Value = true;
            p.IgnoreLocationCollision = true;
            
            // JSON: Distancia
            p.maxTravelDistance.Value = 64 * GetCustomInt(monster, "ProjectileRange", 10);
            
            // JSON: Debuff ("26" = Darkness)
            p.debuff.Value = GetCustomString(monster, "ProjectileDebuff", "26");

            monster.currentLocation.projectiles.Add(p);

            // JSON: Sonido
            monster.currentLocation.playSound(GetCustomString(monster, "ShootSound", "Cowboy_gunshot"));
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            monster.NetFlag.Value = false; // Cancelar disparo
            monster.SetVar("shotsLeft", 0);
            // Pequeño delay al ser golpeado
            monster.SetVar("nextShot", Math.Max(0.5f, monster.GetVar("nextShot")));
            monster.currentLocation.playSound("shadowHit");
            return damage;
        }
    }
}
