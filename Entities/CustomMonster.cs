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
        
        private string _cachedBehavior = "default"; 
        private bool _hasLoadedData = false;
        
        // --- VARIABLES DE ESTADO (IA) ---
        private float _stateTimer = 0f;
        private int _aiState = 0; 
        private bool _isInvincibleOverride = false;
        private float _fireCooldown = 0f;
        private float _runAwayTimer = 0f;
        private bool _isMummyDown = false;
        private float _reviveTimer = 0f;
        private bool _isExploding = false;
        
        // --- DEBUG ---
        private bool _textureChecked = false;

        public CustomMonster() : base() { EnsureFallbackTexture(); }

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

            _cachedBehavior = (entry.Data.BehaviorType ?? "Default").ToLowerInvariant();

            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            this.resilience.Value = entry.Data.Defense;
            
            // OPTIMIZACIÓN: Solo asignar si cambia
            if (this.Speed != entry.Data.Speed) this.Speed = entry.Data.Speed;
            
            // IMPORTANTE: Evita que rompan rocas al caminar
            this.willDestroyObjectsUnderfoot = false;

            // Configurar flags de movimiento
            bool shouldGlide = _cachedBehavior == "bat" || _cachedBehavior == "ghost" || _cachedBehavior == "serpent" || _cachedBehavior == "slime" || _cachedBehavior == "fly";
            if (this.isGlider.Value != shouldGlide) this.isGlider.Value = shouldGlide;

            bool shouldFocus = _cachedBehavior == "stalker" || _cachedBehavior == "tank" || _cachedBehavior == "shooter" || _cachedBehavior == "mummy" || _cachedBehavior == "exploder" || _cachedBehavior == "rockcrab";
            if (this.focusedOnFarmers != shouldFocus) this.focusedOnFarmers = shouldFocus;

            // Configurar Sprite
            this.Sprite = new AnimatedSprite(null, 0, entry.Data.SpriteWidth, entry.Data.SpriteHeight);
            
            try
            {
                Texture2D customTex = entry.GetTexture();
                if (customTex != null) 
                {
                    this.Sprite.spriteTexture = customTex;
                }
                else 
                {
                    EnsureFallbackTexture();
                }
            }
            catch (Exception) { EnsureFallbackTexture(); }
            
            this.HideShadow = false;
        }

        private void EnsureFallbackTexture()
        {
            if (this.Sprite == null) this.Sprite = new AnimatedSprite("Characters/Monsters/Shadow Brute", 0, 16, 24);
            
            if (this.Sprite.spriteTexture == null || this.Sprite.spriteTexture.IsDisposed)
            {
                try { this.Sprite.spriteTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); } catch { }
            }
        }

        public override bool isInvincible()
        {
            return _isInvincibleOverride || base.isInvincible();
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            // Lógica de Huida
            if (_cachedBehavior == "bat" || _cachedBehavior == "ghost") 
            {
                _runAwayTimer = 2000f; 
            }

            // Lógica RockCrab (Despertar)
            if (_cachedBehavior == "rockcrab" && _aiState == 0)
            {
                Game1.playSound("hitRock");
                _aiState = 1;       
                _stateTimer = 500f; 
                this.faceGeneralDirection(who.Position);
                return 0; 
            }

            // Lógica Momia (Caer)
            if (_cachedBehavior == "mummy")
            {
                if (_isMummyDown)
                {
                    if (isBomb) { Game1.playSound("rockGolemHit"); return base.takeDamage(9999, 0, 0, true, addedPrecision, who); }
                    return 0; 
                }
                else
                {
                    int actualDamage = Math.Max(1, damage - this.resilience.Value);
                    if (this.Health - actualDamage <= 0)
                    {
                        this.Health = 1; 
                        _isMummyDown = true; 
                        _reviveTimer = 10000f; 
                        Game1.playSound("rockGolemHit");
                        this.Sprite.currentFrame = 4; 
                        this.Sprite.StopAnimation(); 
                        return 0; 
                    }
                }
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
                        drops.Add(ItemRegistry.Create(dropData.ItemId, 1));
                }
            }
            return drops;
        }

        // --- CEREBRO CENTRAL (GAME LOOP) ---
        public override void behaviorAtGameTick(GameTime time)
        {
            // 1. CHEQUEO VISUAL (Solo la primera vez)
            if (!_textureChecked)
            {
                _textureChecked = true;
                if (this.Sprite?.spriteTexture == null)
                {
                    ModEntry.StaticMonitor.Log($"[Visual] {MonsterSourceId.Value} no tiene textura. Usando fallback.", LogLevel.Warn);
                    EnsureFallbackTexture();
                }
            }

            // 2. RECARGA SI ES NECESARIO
            if (!_hasLoadedData || this.Sprite?.spriteTexture == null)
            {
                if (!string.IsNullOrEmpty(MonsterSourceId.Value)) ReloadData();
                if (this.Sprite?.spriteTexture == null) return;
            }

            // 3. SINCRONIZAR VELOCIDAD
            int speed = MonsterRegistry.Get(MonsterSourceId.Value)?.Data.Speed ?? 2;
            if (this.Speed != speed) this.Speed = speed;

            // 4. SEGURIDAD ANTI-CRASH (NaN Check)
            // Si las coordenadas se corrompen, reseteamos al monstruo en lugar de congelar el juego.
            if (float.IsNaN(this.Position.X) || float.IsNaN(this.Position.Y)) {
                this.Position = Game1.player.Position + new Vector2(64, 64);
                this.xVelocity = 0; 
                this.yVelocity = 0;
            }
            if (float.IsNaN(this.xVelocity) || float.IsNaN(this.yVelocity)) {
                this.xVelocity = 0; 
                this.yVelocity = 0;
            }

            // 5. EJECUTAR COMPORTAMIENTO
            switch (_cachedBehavior)
            {
                case "slime":       BehaviorSlime(time, speed); break;
                case "bat":         BehaviorBat(time, speed); break;
                case "ghost":       BehaviorGhost(time, speed); break;
                case "shooter":     BehaviorShooter(time, speed); break;
                case "rockcrab":    BehaviorRockCrab(time, speed); break; 
                case "duggy":       BehaviorDuggy(time, speed); break;
                case "serpent":     BehaviorSerpent(time, speed); break;
                case "mummy":       BehaviorMummy(time, speed); break;
                case "exploder":    BehaviorExploder(time, speed); break;
                case "tank":        BehaviorTank(time, speed); break;
                case "stalker":     
                default:            BehaviorStalker(time, speed); break;
            }
        }

        // --- HELPERS SEGUROS ---
        private void SafeNormalize(ref Vector2 vector)
        {
            // Evita división por cero
            if (vector.LengthSquared() > 0.0001f) vector.Normalize();
            else vector = Vector2.Zero;
        }

        // --- BEHAVIORS ---

        private void BehaviorBat(GameTime time, int speed)
        {
            if (!this.isGlider.Value) this.isGlider.Value = true;
            
            if (_runAwayTimer > 0)
            {
                _runAwayTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                Vector2 away = this.Position - this.Player.Position;
                SafeNormalize(ref away); // Seguro
                this.xVelocity = away.X * speed * 1.5f;
                this.yVelocity = away.Y * speed * 1.5f;
            }
            else if (withinPlayerThreshold(16)) 
            {
                Vector2 trajectory = this.Player.Position - this.Position;
                SafeNormalize(ref trajectory); // Seguro
                
                // Física suavizada
                float acceleration = 0.07f;
                float maxVel = speed * 2f;

                if (this.xVelocity < trajectory.X * speed) this.xVelocity += acceleration;
                else if (this.xVelocity > trajectory.X * speed) this.xVelocity -= acceleration;
                
                if (this.yVelocity < trajectory.Y * speed) this.yVelocity += acceleration;
                else if (this.yVelocity > trajectory.Y * speed) this.yVelocity -= acceleration;

                this.xVelocity = MathHelper.Clamp(this.xVelocity, -maxVel, maxVel);
                this.yVelocity = MathHelper.Clamp(this.yVelocity, -maxVel, maxVel);
            }

            this.Sprite.Animate(time, 0, 4, 80f); 
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        private void BehaviorGhost(GameTime time, int speed)
        {
            if (!this.isGlider.Value) this.isGlider.Value = true;
            if (withinPlayerThreshold(20))
            {
                Vector2 trajectory = this.Player.Position - this.Position;
                SafeNormalize(ref trajectory);
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

        private void BehaviorSerpent(GameTime time, int speed)
        {
            if (!this.isGlider.Value) this.isGlider.Value = true;
            if (withinPlayerThreshold(20))
            {
                Vector2 target = this.Player.Position;
                Vector2 diff = target - this.Position;
                float angle = (float)Math.Atan2(diff.Y, diff.X);
                float sineWave = (float)Math.Sin(time.TotalGameTime.TotalMilliseconds / 100.0) * 2f;
                
                this.xVelocity = (float)Math.Cos(angle) * speed + sineWave;
                this.yVelocity = (float)Math.Sin(angle) * speed + sineWave;
                this.rotation = angle + (float)Math.PI / 2f; 
            }
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        private void BehaviorShooter(GameTime time, int speed)
        {
            if (this.isGlider.Value) this.isGlider.Value = false; 

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
                SafeNormalize(ref away);
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
                    Vector2 shotVelocity = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), 10f, this.Player);

                    Game1.currentLocation.projectiles.Add(new BasicProjectile(
                        this.DamageToFarmer,           
                        BasicProjectile.shadowBall,    
                        0, 0, 0f,                            
                        shotVelocity.X, shotVelocity.Y,                
                        this.Position,                 
                        "flameSpell_hit", "flameSpell",                  
                        null, false, false,                         
                        Game1.currentLocation, this, null                           
                    ));
                    _fireCooldown = 3000f; 
                }
            }
        }

        private void BehaviorRockCrab(GameTime time, int speed)
        {
            int baseFrame = this.FacingDirection * 4;

            if (_aiState == 0) // ROCA (Oculto)
            {
                if (!_isInvincibleOverride) _isInvincibleOverride = true; 
                this.DamageToFarmer = 0;
                
                this.Sprite.currentFrame = baseFrame; 
                this.Sprite.StopAnimation(); 
                this.HideShadow = true; 
                this.IsWalkingTowardPlayer = false;

                float distance = Vector2.Distance(this.GetBoundingBox().Center.ToVector2(), this.Player.GetBoundingBox().Center.ToVector2());
                if (distance < 192f) 
                {
                    _aiState = 1;
                    _stateTimer = 500f;
                    Game1.playSound("stoneCrack");
                    this.faceGeneralDirection(this.Player.Position); 
                    this.shake(Game1.random.Next(2, 4));
                }
            }
            else if (_aiState == 1) // LEVANTÁNDOSE
            {
                if (!_isInvincibleOverride) _isInvincibleOverride = true; 
                this.DamageToFarmer = 0;
                this.HideShadow = false; 
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                
                this.Sprite.currentFrame = baseFrame + 1;
                this.Sprite.StopAnimation();
                
                if (_stateTimer <= 0) { 
                    _aiState = 2; 
                    _isInvincibleOverride = false; 
                    var entry = MonsterRegistry.Get(MonsterSourceId.Value);
                    this.DamageToFarmer = entry?.Data.DamageToFarmer ?? 10;
                }
            }
            else // PERSECUCIÓN
            {
                if (_isInvincibleOverride) _isInvincibleOverride = false; 
                this.HideShadow = false;
                
                float distance = Vector2.Distance(this.GetBoundingBox().Center.ToVector2(), this.Player.GetBoundingBox().Center.ToVector2());
                if (distance > 384f) { 
                    _aiState = 0; 
                    return; 
                }
                
                this.IsWalkingTowardPlayer = true;
                base.moveTowardPlayer(speed);
            }
        }

        private void BehaviorSlime(GameTime time, int speed)
        {
            if (_aiState == 0) // IDLE
            {
                if (this.isGlider.Value) this.isGlider.Value = false; 
                if (withinPlayerThreshold(12))
                {
                    if (_stateTimer > 0) _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                    else
                    {
                        if (Game1.random.NextDouble() < 0.02) 
                        {
                            _aiState = 1; 
                            _stateTimer = 600f;
                            Game1.playSound("slimeHit");
                            this.Halt();
                        }
                        else base.moveTowardPlayer(1);
                    }
                }
            }
            else if (_aiState == 1) // CARGANDO
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.shake(Game1.random.Next(1, 3)); 
                this.Sprite.currentFrame = 0; 
                this.Sprite.StopAnimation();

                if (_stateTimer <= 0)
                {
                    _aiState = 2; 
                    _stateTimer = 500f;
                    Vector2 velocity = Utility.getVelocityTowardPlayer(new Point((int)this.Position.X, (int)this.Position.Y), speed * 4f, this.Player);
                    this.xVelocity = velocity.X;
                    this.yVelocity = velocity.Y;
                    if (!this.isGlider.Value) this.isGlider.Value = true; 
                    Game1.playSound("slimeJump");
                }
            }
            else if (_aiState == 2) // AIRE
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Position += new Vector2(this.xVelocity, this.yVelocity);

                if (_stateTimer < 250) { this.xVelocity *= 0.90f; this.yVelocity *= 0.90f; }

                if (this.GetBoundingBox().Intersects(this.Player.GetBoundingBox()))
                {
                    this.xVelocity = -this.xVelocity * 0.5f;
                    this.yVelocity = -this.yVelocity * 0.5f;
                    _aiState = 0;
                    _stateTimer = 1000f;
                }

                if (_stateTimer <= 0 || (Math.Abs(xVelocity) < 0.5f && Math.Abs(yVelocity) < 0.5f))
                {
                    _aiState = 0;
                    _stateTimer = Game1.random.Next(1000, 2000);
                }
            }
        }

        private void BehaviorDuggy(GameTime time, int speed)
        {
            bool playerNear = withinPlayerThreshold(3);
            if (_aiState == 0) 
            {
                this.IsInvisible = true; this.HideShadow = true; this.DamageToFarmer = 0; 
                if (!_isInvincibleOverride) _isInvincibleOverride = true; 
                if (playerNear && _stateTimer <= 0) { Game1.playSound("dig"); _aiState = 1; _stateTimer = 2000f; }
            }
            else 
            {
                this.IsInvisible = false; this.HideShadow = false; 
                if (_isInvincibleOverride) _isInvincibleOverride = false; 
                var entry = MonsterRegistry.Get(MonsterSourceId.Value);
                this.DamageToFarmer = entry?.Data.DamageToFarmer ?? 10;
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Sprite.Animate(time, 0, 4, 150f);
                if (_stateTimer <= 0) { _aiState = 0; _stateTimer = 1000f; }
            }
        }

        private void BehaviorMummy(GameTime time, int speed)
        {
            if (_isMummyDown)
            {
                this.IsWalkingTowardPlayer = false;
                this.Halt();
                if (this.isGlider.Value) this.isGlider.Value = false;
                if (!_isInvincibleOverride) _isInvincibleOverride = true;
                this.Sprite.currentFrame = 4;
                this.Sprite.StopAnimation();
                _reviveTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (_reviveTimer <= 0)
                {
                    _isMummyDown = false;
                    this.Health = this.MaxHealth;
                    Game1.playSound("shadowDie");
                    this.Sprite.currentFrame = 0;
                }
            }
            else
            {
                if (_isInvincibleOverride) _isInvincibleOverride = false;
                BehaviorStalker(time, Math.Max(1, speed - 1));
            }
        }

        private void BehaviorExploder(GameTime time, int speed)
        {
            if (_isExploding)
            {
                this.Halt();
                this.shake(Game1.random.Next(2, 5));
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                if (_stateTimer <= 0)
                {
                    this.Health = -999; 
                    Game1.createRadialDebris(Game1.currentLocation, 12, (int)this.Position.X / 64, (int)this.Position.Y / 64, 6, false);
                    Game1.playSound("explosion");
                    if (withinPlayerThreshold(2)) this.Player.takeDamage(20, false, null);
                    this.takeDamage(9999, 0, 0, true, 0, this.Player);
                }
            }
            else
            {
                BehaviorStalker(time, speed);
                if (this.Health < this.MaxHealth * 0.2f) { _isExploding = true; _stateTimer = 2000f; Game1.playSound("fuse"); }
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
    }
}