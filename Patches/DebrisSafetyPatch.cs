using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;

namespace MonstrosityFramework.Patches
{
    [HarmonyPatch(typeof(GameLocation), "drawDebris")]
    public static class DebrisSafetyPatch
    {
        // Textura de emergencia (cargada una sola vez)
        private static Texture2D _fallbackTexture;

        public static void Prefix(GameLocation __instance)
        {
            try
            {
                // Iteramos al revés para poder eliminar elementos sin romper el índice
                for (int i = __instance.debris.Count - 1; i >= 0; i--)
                {
                    var debris = __instance.debris[i];

                    // Verificamos si es un tipo de escombro que requiere textura
                    if (debris.debrisType.Value == Debris.DebrisType.SPRITECHUNKS)
                    {
                        if (debris.spriteChunkSheet == null)
                        {
                            ModEntry.StaticMonitor.Log($"[DebrisSafety] ¡Debris corrupto detectado en {__instance.Name}! Neutralizando...", LogLevel.Warn);

                            // OPCIÓN A: Repararlo (Asignar textura de Shadow Brute)
                            if (_fallbackTexture == null || _fallbackTexture.IsDisposed)
                            {
                                try { _fallbackTexture = Game1.content.Load<Texture2D>("Characters/Monsters/Shadow Brute"); }
                                catch { /* Si falla esto, estamos perdidos */ }
                            }
                            
                            if (_fallbackTexture != null)
                            {
                                debris.spriteChunkSheet = _fallbackTexture;
                            }
                            else
                            {
                                // OPCIÓN B: Si no podemos repararlo, lo borramos para evitar el crash
                                __instance.debris.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Si el parche falla, no queremos romper el juego, solo lo logueamos.
                ModEntry.StaticMonitor.Log($"Error en DebrisSafetyPatch: {ex.Message}", LogLevel.Error);
            }
        }
    }
}