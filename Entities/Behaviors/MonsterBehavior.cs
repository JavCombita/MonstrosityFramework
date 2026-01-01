using Microsoft.Xna.Framework;
using StardewValley;
using System;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Framework.Data;

namespace MonstrosityFramework.Entities.Behaviors
{
    public abstract class MonsterBehavior
    {
        // --- CICLO DE VIDA ---
        
        /// <summary>
        /// Se llama al crear el monstruo o recargar el save. 
        /// Úsalo para configurar variables iniciales (LocalData) y leer configs del JSON.
        /// </summary>
        public virtual void Initialize(CustomMonster monster) { }

        /// <summary>
        /// Lógica principal por tick (IA).
        /// </summary>
        public abstract void Update(CustomMonster monster, GameTime time);

        /// <summary>
        /// Se llama en updateAnimation(). Útil para efectos visuales (Luces, partículas).
        /// </summary>
        public virtual void OnUpdateAnimation(CustomMonster monster, GameTime time) { }

        /// <summary>
        /// Manejo de daño recibido. Retorna el daño modificado.
        /// Retorna -1 para cancelar el daño (invulnerabilidad).
        /// </summary>
        public virtual int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who) => damage;

        /// <summary>
        /// Se llama al morir (animación de muerte). Limpiar luces o spawnear cosas.
        /// </summary>
        public virtual void OnDeath(CustomMonster monster) { }


        // --- HELPERS DE UTILIDAD Y JSON ---

        protected MonsterData GetData(CustomMonster monster)
        {
            if (string.IsNullOrEmpty(monster.MonsterSourceId.Value)) return null;
            return MonsterRegistry.Get(monster.MonsterSourceId.Value)?.Data;
        }

        protected bool IsPlayerWithinRange(CustomMonster monster, float tiles) => monster.withinPlayerThreshold((int)tiles);

        /// <summary>
        /// Obtiene el rango de detección ("DetectionRange" en JSON) o usa el default.
        /// </summary>
        protected float GetVisionRange(CustomMonster monster, float defaultRange = 8f)
        {
            return GetCustomFloat(monster, "DetectionRange", defaultRange);
        }

        // -- Lectores de CustomFields (Con valores por defecto) --

        protected int GetCustomInt(CustomMonster monster, string key, int defaultValue)
        {
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue(key, out string val))
            {
                if (int.TryParse(val, out int result)) return result;
            }
            return defaultValue;
        }

        protected float GetCustomFloat(CustomMonster monster, string key, float defaultValue)
        {
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue(key, out string val))
            {
                if (float.TryParse(val, out float result)) return result;
            }
            return defaultValue;
        }

        protected string GetCustomString(CustomMonster monster, string key, string defaultValue)
        {
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue(key, out string val))
            {
                return val;
            }
            return defaultValue;
        }

        protected bool GetCustomBool(CustomMonster monster, string key, bool defaultValue)
        {
            // Soporta "true"/"false" y "1"/"0"
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue(key, out string val))
            {
                if (bool.TryParse(val, out bool result)) return result;
                if (val == "1") return true;
                if (val == "0") return false;
            }
            return defaultValue;
        }

        /// <summary>
        /// Lee un color Hexadecimal (Ej: "#FF0000").
        /// </summary>
        protected Color GetCustomColor(CustomMonster monster, string key, Color defaultColor)
        {
            string hex = GetCustomString(monster, key, null);
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            
            try {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                return new Color(
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16)
                );
            } catch { return defaultColor; }
        }
    }
}
