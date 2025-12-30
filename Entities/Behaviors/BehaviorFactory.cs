using System.Collections.Generic;

namespace MonstrosityFramework.Entities.Behaviors
{
    public static class BehaviorFactory
    {
        private static readonly Dictionary<string, MonsterBehavior> _behaviors = new();
        private static readonly MonsterBehavior _defaultBehavior = new StalkerBehavior();

        static BehaviorFactory()
		{
			Register("default", _defaultBehavior);
			Register("stalker", new StalkerBehavior());
			Register("slime", new SlimeBehavior());
			Register("bat", new BatBehavior());
			Register("ghost", new BatBehavior()); // Reusamos Bat por ahora
			Register("rockcrab", new RockCrabBehavior());
			Register("mummy", new MummyBehavior());
			Register("shooter", new ShooterBehavior());
			Register("duggy", new DuggyBehavior());
		}

        public static void Register(string id, MonsterBehavior behavior)
        {
            _behaviors[id.ToLowerInvariant()] = behavior;
        }

        public static MonsterBehavior GetBehavior(string id)
        {
            if (string.IsNullOrEmpty(id)) return _defaultBehavior;
            return _behaviors.TryGetValue(id.ToLowerInvariant(), out var behavior) ? behavior : _defaultBehavior;
        }
    }
}