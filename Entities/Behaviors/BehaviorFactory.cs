using System.Collections.Generic;

namespace MonstrosityFramework.Entities.Behaviors
{
    public static class BehaviorFactory
    {
        private static readonly Dictionary<string, MonsterBehavior> _behaviors = new();
        private static readonly MonsterBehavior _defaultBehavior = new StalkerBehavior();

        static BehaviorFactory()
        {
            // Registro de Behaviors Vanilla
            Register("default", _defaultBehavior);
            Register("stalker", new StalkerBehavior()); // IA Básica
            
            Register("bat", new BatBehavior());         // Vuelo, Espiral, Lunge
            Register("ghost", new GhostBehavior());     // Vuelo lento, Luz, Oscilación
            Register("slime", new SlimeBehavior());     // Salto, Reproducción
            Register("rockcrab", new RockCrabBehavior()); // Camuflaje, Caparazón
            Register("duggy", new DuggyBehavior());     // Ataque subterráneo
            Register("shooter", new ShooterBehavior()); // Proyectiles
            Register("mummy", new MummyBehavior());     // Revivir, Crusader check
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
