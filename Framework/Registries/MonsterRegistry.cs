using System.Collections.Generic;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data; 

namespace MonstrosityFramework.Framework.Registries
{
    public static class MonsterRegistry
    {
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Advertencia: Sobreescribiendo definición de '{uniqueId}'.", LogLevel.Warn);
                _registry[uniqueId]?.Dispose();
            }
            _registry[uniqueId] = monster;
        }

        public static void Register(string uniqueId, MonsterData data, IContentPack pack, IManifest manifest = null)
        {
            // Ahora sí existe este constructor en RegisteredMonster.cs
            var registeredMonster = new RegisteredMonster(data, pack, manifest);
            Register(uniqueId, registeredMonster);
        }

        public static RegisteredMonster Get(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            return _registry.TryGetValue(uniqueId, out var monster) ? monster : null;
        }

        // --- NUEVO MÉTODO (Soluciona el error CS0117) ---
        public static bool IsRegistered(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return false;
            return _registry.ContainsKey(uniqueId);
        }

        public static IEnumerable<string> GetAllIds() => _registry.Keys;

        public static void Cleanup()
        {
            foreach (var monster in _registry.Values) monster?.Dispose();
        }

        public static void ClearAll()
        {
            Cleanup();
            _registry.Clear();
        }
    }
}