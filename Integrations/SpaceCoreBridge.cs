using StardewModdingAPI;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Integrations
{
    public static class SpaceCoreBridge
    {
        private const string SpaceCoreId = "spacechase0.SpaceCore";
        private static ISpaceCoreApi _api;

        /// <summary>
        /// Intenta conectar con la API de SpaceCore y registrar los tipos personalizados.
        /// </summary>
        public static bool Init(IModHelper helper, IMonitor monitor)
        {
            // Intentamos obtener la API
            _api = helper.ModRegistry.GetApi<ISpaceCoreApi>(SpaceCoreId);

            if (_api == null)
            {
                monitor.Log("SpaceCore no encontrado. Los monstruos personalizados no se guardarán y podrían causar errores al dormir.", LogLevel.Warn);
                return false;
            }

            try
            {
                // Registramos el tipo. SpaceCore se encarga del resto.
                _api.RegisterSerializerType(typeof(CustomMonster));
                monitor.Log("SpaceCore conectado. Serialización de CustomMonster activa.", LogLevel.Info);
                return true;
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Error al registrar tipos en SpaceCore: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }

    // Definición de la interfaz interna para no depender de la DLL física de SpaceCore
    // Esto permite compilar el mod sin tener SpaceCore.dll en las referencias si quisieras.
    public interface ISpaceCoreApi
    {
        void RegisterSerializerType(System.Type type);
    }
}