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
            monster.Slipperiness = 8; 

            // Generamos ID único como string para 1.6
            string lightId = monster.MonsterSourceId.Value + "_" + monster.Position.ToString();
            monster.SetObj("lightId", lightId);
        }

        public override void OnUpdateAnimation(CustomMonster monster, GameTime time)
        {
            if (GetCustomInt(monster, "HasLight", 0) == 1)
            {
                string id = monster.GetObj<string>("lightId");
                Color lightColor = GetCustomColor(monster, "LightColor", new Color(64, 255, 64));

                // Lógica de Luces para Stardew Valley 1.6
                // currentLightSources es un Dictionary<string, LightSource>
                if (!Game1.currentLightSources.ContainsKey(id))
                {
                    // Constructor 1.6: LightSource(string Id, int TextureIndex, Vector2 Position, float Radius, Color Color, ...)
                    // TextureIndex 4 = Luz circular
                    Game1.currentLightSources.Add(id, new LightSource(id, 4, monster.Position + new Vector2(32f, 32f), 1.5f, lightColor));
                }
                else
                {
                    // Actualizar posición
                    Game1.currentLightSources[id].position.Value = monster.Position + new Vector2(32f, 32f);
                }
            }
        }

        public override void OnDeath(CustomMonster monster)
        {
            if (GetCustomInt(monster, "HasLight", 0) == 1)
            {
                string id = monster.GetObj<string>("lightId");
                if (id != null) Game1.currentLightSources.Remove(id);
            }
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            monster.isGlider.Value = true;

            float yOffset = (float)Math.Sin(time.TotalGameTime.TotalMilliseconds / 1000.0 * (Math.PI * 2.0)) * 20f;
            monster.drawOffset = new Vector2(0, yOffset);

            if (monster.Player != null)
            {
                Point pDiff = monster.Player.StandingPixel - monster.StandingPixel;
                float xSlope = -pDiff.X;
                float ySlope = pDiff.Y;
                float t = 400f; 
                xSlope /= t; 
                ySlope /= t;

                float maxAccel = Math.Min(4f, Math.Max(1f, 5f - t / 64f / 2f));
                
                monster.xVelocity += (-xSlope * maxAccel / 6f);
                monster.yVelocity += (-ySlope * maxAccel / 6f);

                if (Math.Abs(monster.xVelocity) > Math.Abs(-xSlope * 5f)) 
                    monster.xVelocity -= (-xSlope * maxAccel / 6f);
                if (Math.Abs(monster.yVelocity) > Math.Abs(-ySlope * 5f)) 
                    monster.yVelocity -= (-ySlope * maxAccel / 6f);
            }

            if (Math.Abs(monster.xVelocity) > Math.Abs(monster.yVelocity))
                monster.FacingDirection = monster.xVelocity > 0 ? 1 : 3;
            else
                monster.FacingDirection = monster.yVelocity > 0 ? 2 : 0;

            monster.faceDirection(monster.FacingDirection);
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            Utility.addSprinklesToLocation(monster.currentLocation, monster.TilePoint.X, monster.TilePoint.Y, 2, 2, 101, 50, Color.LightBlue);
            
            monster.xVelocity = -monster.xVelocity * 1.5f;
            monster.yVelocity = -monster.yVelocity * 1.5f;
            
            return damage;
        }
    }
}
