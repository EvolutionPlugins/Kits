using Kits.API;
using System;
using System.Collections.Generic;

namespace Kits.Models
{
    [Serializable]
    public class KitsData
    {
        public KitsData()
        {
            Kits = new List<Kit>();
        }

        public List<Kit> Kits { get; set; }
    }
}
