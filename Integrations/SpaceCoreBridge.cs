using StardewModdingAPI;
using MonstrosityFramework.Entities;
using System;
using System.Reflection;

namespace MonstrosityFramework.Integrations
{
    public static class SpaceCoreBridge
    {
        private const string SpaceCoreId = "spacechase0.SpaceCore";

        public static bool Init(IModHelper helper, IMonitor monitor)
        {
            // Verificamos si SpaceCore está cargado
            if (!helper.ModRegistry.IsLoaded(SpaceCoreId))
            {
                monitor.Log("SpaceCore no encontrado. Los monstruos no se serializarán.", LogLevel.Warn);
                return false;
            }

            try
            {
                // TRUCO DE REFLECTION: Obtenemos la API usando nuestra interfaz local 'ISpaceCoreApi'
                // Esto engaña al compilador para que no pida "using SpaceCore;"
                var api = helper.ModRegistry.GetApi<ISpaceCoreApi>(SpaceCoreId);

                if (api != null)
                {
                    api.RegisterSerializerType(typeof(CustomMonster));
                    monitor.Log("SpaceCore conectado exitosamente.", LogLevel.Info);
                    return true;
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error conectando con SpaceCore: {ex.Message}", LogLevel.Error);
            }

            return false;
        }
    }

    // --- INTERFAZ FANTASMA ---
    // Copiamos la firma del método que necesitamos de SpaceCore.
    // Como está definida AQUÍ, no necesitamos 'using SpaceCore'.
    public interface ISpaceCoreApi
    {
        void RegisterSerializerType(Type type);
    }
}