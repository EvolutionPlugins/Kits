using Kits.Models;
using System;
using System.Collections.Generic;

namespace Kits.API
{
    [Serializable]
    public class Kit
    {
        public Kit()
        {
        }

        public string Name { get; set; }
        public List<KitItem> Items { get; set; }
        public float Cooldown { get; set; }
    }
}