using OpenMod.Extensions.Games.Abstractions.Items;
using System.Collections.Generic;

namespace Kits.API
{
    public class Kit
    {
        public string Name;
        public List<(string, IItemState)> Items;
        public float Cooldown;
    }
}