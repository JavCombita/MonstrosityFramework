using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Linq;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class SlimeBehavior : MonsterBehavior
    {
        private const int MatingRange = 128;

        public override void Initialize(CustomMonster monster)
        {
            // Estado inicial Vanilla
            monster.SetVar("readyToJump", -1);
            monster.Slipperiness = 3;
            monster.SetVar("readyToMate", Game1.random.Next(10000, 120000)); // 10s a 2m
            
            // Configurar color si existe en JSON (para CustomMonster.draw)
            Color c = GetCustomColor(monster, "Tint", Color.White);
            if (c != Color.White)
            {
                monster.SetVar("TintR", c.R);
                monster.SetVar("TintG", c.G);
                monster.SetVar("TintB", c.B);
            }
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            float readyToJump = monster.GetVar("readyToJump", -1);

            // --- 1. LÓGICA DE SALTO ---
            if (readyToJump > 0)
            {
                // ESTADO: CARGANDO
                monster.ModVar("readyToJump", -time.ElapsedGameTime.Milliseconds);
                monster.Halt(); 
                monster.IsWalkingTowardPlayer = false;

                // Animación "Aplastarse" (Frames 16-19)
                int frameOffset = (800 - (int)readyToJump) / 200;
                monster.Sprite.currentFrame = 16 + Math.Max(0, Math.Min(3, frameOffset));

                // ¡SALTO!
                if (monster.GetVar("readyToJump") <= 0)
                {
                    monster.IsWalkingTowardPlayer = true;
                    monster.SetVar("readyToJump", -1);
                    monster.Slipperiness = 10; // Muy resbaladizo en el aire

                    // Física Vanilla: Invertir vector y dividir por 2
                    Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(monster.GetBoundingBox(), monster.Player);
                    monster.xVelocity = (-trajectory.X / 2f);
                    monster.yVelocity = (-trajectory.Y / 2f);

                    if (Utility.isOnScreen(monster.Position, 128)) monster.currentLocation.localSound("slime");
                    monster.Sprite.currentFrame = 1; // Frame salto
                }
                return; // Importante: No procesar movimiento normal ni apareamiento mientras carga
            }
            
            // Si no está cargando, movimiento normal
            if (readyToJump <= 0)
            {
                // Animar si se mueve
                if (Math.Abs(monster.xVelocity) >= 0.5f || Math.Abs(monster.yVelocity) >= 0.5f)
                    monster.Sprite.AnimateDown(time);
                
                // Reducir fricción al aterrizar
                if (monster.Slipperiness > 3 && Math.Abs(monster.xVelocity) < 0.2f && Math.Abs(monster.yVelocity) < 0.2f)
                    monster.Slipperiness = 3;

                monster.moveTowardPlayerThreshold.Value = 8;
                
                // DECISIÓN DE SALTAR (Random tick chance)
                // Usamos GetCustomFloat para permitir Slimes "saltarines" (Config JSON: JumpChance)
                if (IsPlayerWithinRange(monster, GetVisionRange(monster, 8)) && 
                    Game1.random.NextDouble() < GetCustomFloat(monster, "JumpChance", 0.01f))
                {
                    monster.SetVar("readyToJump", GetCustomInt(monster, "JumpChargeTime", 800));
                }
            }

            // --- 2. LÓGICA DE REPRODUCCIÓN (MATING) ---
            // Solo si está activado en JSON: "CanMate": "1"
            if (readyToJump <= 0 && GetCustomInt(monster, "CanMate", 0) == 1)
            {
                ProcessMating(monster, time);
            }
        }

        private void ProcessMating(CustomMonster monster, GameTime time)
        {
            float mateTimer = monster.GetVar("readyToMate");
            CustomMonster mate = monster.GetObj<CustomMonster>("Mate");

            // A) Ya tiene pareja
            if (mate != null)
            {
                if (mate.Health <= 0 || mate.currentLocation != monster.currentLocation)
                {
                    monster.RemoveObj("Mate");
                    return;
                }

                monster.IsWalkingTowardPlayer = false;
                
                // CORRECCIÓN AQUÍ: Convertir Point a Vector2 explícitamente
                Vector2 myCenter = new Vector2(monster.GetBoundingBox().Center.X, monster.GetBoundingBox().Center.Y);
                Vector2 mateCenter = new Vector2(mate.GetBoundingBox().Center.X, mate.GetBoundingBox().Center.Y);

                Vector2 trajectory = Utility.getVelocityTowardPoint(myCenter, mateCenter, monster.Speed);
                monster.xVelocity = trajectory.X;
                monster.yVelocity = -trajectory.Y; 

                if (Vector2.Distance(monster.Position, mate.Position) < 64f)
                {
                    SpawnBaby(monster, mate);
                    ResetMating(monster);
                    ResetMating(mate);
                    mate.RemoveObj("Mate");
                    monster.RemoveObj("Mate");
                }
            }
            // B) Buscando pareja (Cooldown)
            else if (mateTimer > 0)
            {
                monster.ModVar("readyToMate", -time.ElapsedGameTime.Milliseconds);
            }
            // C) Buscar nueva pareja
            else
            {
                var candidate = monster.currentLocation.characters.OfType<CustomMonster>().FirstOrDefault(c => 
                    c != monster &&
                    c.MonsterSourceId.Value == monster.MonsterSourceId.Value &&
                    c.GetVar("readyToMate") <= 0 &&
                    c.GetObj<CustomMonster>("Mate") == null &&
                    Vector2.Distance(monster.Position, c.Position) < MatingRange * 2
                );

                if (candidate != null)
                {
                    monster.SetObj("Mate", candidate);
                    candidate.SetObj("Mate", monster);
                    monster.doEmote(20); // Corazón
                    candidate.doEmote(20);
                }
            }
        }

        private void SpawnBaby(CustomMonster p1, CustomMonster p2)
        {
            var baby = new CustomMonster(p1.MonsterSourceId.Value, p1.Position);
            baby.ReloadData();
            baby.MaxHealth = (p1.MaxHealth + p2.MaxHealth) / 2;
            baby.Health = baby.MaxHealth;
            baby.DamageToFarmer = Math.Max(1, (p1.DamageToFarmer + p2.DamageToFarmer) / 2);
            baby.Scale = 0.6f; // Pequeño
            p1.currentLocation.characters.Add(baby);
            p1.currentLocation.playSound("slime");
        }

        private void ResetMating(CustomMonster m)
        {
            m.SetVar("readyToMate", 120000); // 2 min cooldown
            m.IsWalkingTowardPlayer = true;
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            monster.SetVar("readyToJump", -1); // Cancelar salto al ser golpeado
            monster.IsWalkingTowardPlayer = true;
            monster.Slipperiness = 3;
            monster.currentLocation.playSound("slimeHit");
            return damage;
        }
    }
}
