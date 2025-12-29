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
    // Vital para guardar partida con SpaceCore
    [XmlType("Mods_JavCombita_Monstrosity_CustomMonster")] 
    public class CustomMonster : Monster
    {
        public readonly NetString MonsterSourceId = new();
        
        // --- VARIABLES DE ESTADO INTERNO ---
        private bool _hasLoadedData = false;
        
        // Control genérico de estados (usado por todas las IAs)
        private float _stateTimer = 0f;
        private int _aiState = 0; 
        
        // Control de invulnerabilidad (Override para Duggy/RockCrab/Mummy)
        private bool _isInvincibleOverride = false;
        
        // IA Tirador
        private float _fireCooldown = 0f;
        
        // IA Volador
        private float _runAwayTimer = 0f;

        // IA Momia
        private bool _isMummyDown = false;
        private float _reviveTimer = 0f;

        // IA Exploder (HotHead)
        private bool _isExploding = false;

        // --- CONSTRUCTORES ---

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

        // --- SISTEMA DE CARGA DE DATOS ---

        public void ReloadData()
        {
            _hasLoadedData = true;
            if (string.IsNullOrEmpty(MonsterSourceId.Value)) return;

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            if (entry == null) { EnsureFallbackTexture(); return; }

            // Aplicar Stats
            this.Name = entry.Data.DisplayName;
            this.MaxHealth = entry.Data.MaxHealth;
            this.Health = this.MaxHealth;
            this.DamageToFarmer = entry.Data.DamageToFarmer;
            this.ExperienceGained = entry.Data.Exp;
            
            string behavior = entry.Data.BehaviorType ?? "Default";
            
            // Configurar física (isGlider es un NetBool en 1.6)
            if (behavior == "Bat" || behavior == "Ghost" || behavior == "Serpent" || behavior == "Slime" || behavior == "Fly") 
            {
                this.isGlider.Value = true;
            }
            else
            {
                this.isGlider.Value = false;
            }

            // Optimización de pathfinding para caminantes
            if (behavior == "Stalker" || behavior == "Tank" || behavior == "Shooter" || behavior == "Mummy" || behavior == "Exploder")
            {
                this.focusedOnFarmers = true; // Ayuda a que no se atoren tanto
            }

            // Configurar Sprite
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

        // --- OVERRIDES DEL MOTOR DE JUEGO ---

        public override bool isInvincible()
        {
            return _isInvincibleOverride || base.isInvincible();
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";

            // IA Voladora: Huir al ser golpeado
            if (behavior == "Bat" || behavior == "Ghost")
            {
                _runAwayTimer = 1000f;
            }

            // IA Cangrejo: Invulnerable si está escondido
            if (behavior == "RockCrab" && _aiState == 0)
            {
                Game1.playSound("hitRock");
                return 0; 
            }

            // IA Momia: Mecánica de resurrección
            if (behavior == "Mummy")
            {
                if (_isMummyDown)
                {
                    if (isBomb) // Solo muere con bombas
                    {
                        Game1.playSound("rockGolemHit"); 
                        return base.takeDamage(9999, 0, 0, true, addedPrecision, who);
                    }
                    return 0; // Inmune a espadas en el suelo
                }
                else
                {
                    int actualDamage = Math.Max(1, damage - (int)this.Defense);
                    if (this.Health - actualDamage <= 0)
                    {
                        this.Health = 1; // Evitar muerte
                        _isMummyDown = true;
                        _reviveTimer = 10000f; // 10s para revivir
                        Game1.playSound("rockGolemHit");
                        this.Sprite.currentFrame = 4; // Frame de "caído" (ajustar según sprite)
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
                    {
                        Item item = ItemRegistry.Create(dropData.ItemId, 1);
                        drops.Add(item);
                    }
                }
            }
            return drops;
        }

        // ============================================================================================
        // 🧠 CEREBRO CENTRAL (AI DISPATCHER)
        // ============================================================================================
        
        public override void behaviorAtGameTick(GameTime time)
        {
            // 1. Lazy Loading Seguro
            if (!_hasLoadedData || this.Sprite?.spriteTexture == null)
            {
                if (!string.IsNullOrEmpty(MonsterSourceId.Value)) ReloadData();
                if (this.Sprite?.spriteTexture == null) return;
            }

            var entry = MonsterRegistry.Get(MonsterSourceId.Value);
            string behavior = entry?.Data.BehaviorType ?? "Default";
            int speed = entry?.Data.Speed ?? 2;

            // 2. Selección de Comportamiento
            switch (behavior)
            {
                case "Slime":       BehaviorSlime(time, speed); break;
                case "Bat":         BehaviorBat(time, speed); break;
                case "Ghost":       BehaviorGhost(time, speed); break;
                case "Shooter":     BehaviorShooter(time, speed); break;
                case "RockCrab":    BehaviorRockCrab(time, speed); break;
                case "Duggy":       BehaviorDuggy(time, speed); break;
                case "Serpent":     BehaviorSerpent(time, speed); break;
                case "Mummy":       BehaviorMummy(time, speed); break;
                case "Exploder":    BehaviorExploder(time, speed); break;
                case "Tank":        BehaviorTank(time, speed); break;
                case "Stalker":     
                default:            BehaviorStalker(time, speed); break;
            }
        }

        // ============================================================================================
        // 🤖 COMPORTAMIENTOS (BEHAVIORS)
        // ============================================================================================

        private void BehaviorSlime(GameTime time, int speed)
        {
            if (_aiState == 0) // IDLE
            {
                this.isGlider.Value = false; 
                if (withinPlayerThreshold(12))
                {
                    if (_stateTimer > 0) _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                    else
                    {
                        if (Game1.random.NextDouble() < 0.02) 
                        {
                            _aiState = 1; // Cargar
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
                if (_stateTimer <= 0)
                {
                    _aiState = 2; // SALTO
                    _stateTimer = 500f;
                    Vector2 velocity = Utility.getVelocityTowardPlayer(new Point((int)this.Position.X, (int)this.Position.Y), speed * 4f, this.Player);
                    this.xVelocity = velocity.X;
                    this.yVelocity = velocity.Y;
                    this.isGlider.Value = true; 
                    Game1.playSound("slimeJump");
                }
            }
            else if (_aiState == 2) // EN AIRE
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Position += new Vector2(this.xVelocity, this.yVelocity); // Fix CS1612

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
            if (_aiState == 1) this.Sprite.currentFrame = 0;
            else this.Sprite.Animate(time, 0, 4, 100f);
        }

        private void BehaviorBat(GameTime time, int speed)
        {
            this.isGlider.Value = true; 
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

        private void BehaviorGhost(GameTime time, int speed)
        {
            this.isGlider.Value = true;
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

        private void BehaviorSerpent(GameTime time, int speed)
        {
            this.isGlider.Value = true;
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
            this.isGlider.Value = false; 
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
                    Vector2 shotVelocity = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), 10f, this.Player);

                    // Fix CS1739: Argumentos posicionales
                    Game1.currentLocation.projectiles.Add(new BasicProjectile(
                        this.DamageToFarmer,           
                        BasicProjectile.shadowBall,    
                        0,                             
                        0,                             
                        0f,                            
                        shotVelocity.X,                
                        shotVelocity.Y,                
                        0,                             
                        "flameSpell_hit",              
                        "flameSpell",                  
                        false,                         
                        false,                         
                        Game1.currentLocation,         
                        this,                          
                        false,                         
                        null                           
                    ));
                    _fireCooldown = 3000f; 
                }
            }
        }

        private void BehaviorDuggy(GameTime time, int speed)
        {
            bool playerNear = withinPlayerThreshold(3);
            if (_aiState == 0) // Bajo tierra
            {
                this.IsInvisible = true;
                this.HideShadow = true;
                this.DamageToFarmer = 0; 
                _isInvincibleOverride = true; 

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
                _isInvincibleOverride = false; 
                var entry = MonsterRegistry.Get(MonsterSourceId.Value);
                this.DamageToFarmer = entry?.Data.DamageToFarmer ?? 10;
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.Sprite.Animate(time, 0, 4, 150f);
                if (_stateTimer <= 0) { _aiState = 0; _stateTimer = 1000f; }
            }
        }

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

        private void BehaviorMummy(GameTime time, int speed)
        {
            if (_isMummyDown)
            {
                this.IsWalkingTowardPlayer = false;
                this.Halt();
                this.isGlider.Value = false;
                _isInvincibleOverride = true;
                this.Sprite.currentFrame = 4; // Frame de "derrotado"
                
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
                _isInvincibleOverride = false;
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
                    // BOOM
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
                if (this.Health < this.MaxHealth * 0.2f)
                {
                    _isExploding = true;
                    _stateTimer = 2000f;
                    Game1.playSound("fuse");
                }
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