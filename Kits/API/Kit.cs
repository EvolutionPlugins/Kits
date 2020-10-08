using Kits.Models;
using System;
using System.Collections.Generic;

namespace Kits.API
{
    [Serializable]
    public class Kit
    {
        public string Name;
        public List<KitItem> Items;
        public float Cooldown;
    }
}