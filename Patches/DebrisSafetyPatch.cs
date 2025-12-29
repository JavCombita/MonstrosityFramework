using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations; // Necesario para GameLocation
using System.Collections.Generic;
using StardewModdingAPI;
using System;

namespace MonstrosityFramework.Patches
{
    [HarmonyPatch(typeof(GameLocation), "drawDebris")]
    public static class DebrisSafetyPatch
    {
        private static Texture2D _fallbackTexture;

        public static void Prefix(GameLocation __instance)
        {
            try
            {
                for (int i = __instance.debris.Count - 1; i >= 0; i--)
                {
                    var debris = __instance.debris[i];

                    if (debris.debrisType.Value == Debris.DebrisType.SPRITECHUNKS)
                    {
                        // EL CAMBIO ESTÁ AQUÍ: Leemos la propiedad y si es null, usamos Reflection para escribir
                        if (debris.spriteChunkSheet == null)
                        {
                            // Cargamos textura de emergencia si no existe
                            if (_fallbackTexture == null || _fallbackTexture.IsDisposed)
                            {
                                try { _fallbackTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); }
                                catch { /* Nada que hacer */ }
                            }
                            
                            if (_fallbackTexture != null)
                            {
                                // REFLECTION: Forzamos la escritura en la propiedad de solo lectura
                                // Usamos 'spriteChunkSheet' asumiendo que SMAPI encontrará el campo de respaldo
                                ModEntry.ModHelper.Reflection
                                    .GetProperty<Texture2D>(debris, "spriteChunkSheet")
                                    .SetValue(_fallbackTexture);
                                    
                                // Opcional: Si GetProperty falla, prueba GetField. 
                                // Pero en 1.6 suele ser una propiedad.
                            }
                            else
                            {
                                // Si todo falla, borrar el debris para evitar el crash
                                __instance.debris.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Loguear solo una vez para no spammear si falla mucho
                 ModEntry.StaticMonitor.LogOnce($"Error en DebrisSafetyPatch: {ex.Message}", LogLevel.Trace);
            }
        }
    }
}