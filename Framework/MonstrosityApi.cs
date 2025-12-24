using StardewModdingAPI;
using MonstrosityFramework.API;
using MonstrosityFramework.Framework.Data;
using MonstrosityFramework.Framework.Registries;

namespace MonstrosityFramework.Framework
{
    public class MonstrosityApi : IMonstrosityApi
    {
        // Referencia al Monitor para logs (inyectado desde ModEntry)
        private readonly IMonitor _monitor;

        public MonstrosityApi(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void RegisterMonster(IManifest mod, string localId, MonsterData data)
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(localId))
            {
                _monitor.Log($"El mod {mod.Name} intentó registrar un monstruo sin ID.", LogLevel.Error);
                return;
            }
            if (data == null)
            {
                _monitor.Log($"El mod {mod.Name} intentó registrar datos nulos para {localId}.", LogLevel.Error);
                return;
            }

            // Generar GUID: "Author.ModName.MonsterId"
            string uniqueId = $"{mod.UniqueID}.{localId}";

            _monitor.Log($"Registrando monstruo: {uniqueId} ({data.DisplayName})", LogLevel.Trace);

            // Crear el wrapper y guardar
            var entry = new RegisteredMonster(mod, data);
            MonsterRegistry.Register(uniqueId, entry);
        }

        public MonsterData GetMonsterData(string uniqueId)
        {
            var entry = MonsterRegistry.Get(uniqueId);
            return entry?.Data;
        }

        public bool IsMonsterRegistered(string uniqueId)
        {
            return MonsterRegistry.IsRegistered(uniqueId);
        }
    }
}