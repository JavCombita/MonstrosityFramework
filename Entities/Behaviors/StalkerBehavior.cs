using Microsoft.Xna.Framework;
using StardewValley;

namespace MonstrosityFramework.Entities.Behaviors
{
    public class StalkerBehavior : MonsterBehavior
    {
        public override void Update(CustomMonster monster, GameTime time)
        {
            float detectionRange = 16f;
            var data = GetData(monster);
            if (data != null && data.CustomFields.TryGetValue("DetectionRange", out string rangeStr))
            {
                if (float.TryParse(rangeStr, out float parsed)) detectionRange = parsed;
            }

            if (!IsPlayerWithinRange(monster, detectionRange))
            {
                monster.IsWalkingTowardPlayer = false;
                monster.Halt();
                // Frame Idle según dirección
                int idleFrame = 0;
                switch(monster.FacingDirection)
                {
                    case 2: idleFrame = 0; break; 
                    case 1: idleFrame = 4; break;
                    case 0: idleFrame = 8; break;
                    case 3: idleFrame = 12; break;
                }
                monster.Sprite.currentFrame = idleFrame;
                return;
            }

            monster.IsWalkingTowardPlayer = true;
            monster.moveTowardPlayer(monster.Speed);

            // ANIMACIÓN DIRECCIONAL
            int baseRowStart = 0;
            switch(monster.FacingDirection)
            {
                case 2: baseRowStart = 0; break;  // Sur
                case 1: baseRowStart = 4; break;  // Este
                case 0: baseRowStart = 8; break;  // Norte
                case 3: baseRowStart = 12; break; // Oeste
            }
            monster.Sprite.Animate(time, baseRowStart, 4, 150f);
        }
    }
}