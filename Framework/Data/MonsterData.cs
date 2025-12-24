using System.Collections.Generic;

namespace MonstrosityFramework.Framework.Data
{
    /// <summary>
    /// Define las estadísticas, apariencia y comportamiento de un monstruo personalizado.
    /// Esto mapea directamente el archivo monsters.json del usuario.
    /// </summary>
    public class MonsterData
    {
        // --- Identidad Visual ---
        public string DisplayName { get; set; } = "Unknown Monster";
		
		/// <summary>
        /// [OPCIONAL] Si usas Content Patcher, pon aquí el UniqueID de tu Content Pack.
        /// Si lo dejas vacío, buscará en la carpeta del mod que registró el monstruo.
        /// </summary>
        public string ContentPackID { get; set; } = null;
        
        /// <summary>
        /// Ruta relativa a la carpeta del mod hijo (ej: "assets/sprites/goblin.png").
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
        /// <summary>
        /// Define qué lógica usará el monstruo. 
        /// Valores soportados: "Default", "Stalker", "Shooter", "Tank".
        /// </summary>
        public string BehaviorType { get; set; } = "Default";

        // --- Loot ---
        public List<MonsterDropData> Drops { get; set; } = new();

        // --- Reglas de Aparición ---
        public SpawnRules Spawn { get; set; } = new();

        // --- Extensibilidad (Elite Feature) ---
        /// <summary>
        /// Datos arbitrarios para integración con otros mods (ej: Debuffs, Elementos).
        /// </summary>
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    public class SpawnRules
    {
        /// <summary>
        /// Pisos de la mina donde puede aparecer.
        /// </summary>
        public int MinMineLevel { get; set; } = -1;
        public int MaxMineLevel { get; set; } = -1;

        /// <summary>
        /// Probabilidad relativa de aparecer frente a otros monstruos.
        /// </summary>
        public double SpawnWeight { get; set; } = 1.0;

        /// <summary>
        /// Lista de nombres de ubicaciones específicas (ej: "Farm", "Forest").
        /// </summary>
        public List<string> SpecificLocations { get; set; } = new();
    }
}