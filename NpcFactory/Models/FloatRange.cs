using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    public struct FloatRange
    {
        float Min;
        float Max;

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float GetRandom()
        {
            float range = Max - Min;
            return range == 0 ? Min : Min + (float)Random.Shared.NextSingle() % range;
        }
    }
}
