using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class StalkerBehavior : MonsterBehavior
    {
        // Comportamiento estándar de persecución (Shadow Brute / Golem)

        public override void Update(CustomMonster monster, GameTime time)
        {
            // 1. Obtener rango de visión (Default: 16 tiles = 1024 pixels)
            // Permitimos sobreescribir esto desde el JSON "CustomFields": { "DetectionRange": "30" }
            float detectionRange = 16f;
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue("DetectionRange", out string rangeStr))
            {
                if (float.TryParse(rangeStr, out float parsed)) detectionRange = parsed;
            }

            // 2. Comprobar distancia
            if (!IsPlayerWithinRange(monster, detectionRange))
            {
                // Si el jugador escapó, el monstruo se detiene y pierde el interés
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();
                return;
            }

            // 3. Persecución
            // moveTowardPlayer se encarga del pathfinding básico de Stardew (evitar esquinas)
            monster.IsWalkingTowardPlayer = true;
            monster.moveTowardPlayer(monster.Speed);

            // 4. Lógica de "Desatascar" (Opcional pero recomendada para mods)
            // Si el monstruo está intentando moverse pero su posición no cambia, salta un poco
            if (monster.IsWalkingTowardPlayer && (monster.xVelocity != 0 || monster.yVelocity != 0))
            {
               // Aquí podrías añadir lógica anti-stuck si notas que se traban mucho,
               // pero el método base moveTowardPlayer suele ser suficiente.
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // Reacción estándar: Al ser golpeado, se asegura de mirar al agresor
            // y se activa la persecución incluso si estaba fuera de rango (aggro forzado)
            
            monster.IsWalkingTowardPlayer = true;
            
            // Efecto visual de retroceso (Knockback) ya lo maneja la clase base Monster,
            // pero aquí podríamos añadir sonidos extra o efectos de partículas si quisiéramos.
            
            return damage;
        }
    }
}