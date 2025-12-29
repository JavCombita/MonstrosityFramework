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
        
        // Control genérico de estados
        private float _stateTimer = 0f;
        private int _aiState = 0; 
        
        // Control de invulnerabilidad personalizado (para Duggies)
        private bool _isInvincibleOverride = false;
        
        // IA Tirador
        private float _fireCooldown = 0f;
        
        // IA Fantasma/Volador
        private float _runAwayTimer = 0f;

        // IA Serpiente
        // (No usamos variables extra aquí, usamos velocity directa)

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

        // SOBREESCRIBIMOS EL MÉTODO DE INVULNERABILIDAD
        public override bool isInvincible()
        {
            return _isInvincibleOverride || base.isInvincible();
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
            // FIX: Usamos isGlider.Value (NetBool) en lugar de IsGlider
            if (behavior == "Bat" || behavior == "Ghost" || behavior == "Serpent" || behavior == "Slime") 
            {
                this.isGlider.Value = true;
            }
            else
            {
                this.isGlider.Value = false;
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
                case "Slime":       BehaviorSlime(time, speed); break;
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
            if (_aiState == 0) // IDLE
            {
                this.isGlider.Value = false; // Suelo
                
                if (withinPlayerThreshold(12))
                {
                    if (_stateTimer > 0)
                    {
                        _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                    }
                    else
                    {
                        if (Game1.random.NextDouble() < 0.02) 
                        {
                            _aiState = 1; // Cargar
                            _stateTimer = 600f;
                            Game1.playSound("slimeHit");
                            this.Halt();
                        }
                        else
                        {
                            base.moveTowardPlayer(1);
                        }
                    }
                }
            }
            else if (_aiState == 1) // CARGANDO
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
                this.shake(Game1.random.Next(1, 3)); 

                if (_stateTimer <= 0)
                {
                    _aiState = 2; // SALTAR
                    _stateTimer = 500f;
                    
                    Vector2 velocity = Utility.getVelocityTowardPlayer(new Point((int)this.Position.X, (int)this.Position.Y), speed * 4f, this.Player);
                    this.xVelocity = velocity.X;
                    this.yVelocity = velocity.Y;
                    
                    this.isGlider.Value = true; // Aire
                    Game1.playSound("slimeJump");
                }
            }
            else if (_aiState == 2) // EN AIRE
            {
                _stateTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;

                // FIX: Modificar posición creando nuevo Vector2 (CS1612)
                this.Position += new Vector2(this.xVelocity, this.yVelocity);

                if (_stateTimer < 250) 
                {
                    this.xVelocity *= 0.90f;
                    this.yVelocity *= 0.90f;
                }

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

        // ============================================================================================
        // 🦇 IA VOLADORA
        // ============================================================================================
        private void BehaviorBat(GameTime time, int speed)
        {
            this.isGlider.Value = true; // FIX: CS1061

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

        // ============================================================================================
        // 🐍 IA SERPIENTE
        // ============================================================================================
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
                
                // FIX: Usamos el campo 'rotation' en minúscula
                this.rotation = angle + (float)Math.PI / 2f; 
            }
            this.MovePosition(time, Game1.viewport, Game1.currentLocation);
        }

        // ============================================================================================
        // 🏹 IA TIRADOR
        // ============================================================================================
        private void BehaviorShooter(GameTime time, int speed)
        {
            this.isGlider.Value = false; // FIX: Caminante
            
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
                    // FIX: Calculamos velocidad ANTES y usamos constructor correcto de BasicProjectile
                    Vector2 shotVelocity = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), 10f, this.Player);

                    Game1.currentLocation.projectiles.Add(new BasicProjectile(
                        damageToFarmer: this.DamageToFarmer,
                        parentSheetIndex: BasicProjectile.shadowBall, // FIX: Nombre de parámetro corregido
                        x: (int)this.Position.X, 
                        y: (int)this.Position.Y, 
                        speed: 10f, 
                        xVelocity: shotVelocity.X, // FIX: Pasamos la velocidad calculada
                        yVelocity: shotVelocity.Y, 
                        rotate: false,
                        collisionSound: "flameSpell_hit", 
                        firingSound: "flameSpell", 
                        explode: false, 
                        damagesMonsters: false, 
                        location: Game1.currentLocation, 
                        shooter: this
                    ));
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
                _isInvincibleOverride = true; // FIX: Usamos el override

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
                _isInvincibleOverride = false; // FIX
                
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
                // variable _wasHitRecently eliminada porque no se usaba para nada más
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