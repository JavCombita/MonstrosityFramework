using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using MonstrosityFramework.API;
using MonstrosityFramework.Entities; // Necesario para CustomMonster
using MonstrosityFramework.Framework.Data;
using MonstrosityFramework.Framework.Registries;

namespace MonstrosityFramework.Framework
{
    public class MonstrosityApi : IMonstrosityApi
    {
        private readonly IManifest _manifest;

        public MonstrosityApi(IManifest manifest)
        {
            _manifest = manifest;
        }

        public void RegisterMonster(IManifest ownerMod, string id, MonsterData data)
        {
            string fullId = $"{ownerMod.UniqueID}.{id}";
            MonsterRegistry.Register(fullId, data, null, ownerMod);
        }

        public void RegisterMonsterFromPack(IContentPack pack, string localId, MonsterData data)
        {
            string fullId = $"{pack.Manifest.UniqueID}.{localId}";
            MonsterRegistry.Register(fullId, data, pack, pack.Manifest);
        }

        public MonsterData GetMonsterData(string id)
        {
            return MonsterRegistry.Get(id)?.Data;
        }

        // --- MÉTODO CLAVE PARA OTROS MODDERS ---
        public Monster SpawnMonster(string id, GameLocation location, Vector2 tile)
        {
            // Verificamos que el ID exista
            var entry = MonsterRegistry.Get(id);
            if (entry == null) return null;

            // Instanciamos DIRECTAMENTE
            var monster = new CustomMonster(id, tile * 64f);
            
            if (location != null)
            {
                location.characters.Add(monster);
            }

            return monster;
        }
    }
}