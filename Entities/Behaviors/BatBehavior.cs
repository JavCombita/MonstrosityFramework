using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class BatBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            // Asegurar que vuela
            if (!monster.isGlider.Value) monster.isGlider.Value = true;
            
            // Animación constante
            monster.Sprite.Animate(time, 0, 4, 80f);

            if (IsPlayerWithinRange(monster, 18)) 
            {
                Vector2 trajectory = monster.Player.Position - monster.Position;
                if (trajectory.LengthSquared() > 0.0001f) trajectory.Normalize();

                // Física de aceleración (Inercia)
                float acceleration = 0.15f; // Ajustable
                float maxSpeed = monster.Speed * 2f;

                // Modificar velocidad gradualmente hacia el jugador
                if (monster.xVelocity < trajectory.X * maxSpeed) monster.xVelocity += acceleration;
                else if (monster.xVelocity > trajectory.X * maxSpeed) monster.xVelocity -= acceleration;
                
                if (monster.yVelocity < trajectory.Y * maxSpeed) monster.yVelocity += acceleration;
                else if (monster.yVelocity > trajectory.Y * maxSpeed) monster.yVelocity -= acceleration;
            }
            
            // Aplicar movimiento manual (CustomMonster no usa moveTowardPlayer para vuelo inercial)
            monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
            
            // Colisión suave (evita que se peguen infinitamente)
            if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
            {
                // Pequeño rebote al tocar al jugador
                monster.xVelocity = -monster.xVelocity * 0.5f;
                monster.yVelocity = -monster.yVelocity * 0.5f;
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // Al recibir daño, los murciélagos suelen alejarse un poco (knockback natural del juego),
            // pero podríamos forzar un estado de "huida" si quisiéramos.
            // Por ahora, dejamos que el sistema de knockback vanilla actúe.
            return damage;
        }
    }
}