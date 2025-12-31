using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class StalkerBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            float detection = GetVisionRange(monster, 16f);

            if (!IsPlayerWithinRange(monster, detection))
            {
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();
                monster.Sprite.currentFrame = monster.FacingDirection * 4;
                return;
            }

            monster.IsWalkingTowardPlayer = true;
            monster.moveTowardPlayer(monster.Speed);

            int startFrame = monster.FacingDirection * 4;
            monster.Sprite.Animate(time, startFrame, 4, 150f);
        }
    }
}