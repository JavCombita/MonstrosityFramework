using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Projectiles;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class ShooterBehavior : MonsterBehavior
    {
        // StateTimer se usa como Cooldown de disparo
        // AIState se usa para controlar movimiento (0=Mover, 1=Disparar/Quieto)

        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
            float distance = Vector2.Distance(monster.Position, monster.Player.Position);
            bool hasLineOfSight = monster.withinPlayerThreshold(10); // Simplificado

            if (distance > 350f) // Lejos: Acercarse
            {
                monster.IsWalkingTowardPlayer = true;
                monster.moveTowardPlayer(monster.Speed);
            }
            else if (distance < 120f) // Muy cerca: Alejarse (Kiting)
            {
                monster.IsWalkingTowardPlayer = false;
                // Vector opuesto al jugador
                Vector2 away = monster.Position - monster.Player.Position;
                if (away != Vector2.Zero) away.Normalize();
                
                monster.xVelocity = away.X * monster.Speed;
                monster.yVelocity = away.Y * monster.Speed;
                monster.MovePosition(time, Game1.viewport, Game1.currentLocation);
            }
            else // Rango medio: Disparar
            {
                monster.Halt();
                monster.faceGeneralDirection(monster.Player.Position);

                if (monster.StateTimer <= 0 && hasLineOfSight)
                {
                    Shoot(monster);
                    monster.StateTimer = 3000f; // 3s cooldown
                }
            }
        }

        private void Shoot(CustomMonster monster)
        {
            // Intentar leer ID de proyectil desde CustomFields, sino usar fuego por defecto
            var data = GetData(monster);
            string projectileType = "fire";
            if (data != null && data.CustomFields.ContainsKey("ProjectileType"))
                projectileType = data.CustomFields["ProjectileType"];

            Vector2 shotVelocity = Utility.getVelocityTowardPlayer(
                new Point((int)monster.Position.X, (int)monster.Position.Y), 
                10f, monster.Player
            );

            // Crear proyectil básico (bola de sombra/fuego)
            BasicProjectile projectile = new BasicProjectile(
                monster.DamageToFarmer,           
                BasicProjectile.shadowBall, // Sprite ID (puede parametrizarse)
                0, 0, 0f,                            
                shotVelocity.X, shotVelocity.Y,                
                monster.Position,                 
                "flameSpell_hit", "flameSpell",                  
                null, false, false,                         
                Game1.currentLocation, monster
            );
            
            // Si es "hielo" o algo diferente, aquí podríamos cambiar debuffs
            if (projectileType == "ice") projectile.debuff.Value = "19"; // Frozen

            Game1.currentLocation.projectiles.Add(projectile);
        }
    }
}