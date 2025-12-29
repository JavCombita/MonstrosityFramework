using StardewModdingAPI;
using SpaceCore;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Integrations
{
    public static class SpaceCoreBridge
    {
        public static bool Init(IModHelper helper, IMonitor monitor)
        {
            if (!helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
                return false;

            // REFLECTION SAFE: Usamos la API de SpaceCore para registrar la clase
            var spaceCoreApi = helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            if (spaceCoreApi != null)
            {
                // Esto permite que el juego guarde "Mods_JavCombita_Monstrosity_CustomMonster" en el XML
                spaceCoreApi.RegisterSerializerType(typeof(CustomMonster));
                monitor.Log("SpaceCore API enganchada: CustomMonster registrado para serialización.", LogLevel.Info);
                return true;
            }
            return false;
        }
    }

    // Interfaz dummy para Reflection si no quieres compilar contra la DLL directamente (opcional pero recomendado)
    public interface ISpaceCoreApi
    {
        void RegisterSerializerType(Type type);
    }
}