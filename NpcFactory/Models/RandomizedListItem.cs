using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    class RandomizedListItem<T>(T underlying, uint chance)
    {
        public T Underlying = underlying;
        public uint Chance = chance;
    }
}
