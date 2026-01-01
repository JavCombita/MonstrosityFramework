using StardewModdingAPI;
using MonstrosityFramework.Entities;
using System;

namespace MonstrosityFramework.Integrations
{
    public static class SpaceCoreBridge
    {
        private const string SpaceCoreId = "spacechase0.SpaceCore";

        public static bool Init(IModHelper helper, IMonitor monitor)
        {
            // 1. Verificar si SpaceCore está instalado
            if (!helper.ModRegistry.IsLoaded(SpaceCoreId))
            {
                monitor.Log("SpaceCore no detectado. Los monstruos NO se guardarán al dormir.", LogLevel.Warn);
                return false;
            }

            try
            {
                // 2. Obtener la API usando la Interfaz Fantasma
                // SMAPI mapeará automáticamente los métodos de SpaceCore a esta interfaz.
                var api = helper.ModRegistry.GetApi<ISpaceCoreApi>(SpaceCoreId);

                if (api != null)
                {
                    // 3. Registrar nuestro tipo
                    api.RegisterSerializerType(typeof(CustomMonster));
                    
                    monitor.Log("Integración SpaceCore: ACTIVA (Serialización habilitada).", LogLevel.Info);
                    return true;
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error conectando con la API de SpaceCore: {ex.Message}", LogLevel.Error);
            }

            return false;
        }
    }

    // --- INTERFAZ FANTASMA ---
    // Define solo lo que necesitamos. SMAPI hace el resto.
    public interface ISpaceCoreApi
    {
        void RegisterSerializerType(Type type);
    }
}
