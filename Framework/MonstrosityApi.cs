using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters; // Necesario para devolver el tipo 'Monster'
using MonstrosityFramework.API;
using MonstrosityFramework.Framework.Data;
using MonstrosityFramework.Framework.Registries;
using MonstrosityFramework.Entities;

namespace MonstrosityFramework.Framework
{
    public class MonstrosityApi : IMonstrosityApi
    {
        private readonly IMonitor _monitor;

        public MonstrosityApi(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void RegisterMonster(IManifest ownerMod, string id, MonsterData data)
        {
            string globalId = id.Contains(".") ? id : $"{ownerMod.UniqueID}.{id}";
            var entry = new RegisteredMonster(ownerMod, data);
            MonsterRegistry.Register(globalId, entry);
            _monitor.Log($"[API] Monstruo registrado (Legacy): {globalId}", LogLevel.Trace);
        }

        public void RegisterMonsterFromPack(IContentPack pack, string localId, MonsterData data)
        {
            string globalId = $"{pack.Manifest.UniqueID}.{localId}";
            var entry = new RegisteredMonster(pack, data);
            MonsterRegistry.Register(globalId, entry);
            _monitor.Log($"[API] Monstruo registrado (Pack): {globalId}", LogLevel.Trace);
        }

        public MonsterData GetMonsterData(string id)
        {
            var entry = MonsterRegistry.Get(id);
            return entry?.Data;
        }

        // --- NUEVO MÉTODO PARA MODDERS C# ---
        public Monster SpawnMonster(string id, GameLocation location, Vector2 tile)
        {
            if (location == null) return null;

            if (!MonsterRegistry.IsRegistered(id))
            {
                _monitor.Log($"[API] Intento de spawnear ID desconocido: '{id}'", LogLevel.Warn);
                return null;
            }

            // Crear la entidad
            // Nota: Multiplicamos por 64f porque CustomMonster espera píxeles, pero la API recibe Tiles.
            var monster = new CustomMonster(id, tile * 64f);
            
            // Añadir al mundo
            location.characters.Add(monster);
            
            _monitor.Log($"[API] Spawneado '{id}' en {location.Name} ({tile.X}, {tile.Y})", LogLevel.Trace);
            
            return monster;
        }
    }
}