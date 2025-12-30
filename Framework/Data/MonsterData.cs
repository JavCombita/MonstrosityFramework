using System.Collections.Generic;

namespace MonstrosityFramework.Framework.Data
{
    /// <summary>
    /// Define las estadísticas, apariencia y comportamiento de un monstruo personalizado.
    /// Mapea directamente el archivo monsters.json del usuario.
    /// </summary>
    public class MonsterData
    {
        // --- Identidad Visual ---
        public string DisplayName { get; set; } = "Unknown Monster";
        
        /// <summary>
        /// ID del Content Pack (Opcional). Útil para logs.
        /// </summary>
        public string ContentPackID { get; set; } = null;
        
        /// <summary>
        /// Ruta de la textura. 
        /// Si es un Content Pack: relativa a la carpeta del mod (ej: "assets/sprite.png").
        /// Si es CP/Global: ruta completa del juego (ej: "Mods/Author/Texture").
        /// </summary>
        public string TexturePath { get; set; }
        
        public int SpriteWidth { get; set; } = 16;
        public int SpriteHeight { get; set; } = 24;

        // --- Estadísticas de Combate ---
        public int MaxHealth { get; set; } = 20;
        public int DamageToFarmer { get; set; } = 5;
        public int Defense { get; set; } = 0;
        public int Exp { get; set; } = 10;
        public int Speed { get; set; } = 2;

        // --- Inteligencia Artificial ---
        public string BehaviorType { get; set; } = "Default";

        // --- Loot (Inicializado para evitar NullRef) ---
        public List<MonsterDropData> Drops { get; set; } = new();

        // --- Reglas de Aparición (Inicializado) ---
        public SpawnRules Spawn { get; set; } = new();

        // --- Extensibilidad ---
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    public class SpawnRules
    {
        public int MinMineLevel { get; set; } = -1;
        public int MaxMineLevel { get; set; } = -1;
        public double SpawnWeight { get; set; } = 1.0;
        
        // Inicializado para seguridad
        public List<string> SpecificLocations { get; set; } = new(); 
    }
}