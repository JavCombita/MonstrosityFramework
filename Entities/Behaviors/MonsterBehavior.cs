using Microsoft.Xna.Framework;
using StardewValley;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Framework.Data;

namespace MonstrosityFramework.Entities.Behaviors
{
    public abstract class MonsterBehavior
    {
        public abstract void Update(CustomMonster monster, GameTime time);

        public virtual int OnTakeDamage(CustomMonster monster, int damage, bool isBomb, Farmer who) 
        {
            return damage; 
        }

        // --- Helpers de Utilidad ---

        protected MonsterData GetData(CustomMonster monster)
        {
            if (string.IsNullOrEmpty(monster.MonsterSourceId.Value)) return null;
            return MonsterRegistry.Get(monster.MonsterSourceId.Value)?.Data;
        }

        protected bool IsPlayerWithinRange(CustomMonster monster, float tiles)
        {
            return monster.withinPlayerThreshold((int)tiles);
        }

        /// <summary>
        /// Obtiene el rango de visión definido en el JSON o un default.
        /// Busca la clave "DetectionRange" en CustomFields.
        /// </summary>
        protected float GetVisionRange(CustomMonster monster, float defaultRange = 8f)
        {
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue("DetectionRange", out string rangeStr))
            {
                if (float.TryParse(rangeStr, out float parsed)) return parsed;
            }
            return defaultRange;
        }

        protected void MoveTowardPlayer(CustomMonster monster, int speed)
        {
            monster.IsWalkingTowardPlayer = true;
            monster.moveTowardPlayer(speed);
        }
    }
}