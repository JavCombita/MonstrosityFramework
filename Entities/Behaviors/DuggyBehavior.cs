using Microsoft.Xna.Framework;
using StardewValley;
using MonstrosityFramework.Entities; // Importante para acceder a CustomMonster

namespace MonstrosityFramework.Entities.Behaviors
{
    public class DuggyBehavior : MonsterBehavior
    {
        // NetState: 0 = Bajo tierra (Invencible), 1 = Arriba atacando
        
        public override void Initialize(CustomMonster monster)
        {
            monster.NetState.Value = 0; 
            monster.HideShadow = true;
            monster.IsInvisible = true;
            monster.IsInvincibleOverride = true; 
            monster.SetVar("animState", 0f); // Usamos LocalData para el timer de animación
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.NetState.Value == 0) // BAJO TIERRA
            {
                monster.IsInvisible = true;
                monster.HideShadow = true;
                monster.DamageToFarmer = 0; 
                monster.IsInvincibleOverride = true; 

                if (monster.Health < monster.MaxHealth) monster.Health = monster.MaxHealth;

                // FIX: Usar GetVisionRange para respetar "DetectionRange" del JSON
                if (IsPlayerWithinRange(monster, GetVisionRange(monster, 3)))
                {
                    monster.Position = monster.Player.Position; 
                    monster.NetState.Value = 1;
                    monster.SetVar("animState", 0f); // Reset timer
                    monster.currentLocation.playSound("Duggy");
                    monster.IsInvisible = false;
                }
            }
            else // ARRIBA
            {
                monster.IsInvisible = false;
                monster.HideShadow = false;
                monster.IsInvincibleOverride = false;
                monster.DamageToFarmer = GetData(monster)?.DamageToFarmer ?? 8;

                // Animación Manual
                monster.ModVar("animState", time.ElapsedGameTime.Milliseconds);
                float t = monster.GetVar("animState");

                if (t < 400) 
                {
                    monster.Sprite.currentFrame = (int)(t / 100); 
                }
                else if (t < 1000) 
                {
                    monster.Sprite.currentFrame = 4 + (int)((t - 400) / 150) % 4;
                }
                else if (t < 1400) 
                {
                    monster.Sprite.currentFrame = 3 - (int)((t - 1000) / 100);
                }
                else 
                {
                    monster.NetState.Value = 0; // Volver a tierra
                    monster.Position = new Vector2(-1000, -1000); 
                }
            }
        }
    }
}
