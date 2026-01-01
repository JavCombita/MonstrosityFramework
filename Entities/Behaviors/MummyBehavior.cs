using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;
using System;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class MummyBehavior : MonsterBehavior
    {
        // NetState: 0 = Viva, 1 = Arrugada (Muerta temporalmente)
        // NetTimer: Tiempo para revivir (sincronizado)

        public override void Initialize(CustomMonster monster)
        {
            monster.NetState.Value = 0;
            monster.Sprite.currentFrame = 0;
            monster.DamageToFarmer = GetData(monster)?.DamageToFarmer ?? 20;
        }

        public override void Update(CustomMonster monster, GameTime time)
        {
            // --- ESTADO: ARRUGADA (CRUMPLED) ---
            if (monster.NetState.Value == 1) 
            {
                monster.NetTimer.Value -= time.ElapsedGameTime.Milliseconds;
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();

                // Vibrar antes de revivir (< 2 seg)
                if (monster.NetTimer.Value < 2000) 
                    monster.shake((int)monster.NetTimer.Value);

                // REVIVIR
                if (monster.NetTimer.Value <= 0)
                {
                    monster.NetState.Value = 0;
                    monster.Health = monster.MaxHealth; // Restaurar vida real
                    monster.currentLocation.localSound("monsterdead"); 
                    monster.Sprite.currentFrame = 0; 
                    monster.IsWalkingTowardPlayer = true;
                }
                else
                {
                    // Mantener frame de "pila de trapos"
                    monster.Sprite.currentFrame = 19; 
                }
            }
            // --- ESTADO: VIVA ---
            else 
            {
                monster.IsWalkingTowardPlayer = true;
                monster.moveTowardPlayerThreshold.Value = 16;
                
                if (monster.isMoving()) 
                    monster.Sprite.AnimateDown(time);
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // A) Si ya está Arrugada
            if (monster.NetState.Value == 1)
            {
                if (isBomb)
                {
                    // La bomba mata definitivamente
                    monster.Health = 0; 
                    monster.currentLocation.playSound("ghost");
                    Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(44, monster.Position, Color.BlueViolet, 10) { holdLastFrame = true }, monster.currentLocation);
                    return 999; // Daño letal
                }
                return -1; // Invulnerable a armas mientras está en el suelo
            }

            // B) Si está Viva
            int actualDamage = Math.Max(1, damage - (int)monster.resilience.Value);
            int projectedHealth = monster.Health - actualDamage;

            monster.currentLocation.playSound("shadowHit");
            monster.currentLocation.playSound("skeletonStep");

            // Si el daño es fatal...
            if (projectedHealth <= 0)
            {
                // Verificar Encantamiento Cruzado (Crusader)
                bool hasCrusader = false;
                if (who.CurrentTool is MeleeWeapon weapon)
                {
                    foreach(var ench in weapon.enchantments)
                    {
                        if (ench is CrusaderEnchantment) hasCrusader = true;
                    }
                }

                // SI NO es bomba Y NO tiene Crusader -> Se Arruga (No muere)
                if (!isBomb && !hasCrusader)
                {
                    monster.NetState.Value = 1;
                    
                    // Tiempo de resurrección configurable (Default 10s)
                    monster.NetTimer.Value = GetCustomInt(monster, "RevivalTime", 10000); 
                    
                    monster.Health = monster.MaxHealth; // Reset HP (visual)
                    monster.currentLocation.localSound("skeletonDie");
                    monster.Sprite.currentFrame = 19;
                    
                    return 0; // Anulamos el daño letal del juego
                }
                
                // Si es bomba o Crusader, muere de verdad (dejar pasar el daño normal)
            }

            return actualDamage;
        }
    }
}
