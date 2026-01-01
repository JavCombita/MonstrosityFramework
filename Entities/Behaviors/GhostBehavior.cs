using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Linq;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class GhostBehavior : MonsterBehavior
    {
        public override void Initialize(CustomMonster monster)
        {
            monster.isGlider.Value = true;
            monster.HideShadow = true;
            monster.Slipperiness = 8; // Ghost Vanilla es muy resbaladizo

            // Generar ID único para la luz (basado en posición inicial)
            int lightId = (int)monster.Position.X * 1000 + (int)monster.Position.Y + Game1.random.Next(100);
            monster.SetVar("lightId", lightId);
        }

        public override void OnUpdateAnimation(CustomMonster monster, GameTime time)
        {
            // Lógica de Iluminación (Configurable por JSON)
            if (GetCustomInt(monster, "HasLight", 0) == 1)
            {
                int id = (int)monster.GetVar("lightId");
                // Leemos color del JSON o usamos un amarillo verdoso por defecto
                Color lightColor = GetCustomColor(monster, "LightColor", new Color(64, 255, 64));

                LightSource light = Game1.currentLightSources.FirstOrDefault(l => l.identifier == id);
                if (light == null)
                {
                    light = new LightSource(4, monster.Position, 1.5f, lightColor, id, LightSource.LightContext.None, 0L);
                    Game1.currentLightSources.Add(light);
                }
                
                // Actualizar posición de la luz
                light.position.Value = monster.Position + new Vector2(32f, 32f);
            }
        }

        public override void OnDeath(CustomMonster monster)
        {
            // Limpiar luz
            if (GetCustomInt(monster, "HasLight", 0) == 1)
            {
                int id = (int)monster.GetVar("lightId");
                Utility.removeLightSource(id);
            }
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.isGlider.Value = true;

            // Oscilación visual (Ghost.cs) -> yOffset
            float yOffset = (float)Math.Sin(time.TotalGameTime.TotalMilliseconds / 1000.0 * (Math.PI * 2.0)) * 20f;
            monster.drawOffset = new Vector2(0, yOffset);

            if (monster.Player != null)
            {
                // Cálculo de trayectoria suavizada
                Point pDiff = monster.Player.StandingPixel - monster.StandingPixel;
                float xSlope = -pDiff.X;
                float ySlope = pDiff.Y;
                float t = 400f; // Constante de inercia
                xSlope /= t; 
                ySlope /= t;

                float maxAccel = Math.Min(4f, Math.Max(1f, 5f - t / 64f / 2f));
                
                monster.xVelocity += (-xSlope * maxAccel / 6f);
                monster.yVelocity += (-ySlope * maxAccel / 6f);

                // Fricción
                if (Math.Abs(monster.xVelocity) > Math.Abs(-xSlope * 5f)) 
                    monster.xVelocity -= (-xSlope * maxAccel / 6f);
                if (Math.Abs(monster.yVelocity) > Math.Abs(-ySlope * 5f)) 
                    monster.yVelocity -= (-ySlope * maxAccel / 6f);
            }

            // Orientación basada en velocidad mayor
            if (Math.Abs(monster.xVelocity) > Math.Abs(monster.yVelocity))
                monster.FacingDirection = monster.xVelocity > 0 ? 1 : 3;
            else
                monster.FacingDirection = monster.yVelocity > 0 ? 2 : 0;

            monster.faceDirection(monster.FacingDirection);
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // Efecto visual y empuje extra al ser golpeado
            Utility.addSprinklesToLocation(monster.currentLocation, monster.TilePoint.X, monster.TilePoint.Y, 2, 2, 101, 50, Color.LightBlue);
            
            monster.xVelocity = -monster.xVelocity * 1.5f;
            monster.yVelocity = -monster.yVelocity * 1.5f;
            
            return damage;
        }
    }
}
