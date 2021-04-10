using LiteDB;
using System.Collections.Generic;

namespace Kits.API
{
    public class Kit
    {
        [BsonId] public string? Name { get; set; }
        public float Cooldown { get; set; }
        public decimal Cost { get; set; }
        public decimal Money { get; set; }
        public List<KitItem>? Items { get; set; }
    }
}