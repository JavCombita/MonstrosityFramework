using System.Collections.Generic;
using StardewModdingAPI;
using MonstrosityFramework.Framework.Data; // Asegúrate de tener este using

namespace MonstrosityFramework.Framework.Registries
{
    public static class MonsterRegistry
    {
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        // --- MÉTODO PRINCIPAL (Ya existía) ---
        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Advertencia: Sobreescribiendo definición de '{uniqueId}'.", LogLevel.Warn);
                _registry[uniqueId]?.Dispose();
            }
            _registry[uniqueId] = monster;
        }

        // --- NUEVA SOBRECARGA (Soluciona el error de 4 argumentos) ---
        // Este método actúa como "fábrica", creando el RegisteredMonster por ti.
        public static void Register(string uniqueId, MonsterData data, IContentPack pack, IManifest manifest = null)
        {
            // Creamos la instancia de RegisteredMonster aquí mismo
            var registeredMonster = new RegisteredMonster(data, pack, manifest);
            
            // Llamamos al método principal
            Register(uniqueId, registeredMonster);
        }

        public static RegisteredMonster Get(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            return _registry.TryGetValue(uniqueId, out var monster) ? monster : null;
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