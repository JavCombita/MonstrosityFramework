using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class SlimeBehavior : MonsterBehavior
    {
        // ESTADOS (AIState):
        // 0 = Idle / Drift (Deslizándose por el suelo)
        // 1 = Cargando (Aplastándose 16-17)
        // 2 = Saltando (Aire 18-19)
        // 3 = Aterrizaje / Cooldown

        public override void Update(CustomMonster monster, GameTime time)
        {
            // --- ESTADO 0: IDLE / DRIFT (Deslizándose) ---
            if (monster.AIState == 0)
            {
                monster.isGlider.Value = false;
                monster.Rotation = 0f;

                // 1. GESTIÓN DE ANIMACIÓN "VIVA"
                // Usamos GenericTimer para decidir cuándo cambiar de "variación de movimiento"
                monster.GenericTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.GenericTimer <= 0)
                {
                    monster.GenericTimer = Game1.random.Next(2000, 5000); // Cambiar forma cada 2-5s
                    // Elegir aleatoriamente entre los 4 sets de movimiento: 0, 4, 8, 12
                    monster.FacingDirection = Game1.random.Next(0, 4); 
                }

                // Reproducir la animación seleccionada (Sets: 0-3, 4-7, 8-11, 12-15)
                // Usamos FacingDirection temporalmente para guardar qué set usar (truco de memoria)
                int startFrame = monster.FacingDirection * 4; 
                monster.Sprite.Animate(time, startFrame, 4, 200f); 

                // 2. FÍSICA DE MOVIMIENTO (Drift Vanilla)
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.StateTimer = Game1.random.Next(1000, 2000); // Nueva decisión de movimiento
                    
                    // Lógica Vanilla-ish: Moverse aleatoriamente o hacia el jugador con inercia
                    Vector2 target = IsPlayerWithinRange(monster, 12) ? monster.Player.Position : monster.Position;
                    Vector2 trajectory = target - monster.Position;
                    
                    // Añadir aleatoriedad al vector (Drift)
                    trajectory.X += (float)Game1.random.NextDouble() * 200 - 100;
                    trajectory.Y += (float)Game1.random.NextDouble() * 200 - 100;
                    
                    if (trajectory != Vector2.Zero) trajectory.Normalize();

                    // Impulso suave
                    monster.xVelocity = trajectory.X * (monster.Speed * 0.75f); 
                    monster.yVelocity = trajectory.Y * (monster.Speed * 0.75f);
                }

                // Aplicar fricción e inercia
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                
                // Colisiones simples con bordes del mapa
                if (monster.Position.X < 0 || monster.Position.X > Game1.currentLocation.Map.DisplayWidth) monster.xVelocity *= -1;
                if (monster.Position.Y < 0 || monster.Position.Y > Game1.currentLocation.Map.DisplayHeight) monster.yVelocity *= -1;

                // 3. DECISIÓN DE SALTO
                // Si el jugador está cerca, probabilidad de atacar
                if (IsPlayerWithinRange(monster, 8) && Game1.random.NextDouble() < 0.02)
                {
                    monster.AIState = 1; // Iniciar Carga
                    monster.StateTimer = 600f; // Tiempo de carga
                    monster.Halt(); 
                    monster.xVelocity = 0;
                    monster.yVelocity = 0;
                    Game1.playSound("slimeHit");
                }
            }
            
            // --- ESTADO 1: CARGANDO (Frames 16-17) ---
            else if (monster.AIState == 1)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Animación de Aplastado (Squash)
                monster.Sprite.Animate(time, 16, 2, 150f); // Alterna rápido entre 16 y 17
                monster.shake(Game1.random.Next(1, 3));    // Vibrar
                
                if (monster.StateTimer <= 0)
                {
                    // ¡SALTO!
                    monster.AIState = 2; 
                    monster.StateTimer = 800f; // Duración máxima vuelo
                    monster.isGlider.Value = true; // Activar noclip
                    Game1.playSound("slimeJump");
                    
                    // Cálculo Balístico
                    Vector2 jumpVelocity = Utility.getVelocityTowardPlayer(
                        new Point((int)monster.Position.X, (int)monster.Position.Y), 
                        monster.Speed * 4f, // Velocidad explosiva
                        monster.Player
                    );
                    monster.xVelocity = jumpVelocity.X;
                    monster.yVelocity = jumpVelocity.Y;
                }
            }
            
            // --- ESTADO 2: EN EL AIRE (Frames 18-19) ---
            else if (monster.AIState == 2)
            {
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                monster.Position += new Vector2(monster.xVelocity, monster.yVelocity);
                
                // Animación de Vuelo/Descompresión
                monster.Sprite.Animate(time, 18, 2, 100f); // 18 y 19

                // COLISIÓN CON JUGADOR
                if (monster.GetBoundingBox().Intersects(monster.Player.GetBoundingBox()))
                {
                    monster.Player.takeDamage(monster.DamageToFarmer, false, null);
                    // Rebote fuerte
                    monster.xVelocity = -monster.xVelocity * 0.8f;
                    monster.yVelocity = -monster.yVelocity * 0.8f;
                    Land(monster, 500f); 
                    return;
                }

                // COLISIÓN CON PAREDES (Rebote)
                if (!Game1.currentLocation.isTileOnMap(monster.Position))
                {
                    monster.xVelocity = -monster.xVelocity;
                    monster.yVelocity = -monster.yVelocity;
                }

                // Aterrizaje por gravedad/tiempo
                if (monster.StateTimer <= 0)
                {
                    Land(monster, 1500f); // Cooldown largo si falla
                }
            }
            
            // --- ESTADO 3: ATERRIZAJE (Recuperación) ---
            else if (monster.AIState == 3)
            {
                monster.xVelocity = 0;
                monster.yVelocity = 0;
                
                // Animación lenta de respirar (usamos el set 0 como base)
                monster.Sprite.Animate(time, 0, 4, 300f); 
                
                monster.StateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (monster.StateTimer <= 0)
                {
                    monster.AIState = 0; // Volver a moverse
                    monster.StateTimer = Game1.random.Next(500, 1000);
                }
            }
        }

        public override int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who)
        {
            // Mecánica vanilla: Si le pegas mientras carga, puede cancelar el ataque
            if (monster.AIState == 1) 
            {
                if (Game1.random.NextDouble() < 0.4) // 40% chance de cancelar
                {
                    monster.AIState = 3; // Mandar a cooldown
                    monster.StateTimer = 500f;
                    // Knockback simple
                    Vector2 knockback = monster.Position - who.Position;
                    if (knockback != Vector2.Zero) knockback.Normalize();
                    monster.xVelocity = knockback.X * 5f;
                    monster.yVelocity = knockback.Y * 5f;
                }
            }
            return damage;
        }

        private void Land(CustomMonster monster, float cooldown)
        {
            monster.AIState = 3; 
            monster.StateTimer = cooldown;
            monster.isGlider.Value = false; 
            monster.xVelocity = 0;
            monster.yVelocity = 0;
            // Efecto visual opcional al aterrizar
            if (Game1.currentLocation != null)
                Game1.playSound("slimeHit");
        }
    }
}