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

        /// <summary>
        /// Obtiene los datos originales del JSON para leer configuraciones extra.
        /// </summary>
        protected MonsterData GetData(CustomMonster monster)
        {
            if (string.IsNullOrEmpty(monster.MonsterSourceId.Value)) return null;
            return MonsterRegistry.Get(monster.MonsterSourceId.Value)?.Data;
        }

        protected bool IsPlayerWithinRange(CustomMonster monster, float tiles)
        {
            return monster.withinPlayerThreshold((int)tiles);
        }

        protected void MoveTowardPlayer(CustomMonster monster, int speed)
        {
            monster.IsWalkingTowardPlayer = true;
            monster.moveTowardPlayer(speed);
        }
    }
}