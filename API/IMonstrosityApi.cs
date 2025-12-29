using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using MonstrosityFramework.Framework.Data;

namespace MonstrosityFramework.API
{
    public interface IMonstrosityApi
    {
        /// <summary>
        /// Registra un monstruo manualmente (Legacy / C# directo).
        /// </summary>
        void RegisterMonster(IManifest ownerMod, string id, MonsterData data);

        /// <summary>
        /// Registra un monstruo desde un Content Pack (Recomendado).
        /// </summary>
        void RegisterMonsterFromPack(IContentPack pack, string localId, MonsterData data);

        /// <summary>
        /// Obtiene los datos de un monstruo registrado.
        /// </summary>
        MonsterData GetMonsterData(string id);

        /// <summary>
        /// Spawnea un monstruo en el mundo de forma segura.
        /// Devuelve la instancia del monstruo (como NPC) o null si falló.
        /// </summary>
        /// <param name="id">ID del monstruo (ej: "Author.Mod.Monster")</param>
        /// <param name="location">Mapa donde aparecerá</param>
        /// <param name="tile">Coordenadas en Tiles (x, y)</param>
        Monster SpawnMonster(string id, GameLocation location, Vector2 tile);
    }
}