using Microsoft.Xna.Framework;
using StardewValley;
using System;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class DuggyBehavior : MonsterBehavior
    {
        // Estados: 
        // 0 = Bajo tierra (NetState)
        // 1 = Arriba atacando
        
        public override void Initialize(CustomMonster monster)
        {
            monster.NetState.Value = 0; 
            monster.HideShadow = true;
            monster.IsInvisible = true;
            monster.IsInvincibleOverride = true; // Propiedad custom en CustomMonster
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            if (monster.NetState.Value == 0) // BAJO TIERRA
            {
                monster.IsInvisible = true;
                monster.HideShadow = true;
                monster.DamageToFarmer = 0; 
                monster.IsInvincibleOverride = true; 

                // Hack visual: Mantener HP lleno mientras está abajo
                if (monster.Health < monster.MaxHealth) monster.Health = monster.MaxHealth;

                // Detección (Duggy.cs check)
                if (IsPlayerWithinRange(monster, 3))
                {
                    // IMPORTANTE: Moverse debajo del jugador antes de salir
                    monster.Position = monster.Player.Position; 
                    
                    monster.NetState.Value = 1;
                    monster.StateTimer = 0; // Timer de animación manual
                    monster.currentLocation.playSound("Duggy");
                    monster.IsInvisible = false;
                }
            }
            else // ARRIBA (ATACANDO)
            {
                monster.IsInvisible = false;
                monster.HideShadow = false;
                monster.IsInvincibleOverride = false;
                monster.DamageToFarmer = GetData(monster)?.DamageToFarmer ?? 8;

                // Control de animación manual basado en tiempo
                monster.StateTimer += time.ElapsedGameTime.Milliseconds;
                float t = monster.StateTimer;

                if (t < 400) 
                {
                    // Saliendo (Frames 0-3)
                    monster.Sprite.currentFrame = (int)(t / 100); 
                }
                else if (t < 1000) 
                {
                    // Idle/Atacando (Frames 4-7)
                    monster.Sprite.currentFrame = 4 + (int)((t - 400) / 150) % 4;
                }
                else if (t < 1400) 
                {
                    // Bajando (Frames invertidos 3-0)
                    monster.Sprite.currentFrame = 3 - (int)((t - 1000) / 100);
                }
                else 
                {
                    // Volver a esconderse
                    monster.NetState.Value = 0; 
                    monster.Position = new Vector2(-1000, -1000); // Mover lejos visualmente
                }
            }
        }
    }
}
