using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    class WeightedListItem<T>(T underlying, uint weight)
    {
        public T Underlying = underlying;
        public uint Weight = weight;
    }
}
