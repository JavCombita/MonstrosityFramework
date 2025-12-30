namespace MonstrosityFramework.Framework.Data
{
    public class MonsterDropData
    {
        public string ItemId { get; set; } // Soporta Qualified Item IDs de la 1.6 (ej: "(O)337")
        public float Chance { get; set; } = 1.0f;
        public int MinStack { get; set; } = 1;
        public int MaxStack { get; set; } = 1;
    }
}