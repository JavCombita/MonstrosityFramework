using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System.Xml.Serialization;
using MonstrosityFramework.Framework.Registries;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace MonstrosityFramework.Entities
{
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        public readonly NetString MonsterSourceId = new();
        
        // --- VARIABLES DE ESTADO INTERNO ---
        private bool _hasLoadedData = false;
        
        // Control genérico de estados (usado por todas las IAs)
        private float _stateTimer = 0f;
        private int _aiState = 0; 
        
        // IA Tirador
        private float _fireCooldown = 0f;
        
        // IA Fantasma/Volador
        private bool _wasHitRecently = false;
        private float _runAwayTimer = 0f;

        // IA Serpiente
        private Vector2 _velocity = Vector2.Zero;

        public CustomMonster() : base() 
        {
            EnsureFallbackTexture();
        }

        public CustomMonster(string uniqueId, Vector2 position) : base()
        {
            this.MonsterSourceId.Value = uniqueId;
            this.Position = position;
            this.ReloadData();
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            this.NetFields.AddField(MonsterSourceId);
            this.MonsterSourceId.fieldChangeVisibleEvent += (_, _, _) => { _hasLoadedData = false; ReloadData(); };
        }

        public void ReloadData()
        {
            _hasLoadedData = true;
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null) { EnsureFallbackTexture(); return; }

            // Stats
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            
            string behavior = entry.Data.BehaviorType ?? "Default";
            
            // Configurar si atraviesa paredes
            if (behavior == "Bat" || behavior == "Ghost" || behavior == "Serpent" || behavior == "Slime") // Slime salta obstáculos
            {
                this.IsGlider = true;
            }
            else
            {
                this.IsGlider = false;
            }

            // Sprite
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            
            try
            {
                Texture2D customTex = entry.GetTexture();
                if (customTex != null) this.Sprite.spriteTexture = customTex;
                else EnsureFallbackTexture();
            }
            catch (Exception) { EnsureFallbackTexture(); }
            
            this.HideShadow = false;
        }

        private void EnsureFallbackTexture()
        {
            if (this.Sprite == null) this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
            if (this.Sprite.spriteTexture == null)
            {
                try { this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); } catch { }
            }
        }

        // ============================================================================================
        // 🧠 CEREBRO CENTRAL
        // ============================================================================================
        public override void behaviorAtGameTick(GameTime time)
        {
            if (!_hasLoadedData || this.Sprite?.spriteTexture == null)
            {
                if (!string.IsNullOrEmpty(MonsterSourceId.Value)) ReloadData();
                if (this.Sprite?.spriteTexture == null) return;
            }

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";
            int speed = entry?.Data.Speed ?? 2;

            switch (behavior)
            {
                case "Slime":       BehaviorSlime(time, speed); break; // <--- ¡NUEVO!
                case "Bat":         BehaviorBat(time, speed); break;
                case "Ghost":       BehaviorGhost(time, speed); break;
                case "Shooter":     BehaviorShooter(time, speed); break;
                case "RockCrab":    BehaviorRockCrab(time, speed); break;
                case "Duggy":       BehaviorDuggy(time, speed); break;
                case "Serpent":     BehaviorSerpent(time, speed); break;
                case "Tank":        BehaviorTank(time, speed); break;
                case "Stalker":     
                default:            BehaviorStalker(time, speed); break;
            }
        }

        // ============================================================================================
        // 🟢 IA SLIME (CARGA Y SALTO FÍSICO)
        // ============================================================================================
        private void BehaviorSlime(GameTime time, int speed)
        {
            // _aiState: 
            // 0 = Idle/Caminando (Cooldown)
            // 1 = Cargando Salto (Winding up)
            // 2 = Saltando (En el aire/Lunge)

            if (_aiState == 0) // --- FASE IDLE ---
            {
                this.IsGlider = false; // En el suelo respeta colisiones
                
                // Moverse muy lentamente o aleatoriamente
                if (withinPlayerThreshold(12))
                {
                    if (_stateTimer > 0)
                    {
                        _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                    }
                    else
                    {
                        // Intentar iniciar un salto (Probabilidad aleatoria para no ser un robot)
                        if (Game1.random.NextDouble() < 0.02) 
                        {
                            _aiState = 1; // Pasamos a cargar
                            _stateTimer = 600f; // Tiempo de carga (0.6 segundos)
                            Game1.playSound("slimeHit"); // Sonido de "squish" preparándose
                            this.Halt(); // Se detiene para cargar
                        }
                        else
                        {
                            // Pequeños pasos erráticos hacia el jugador
                            base.moveTowardPlayer(1);
                        }
                    }
                }
            }
            else if (_aiState == 1) // --- FASE DE CARGA (WIND UP) ---
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                // Efecto visual: Vibrar para indicar peligro
                this.shake(Game1.random.Next(1, 3)); 

                if (_stateTimer <= 0)
                {
                    // ¡LANZAMIENTO!
                    _aiState = 2;
                    _stateTimer = 500f; // Duración máxima del impulso
                    
                    // Calculamos vector hacia el jugador
                    Vector2 target = this.Player.Position;
                    // Velocidad explosiva (speed * 4)
                    Vector2 velocity = Utility.getVelocityTowardPlayer(new Point((int)this.Position.X, (int)this.Position.Y), speed * 4f, this.Player);
                    
                    this.xVelocity = velocity.X;
                    this.yVelocity = velocity.Y;
                    
                    this.IsGlider = true; // Ignora colisiones pequeñas y agua al saltar
                    Game1.playSound("slimeJump");
                }
            }
            else if (_aiState == 2) // --- FASE DE SALTO (LUNGE) ---
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;

                // Aplicar movimiento físico manual (ignorando pathfinding)
                this.Position.X += this.xVelocity;
                this.Position.Y += this.yVelocity;

                // Fricción (Air Drag): Reducir velocidad gradualmente
                if (_stateTimer < 250) 
                {
                    this.xVelocity *= 0.90f;
                    this.yVelocity *= 0.90f;
                }

                // Detectar colisión con jugador manualmente para asegurar daño
                if (this.GetBoundingBox().Intersects(this.Player.GetBoundingBox()))
                {
                    // El daño base lo maneja el juego, pero podemos forzar el fin del salto
                    this.xVelocity = -this.xVelocity * 0.5f; // Rebote
                    this.yVelocity = -this.yVelocity * 0.5f;
                    _aiState = 0;
                    _stateTimer = 1000f; // Cooldown post-golpe
                }

                // Fin del salto por tiempo o si casi se detuvo
                if (_stateTimer <= 0 || (Math.Abs(xVelocity) < 0.5f && Math.Abs(yVelocity) < 0.5f))
                {
                    _aiState = 0; // Aterrizar
                    _stateTimer = Game1.random.Next(1000, 2000); // Cooldown aleatorio entre saltos (1-2s)
                }
            }

            // Animación (Opcional: puedes definir frames específicos en tu JSON si quieres)
            // Slime vanilla usa frames específicos, pero para custom monsters usamos Animate genérico
            if (_aiState == 1) this.Sprite.currentFrame = 0; // Agachado
            else this.Sprite.Animate(time, 0, 4, 100f);
        }

        // ============================================================================================
        // 🦇 IA VOLADORA (MURCIÉLAGOS)
        // ============================================================================================
        private void BehaviorBat(GameTime time, int speed)
        {
            this.IsGlider = true;
            if (_runAwayTimer > 0)
            {
                _runAwayTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                Vector2 away = this.Position - this.Player.Position;
                away.Normalize();
                this.xVelocity = away.X * speed * 1.5f;
                this.yVelocity = away.Y * speed * 1.5f;
            }
            else if (withinPlayerThreshold(16)) 
            {
                Vector2 trajectory = this.Player.Position - this.Position;
                trajectory.Normalize();
                if (this.xVelocity < trajectory.X * speed) this.xVelocity += 0.07f;
                else if (this.xVelocity > trajectory.X * speed) this.xVelocity -= 0.07f;
                if (this.yVelocity < trajectory.Y * speed) this.yVelocity += 0.07f;
                else if (this.yVelocity > trajectory.Y * speed) this.yVelocity -= 0.07f;
            }
            this.Sprite.Animate(time, 0, 4, 80f); 
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        // ============================================================================================
        // 👻 IA FANTASMA
        // ============================================================================================
        private void BehaviorGhost(GameTime time, int speed)
        {
            this.IsGlider = true;
            if (withinPlayerThreshold(20))
            {
                Vector2 trajectory = this.Player.Position - this.Position;
                trajectory.Normalize();
                this.xVelocity = trajectory.X * (speed * 0.7f);
                this.yVelocity = trajectory.Y * (speed * 0.7f);
            }
            if (Vector2.Distance(this.Position, this.Player.Position) < 64f)
            {
                this.xVelocity *= 1.5f;
                this.yVelocity *= 1.5f;
            }
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        // ============================================================================================
        // 🐍 IA SERPIENTE
        // ============================================================================================
        private void BehaviorSerpent(GameTime time, int speed)
        {
            this.IsGlider = true;
            if (withinPlayerThreshold(20))
            {
                Vector2 target = this.Player.Position;
                Vector2 diff = target - this.Position;
                float angle = (float)Math.Atan2(diff.Y, diff.X);
                float sineWave = (float)Math.Sin(time.TotalGameTime.TotalMilliseconds / 100.0) * 2f;
                this.xVelocity = (float)Math.Cos(angle) * speed + sineWave;
                this.yVelocity = (float)Math.Sin(angle) * speed + sineWave;
                this.Rotation = angle + (float)Math.PI / 2f; 
            }
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        // ============================================================================================
        // 🏹 IA TIRADOR
        // ============================================================================================
        private void BehaviorShooter(GameTime time, int speed)
        {
            this.IsWalker = true;
            if (_fireCooldown > 0) _fireCooldown -= (float)time.ElapsedGameTime.TotalMilliseconds;
            float dist = Vector2.Distance(this.Position, this.Player.Position);

            if (dist > 350f) 
            {
                this.IsWalkingTowardPlayer = true;
                base.moveTowardPlayer(speed);
            }
            else if (dist < 100f)
            {
                this.IsWalkingTowardPlayer = false;
                Vector2 away = this.Position - this.Player.Position;
                away.Normalize();
                this.xVelocity = away.X * speed;
                this.yVelocity = away.Y * speed;
                this.MovePosition(time, Game1.viewport, Game1.currentLocation);
            }
            else 
            {
                this.IsWalkingTowardPlayer = false;
                this.Halt(); 
                this.faceGeneralDirection(this.Player.Position);

                if (_fireCooldown <= 0)
                {
                    Game1.currentLocation.projectiles.Add(new BasicProjectile(
                        damageToFarmer: this.DamageToFarmer,
                        projectileID: BasicProjectile.shadowBall, 
                        startingPosition: 0, 
                        x: (int)this.Position.X, 
                        y: (int)this.Position.Y, 
                        speed: 10f, 
                        xVelocity: 0, 
                        yVelocity: 0, 
                        motion: Vector2.Zero, 
                        collisionSound: "flameSpell_hit", 
                        firingSound: "flameSpell", 
                        explode: false, 
                        damagesMonsters: false, 
                        location: Game1.currentLocation, 
                        shooter: this
                    )
                    {
                        velocity = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), 10f, this.Player)
                    });
                    _fireCooldown = 3000f; 
                }
            }
        }

        // ============================================================================================
        // 🐛 IA DUGGY
        // ============================================================================================
        private void BehaviorDuggy(GameTime time, int speed)
        {
            bool playerNear = withinPlayerThreshold(3);
            if (_aiState == 0) // Bajo tierra
            {
                this.IsInvisible = true;
                this.HideShadow = true;
                this.DamageToFarmer = 0; 
                this.isInvincible = true; 

                if (playerNear && _stateTimer <= 0)
                {
                    Game1.playSound("dig");
                    _aiState = 1;
                    _stateTimer = 2000f; 
                }
            }
            else // Arriba
            {
                this.IsInvisible = false;
                this.HideShadow = false;
                this.isInvincible = false;
                var entry = MonsterRegistry.Get(MonsterSourceId.Value);
                this.DamageToFarmer = entry?.Data.DamageToFarmer ?? 10;
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Sprite.Animate(time, 0, 4, 150f);
                if (_stateTimer <= 0)
                {
                    _aiState = 0;
                    _stateTimer = 1000f; 
                }
            }
        }

        // ============================================================================================
        // 🦀 IA ROCK CRAB
        // ============================================================================================
        private void BehaviorRockCrab(GameTime time, int speed)
        {
            if (_aiState == 0) // Roca
            {
                this.DamageToFarmer = 0;
                this.Sprite.currentFrame = 0;
                if (withinPlayerThreshold(3)) {
                    _aiState = 1;
                    _stateTimer = 500f;
                    Game1.playSound("stoneCrack");
                }
            }
            else if (_aiState == 1) // Despertando
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Sprite.currentFrame = 1;
                if (_stateTimer <= 0) {
                    _aiState = 2;
                    var entry = MonsterRegistry.Get(MonsterSourceId.Value);
                    this.DamageToFarmer = entry?.Data.DamageToFarmer ?? 10;
                }
            }
            else // Persiguiendo
            {
                if (!withinPlayerThreshold(10)) {
                    _aiState = 0; 
                    this.Sprite.currentFrame = 0;
                    return;
                }
                this.IsWalkingTowardPlayer = true;
                base.moveTowardPlayer(speed);
            }
        }

        private void BehaviorStalker(GameTime time, int speed)
        {
            if (!withinPlayerThreshold(16)) return;
            this.IsWalkingTowardPlayer = true;
            base.moveTowardPlayer(speed);
        }

        private void BehaviorTank(GameTime time, int speed)
        {
            if (!withinPlayerThreshold(10)) return;
            this.IsWalkingTowardPlayer = true;
            base.moveTowardPlayer(Math.Max(1, speed - 1));
        }

        // ============================================================================================
        // ⚔️ UTILIDADES
        // ============================================================================================

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";

            if (behavior == "Bat" || behavior == "Ghost")
            {
                _runAwayTimer = 1000f;
                _wasHitRecently = true;
            }

            if (behavior == "RockCrab" && _aiState == 0)
            {
                Game1.playSound("hitRock");
                return 0; 
            }

            return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
        }

        public override List<Item> getExtraDropItems()
        {
            var drops = new List<Item>();
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return drops;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry != null && entry.Data.Drops != null)
            {
                foreach (var dropData in entry.Data.Drops)
                {
                    if (Game1.random.NextDouble() <= dropData.Chance)
                    {
                        Item item = ItemRegistry.Create(dropData.ItemId, 1);
                        drops.Add(item);
                    }
                }
            }
            return drops;
        }

        private bool IsPlayerLookingAtMe()
        {
            Vector2 toMonster = this.Position - this.Player.Position;
            int faceDir = this.Player.FacingDirection; 
            if (Math.Abs(toMonster.X) > Math.Abs(toMonster.Y))
                return (toMonster.X > 0 && faceDir == 1) || (toMonster.X < 0 && faceDir == 3);
            else
                return (toMonster.Y > 0 && faceDir == 2) || (toMonster.Y < 0 && faceDir == 0);
        }
    }
}