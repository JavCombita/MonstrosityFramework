using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class BatBehavior : MonsterBehavior
    {
        private const float RotationIncrement = (float)Math.PI / 64f;

        public override void Initialize(CustomMonster monster)
        {
            monster.HideShadow = true;
            monster.isGlider.Value = true;
            
            // Variables de Estado Vanilla
            monster.SetVar("wasHitCounter", 0);
            monster.SetVar("turningRight", 0); // 0: false, 1: true
            
            // Variables para Lunge (Embestida tipo Magma Sprite)
            // Se leen del JSON o usan defaults
            if (GetCustomInt(monster, "CanLunge", 0) == 1)
            {
                monster.SetVar("nextLunge", 2000); 
                monster.SetVar("lungeTimer", 0);
                monster.SetVar("isLunging", 0);
            }
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.isGlider.Value = true; // Asegurar que vuele

            // --- 1. LÓGICA DE EMBESTIDA (LUNGE) ---
            if (GetCustomInt(monster, "CanLunge", 0) == 1)
            {
                float nextLunge = monster.GetVar("nextLunge");
                float lungeTimer = monster.GetVar("lungeTimer");
                bool isLunging = monster.GetVar("isLunging") == 1;

                if (isLunging)
                {
                    // Desaceleración suave
                    monster.xVelocity = Utility.MoveTowards(monster.xVelocity, 0f, 0.5f);
                    monster.yVelocity = Utility.MoveTowards(monster.yVelocity, 0f, 0.5f);

                    if (Math.Abs(monster.xVelocity) < 1f && Math.Abs(monster.yVelocity) < 1f)
                    {
                        monster.SetVar("isLunging", 0);
                        monster.SetVar("nextLunge", 3000); // Reset cooldown
                    }
                    return; // Si está embistiendo, salta el resto del movimiento
                }
                else if (lungeTimer > 0)
                {
                    // Cargando la embestida (Vibración)
                    monster.ModVar("lungeTimer", -time.ElapsedGameTime.Milliseconds);
                    monster.Halt();
                    monster.shake(10); 

                    if (monster.GetVar("lungeTimer") <= 0)
                    {
                        // ¡LANZARSE!
                        float lungeSpeed = GetCustomFloat(monster, "LungeSpeed", 25f);
                        Vector2 target = Utility.getVelocityTowardPlayer(monster.GetBoundingBox().Center, lungeSpeed, monster.Player);
                        monster.xVelocity = target.X;
                        monster.yVelocity = -target.Y; // Stardew usa Y invertida a veces en cálculos de velocidad
                        monster.SetVar("isLunging", 1);
                        monster.currentLocation.playSound("throw");
                    }
                    return;
                }
                else if (nextLunge > 0)
                {
                    monster.ModVar("nextLunge", -time.ElapsedGameTime.Milliseconds);
                }
                else if (IsPlayerWithinRange(monster, 6)) // Rango de activación
                {
                    monster.SetVar("lungeTimer", 500); // 0.5s tiempo de carga
                    monster.currentLocation.playSound("magma_sprite_spot");
                }
            }

            // --- 2. LÓGICA DE ESPIRAL (AL SER GOLPEADO) ---
            float hitCounter = monster.GetVar("wasHitCounter");
            if (hitCounter > 0)
            {
                monster.ModVar("wasHitCounter", -time.ElapsedGameTime.Milliseconds);

                // Cálculo matemático de Bat.cs para huir en espiral
                float xSlope = -(monster.Player.StandingPixel.X - monster.StandingPixel.X);
                float ySlope = monster.Player.StandingPixel.Y - monster.StandingPixel.Y;
                float t = Math.Max(1f, Math.Abs(xSlope) + Math.Abs(ySlope));
                xSlope /= t; 
                ySlope /= t;

                float targetRotation = (float)Math.Atan2(-ySlope, xSlope) - (float)Math.PI / 2f;
                float currentRotation = monster.rotation;
                bool turningRight = monster.GetVar("turningRight") == 1;

                if (Math.Abs(targetRotation) - Math.Abs(currentRotation) > Math.PI * 7.0 / 8.0 && Game1.random.NextBool())
                    turningRight = true;
                else if (Math.Abs(targetRotation) - Math.Abs(currentRotation) < Math.PI / 8.0)
                    turningRight = false;
                
                monster.SetVar("turningRight", turningRight ? 1 : 0);

                if (turningRight)
                    monster.rotation -= (float)Math.Sign(targetRotation - currentRotation) * RotationIncrement;
                else
                    monster.rotation += (float)Math.Sign(targetRotation - currentRotation) * RotationIncrement;

                monster.rotation %= (float)Math.PI * 2f;
            }

            // --- 3. MOVIMIENTO NORMAL ---
            float maxAccel = Math.Min(5f, Math.Max(1f, 5f - 400f / 64f / 2f));
            float xComp = (float)Math.Cos(monster.rotation + Math.PI / 2.0);
            float yComp = -(float)Math.Sin(monster.rotation + Math.PI / 2.0);

            monster.xVelocity += (-xComp) * maxAccel / 6f + (float)Game1.random.Next(-10, 10) / 100f;
            monster.yVelocity += (-yComp) * maxAccel / 6f + (float)Game1.random.Next(-10, 10) / 100f;

            // Fricción
            if (Math.Abs(monster.xVelocity) > Math.Abs(-xComp * monster.Speed))
                monster.xVelocity -= (-xComp) * maxAccel / 6f;
            if (Math.Abs(monster.yVelocity) > Math.Abs(-yComp * monster.Speed))
                monster.yVelocity -= (-yComp) * maxAccel / 6f;

            // --- 4. ANIMACIÓN Y SUEÑO ---
            // Si detecta al jugador o fue golpeado -> Vuela
            float vision = GetVisionRange(monster, 6);
            if (IsPlayerWithinRange(monster, vision) || hitCounter > 0)
            {
                monster.Sprite.Animate(time, 0, 4, 80f);
                
                // Sonido aleteo
                if (monster.Sprite.currentFrame % 3 == 0 && Utility.isOnScreen(monster.Position, 512) && Game1.random.NextDouble() < 0.05)
                {
                    monster.currentLocation.localSound("batFlap"); 
                }
            }
            else
            {
                // Durmiendo
                monster.Sprite.currentFrame = 4;
                monster.xVelocity = 0; 
                monster.yVelocity = 0;
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            monster.SetVar("wasHitCounter", 500); // Activar espiral 500ms
            monster.currentLocation.playSound("batScreech");
            return damage;
        }
    }
}
