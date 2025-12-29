using System.Collections.Generic;
using StardewModdingAPI;

namespace MonstrosityFramework.Framework.Registries
{
    /// <summary>
    /// Base de datos estática que mantiene vivos todos los tipos de monstruos cargados.
    /// </summary>
    public static class MonsterRegistry
    {
        // Almacenamiento central: ID Global (ej: "Author.Mod.Monster") -> Datos y Texturas
        private static readonly Dictionary<string, RegisteredMonster> _registry = new();

        /// <summary>
        /// Registra un nuevo monstruo en el sistema.
        /// </summary>
        public static void Register(string uniqueId, RegisteredMonster monster)
        {
            if (_registry.ContainsKey(uniqueId))
            {
                ModEntry.StaticMonitor.Log($"[MonsterRegistry] Advertencia: Sobreescribiendo definición de '{uniqueId}'.", LogLevel.Warn);
                // Si ya existía, liberamos sus recursos (texturas viejas) antes de reemplazarlo
                _registry[uniqueId]?.Dispose();
            }
            
            _registry[uniqueId] = monster;
        }

        /// <summary>
        /// Obtiene un monstruo registrado por su ID único.
        /// </summary>
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

        public static IEnumerable<RegisteredMonster> GetAll() => _registry.Values;

        /// <summary>
        /// Libera la memoria de video (VRAM) de todas las texturas cargadas.
        /// Útil al volver al título para evitar fugas de memoria.
        /// </summary>
        public static void Cleanup()
        {
            foreach (var monster in _registry.Values)
            {
                monster?.Dispose();
            }
        }

        /// <summary>
        /// Elimina todos los registros y limpia memoria. 
        /// VITAL para el comando 'monster_reload'.
        /// </summary>
        public static void ClearAll()
        {
            Cleanup(); // Primero libera texturas
            _registry.Clear(); // Luego vacía la lista
            ModEntry.StaticMonitor.Log("[MonsterRegistry] Base de datos purgada.", LogLevel.Trace);
        }
    }
}