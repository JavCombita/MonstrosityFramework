namespace MonstrosityFramework.Framework.Data
{
    public class MonsterDropData
    {
        /// <summary>
        /// ID del item (ej: "337" para Iridium Bar, o "MyMod.Item" para items moddeados).
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Probabilidad de caída (0.0 a 1.0).
        /// </summary>
        public float Chance { get; set; } = 1.0f;

        /// <summary>
        /// Cantidad mínima.
        /// </summary>
        public int MinStack { get; set; } = 1;

        /// <summary>
        /// Cantidad máxima.
        /// </summary>
        public int MaxStack { get; set; } = 1;
    }
}