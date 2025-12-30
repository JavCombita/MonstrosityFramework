using Microsoft.Xna.Framework;
using MonstrosityFramework.Entities.Behaviors;

namespace MonstrosityFramework.API
{
    public interface IMonstrosityApi
    {
        /// <summary>
        /// Registra un nuevo monstruo en el sistema usando datos crudos (útil para Content Patcher o llamadas directas).
        /// </summary>
        /// <param name="id">El ID único del monstruo (ej: "MiMod.RobotAsesino").</param>
        /// <param name="data">Objeto MonsterData con las estadísticas y texturas.</param>
        void RegisterMonster(string id, object data);

        /// <summary>
        /// Genera e invoca un monstruo personalizado en el mundo.
        /// </summary>
        /// <param name="id">El ID del monstruo a invocar.</param>
        /// <param name="position">Coordenadas en el mapa (Tile o Pixeles, el spawn lo gestiona).</param>
        /// <param name="locationName">Nombre del mapa (ej: "Farm"). Si es null, usa el mapa actual.</param>
        /// <returns>La instancia del CustomMonster creado, o null si falló.</returns>
        object SpawnMonster(string id, Vector2 position, string locationName = null);

        /// <summary>
        /// [ELITE FEATURE] Registra una nueva lógica de Inteligencia Artificial (Behavior).
        /// Permite a otros mods añadir tipos de IA (ej: "Ninja", "Healer") que luego pueden usarse en los JSONs.
        /// </summary>
        /// <param name="behaviorId">El ID único para este comportamiento (ej: "mymod.ninja"). Insensible a mayúsculas.</param>
        /// <param name="behavior">Una instancia de una clase que herede de MonsterBehavior.</param>
        void RegisterBehavior(string behaviorId, MonsterBehavior behavior);
    }
}