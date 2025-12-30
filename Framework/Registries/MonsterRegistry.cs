using System.Collections.Generic;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data; 

namespace MonstrosityFramework.Framework.Registries
{
    public static class MonsterRegistry
    {
        // Diccionario Central
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        /// <summary>
        /// Registra un wrapper ya creado.
        /// </summary>
        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[Registry] Sobreescribiendo ID existente: '{uniqueId}'.", LogLevel.Warn);
                _registry[uniqueId]?.Dispose();
            }
            _registry[uniqueId] = monster;
        }

        /// <summary>
        /// Crea el wrapper y lo registra (Sobrecarga principal para API/CP).
        /// </summary>
        public static void Register(string uniqueId, MonsterData data, IContentPack pack, IManifest manifest = null)
        {
            if (data == null)
            {
                ModEntry.StaticMonitor.Log($"[Registry] Intento de registrar '{uniqueId}' con datos nulos.", LogLevel.Error);
                return;
            }
            
            var registeredMonster = new RegisteredMonster(data, pack, manifest);
            Register(uniqueId, registeredMonster);
        }

        public static RegisteredMonster Get(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            return _registry.TryGetValue(uniqueId, out var monster) ? monster : null;
        }

        public static bool IsRegistered(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return false;
            return _registry.ContainsKey(uniqueId);
        }

        public static IEnumerable<string> GetAllIds() => _registry.Keys;

        public static void Clear()
        {
            foreach (var monster in _registry.Values)
            {
                monster?.Dispose();
            }
            _registry.Clear();
        }
    }
}