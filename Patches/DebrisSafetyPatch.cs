using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations; 
using StardewModdingAPI;
using System;

namespace MonstrosityFramework.Patches
{
    [HarmonyPatch(typeof(GameLocation), "drawDebris")]
    public static class DebrisSafetyPatch
    {
        // Se ejecuta ANTES de que el juego dibuje los escombros
        public static void Prefix(GameLocation __instance)
        {
            if (__instance.debris == null || __instance.debris.Count == 0) return;

            try
            {
                // Iteramos al revés para poder borrar elementos sin romper el índice
                for (int i = __instance.debris.Count - 1; i >= 0; i--)
                {
                    var d = __instance.debris[i];

                    // Si es un escombro de tipo Sprite (el que causa el crash)
                    if (d.debrisType.Value == Debris.DebrisType.SPRITECHUNKS)
                    {
                        // VERIFICACIÓN SUPREMA: ¿Es null la textura?
                        if (d.spriteChunkSheet == null)
                        {
                            // Si es null, lo eliminamos inmediatamente.
                            // No intentamos arreglarlo. Muerto el perro, se acabó la rabia.
                            __instance.debris.RemoveAt(i);
                            
                            // Log opcional para depuración (puedes comentarlo si hace mucho spam)
                            // ModEntry.StaticMonitor.LogOnce($"[DebrisSafety] Escombro corrupto eliminado en {__instance.Name}", LogLevel.Trace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Si este parche falla, atrapamos el error para no ser nosotros los que crasheamos el juego
                ModEntry.StaticMonitor.LogOnce($"Error en DebrisSafetyPatch: {ex.Message}", LogLevel.Error);
            }
        }
    }
}