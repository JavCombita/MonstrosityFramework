using StardewModdingAPI;
using MonstrosityFramework.Framework.Data;

namespace MonstrosityFramework.API
{
    public interface IMonstrosityApi
    {
        /// <summary>
        /// Registra un nuevo monstruo en el sistema.
        /// Debe llamarse durante el evento GameLaunched o después.
        /// </summary>
        /// <param name="mod">El manifiesto del mod que añade el monstruo (usar this.ModManifest).</param>
        /// <param name="localId">ID local del monstruo (ej: "VoidGoblin").</param>
        /// <param name="data">El objeto de configuración cargado desde el JSON.</param>
        void RegisterMonster(IManifest mod, string localId, MonsterData data);

        /// <summary>
        /// Obtiene los datos de un monstruo registrado.
        /// </summary>
        /// <param name="uniqueId">El ID único global (ej: "AuthorName.ModName.VoidGoblin").</param>
        MonsterData GetMonsterData(string uniqueId);

        /// <summary>
        /// Verifica si un ID de monstruo existe en el registro.
        /// </summary>
        bool IsMonsterRegistered(string uniqueId);
    }
}