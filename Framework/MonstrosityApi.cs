using Microsoft.Xna.Framework;
using MonstrosityFramework.API;
using MonstrosityFramework.Entities;
using MonstrosityFramework.Entities.Behaviors;
using MonstrosityFramework.Framework.Data;
using MonstrosityFramework.Framework.Registries;
using StardewModdingAPI;
using StardewValley;
using System;

namespace MonstrosityFramework.Framework
{
    public class MonstrosityApi : IMonstrosityApi
    {
        private readonly IMonitor _monitor;

        public MonstrosityApi(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void RegisterMonster(string id, object data)
        {
            try
            {
                if (data is MonsterData mData)
                {
                    // Registramos el monstruo en tu sistema central
                    MonsterRegistry.Register(id, mData, null, null);
                    _monitor.Log($"[API] Monstruo registrado correctamente: {id}", LogLevel.Trace);
                }
                else
                {
                    _monitor.Log($"[API] Error al registrar {id}: Los datos recibidos no son de tipo MonsterData.", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"[API] Excepción crítica al registrar monstruo {id}: {ex.Message}", LogLevel.Error);
            }
        }

        public object SpawnMonster(string id, Vector2 position, string locationName = null)
        {
            try
            {
                // Determinar ubicación
                GameLocation loc = locationName != null ? Game1.getLocationFromName(locationName) : Game1.currentLocation;
                
                if (loc == null)
                {
                    _monitor.Log($"[API] No se pudo spawnear {id}: La ubicación '{locationName}' no existe.", LogLevel.Warn);
                    return null;
                }

                // Verificar si el monstruo existe en el registro
                if (MonsterRegistry.Get(id) == null)
                {
                    _monitor.Log($"[API] No se pudo spawnear {id}: ID no registrado.", LogLevel.Warn);
                    return null;
                }

                // Crear y añadir la entidad
                var monster = new CustomMonster(id, position);
                loc.characters.Add(monster);
                
                _monitor.Log($"[API] Spawneado {id} en {loc.Name} ({position})", LogLevel.Trace);
                return monster;
            }
            catch (Exception ex)
            {
                _monitor.Log($"[API] Error al spawnear {id}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public void RegisterBehavior(string behaviorId, MonsterBehavior behavior)
        {
            if (string.IsNullOrEmpty(behaviorId))
            {
                _monitor.Log("[API] Error: Se intentó registrar un Behavior con ID vacío.", LogLevel.Warn);
                return;
            }

            if (behavior == null)
            {
                _monitor.Log($"[API] Error: El Behavior '{behaviorId}' es nulo.", LogLevel.Warn);
                return;
            }

            try
            {
                // Conectar con la Factory que creamos antes
                BehaviorFactory.Register(behaviorId, behavior);
                _monitor.Log($"[API] Nuevo comportamiento de IA registrado: '{behaviorId}'", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"[API] Error al registrar Behavior '{behaviorId}': {ex.Message}", LogLevel.Error);
            }
        }
    }
}