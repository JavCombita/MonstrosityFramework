using Microsoft.Xna.Framework;
using MonstrosityFramework.Entities.Behaviors;

namespace MonstrosityFramework.API
{
    public interface IMonstrosityApi
    {
        /// Registers a new monster into the framework using raw data objects.
        /// Useful for Content Patcher integrations or direct code calls.
        /// <param name="id">The unique ID for the monster (e.g., "MyMod.KillerRobot").</param>
        /// <param name="data">The MonsterData object containing stats and texture paths.</param>
        void RegisterMonster(string id, object data);

        /// Spawns and adds a custom monster instance to the game world.
        /// <param name="id">The ID of the registered monster to spawn.</param>
        /// <param name="position">The coordinates (in pixels) for the spawn. If using Tile coordinates, multiply by 64f.</param>
        /// <param name="locationName">The name of the location (e.g., "Farm", "UndergroundMine"). If null, uses the player's current location.</param>
        /// <returns>The spawned CustomMonster instance, or null if the operation failed.</returns>
        object SpawnMonster(string id, Vector2 position, string locationName = null);

        /// Registers a new Artificial Intelligence (Behavior) logic.
        /// Allows other mods to add unique AI types (e.g., "Ninja", "Healer") that can then be referenced in JSON files.
        /// <param name="behaviorId">The unique ID for this behavior (e.g., "mymod.ninja"). Case-insensitive.</param>
        /// <param name="behavior">An instance of a class inheriting from MonsterBehavior.</param>
        void RegisterBehavior(string behaviorId, MonsterBehavior behavior);
    }
}
