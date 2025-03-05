using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    public struct P3Range
    {
        P3Float Min;
        P3Float Max;

        public P3Range(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            Min = new P3Float(minX, minY, minZ);
            Max = new P3Float(maxX, maxY, maxZ);
        }

        private float GetClampedRandomFloat(float min, float max)
        {
            float range = max - min;
            return range == 0 ? min : min + (float)Random.Shared.NextSingle() % range;
        }

        public P3Float GetRandom()
        {
            return new P3Float(
                GetClampedRandomFloat(Min.X, Max.X),
                GetClampedRandomFloat(Min.Y, Max.Y),
                GetClampedRandomFloat(Min.Z, Max.Z)
            );
        }
    }
}
