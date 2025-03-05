using Mutagen.Bethesda.Fallout4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    struct ConversionPlan
    {
        public List<ConversionSource> Sources;
        public WeightedList<ConversionDestination> Destinations;

        public ConversionPlan()
        {
            Sources = new List<ConversionSource>();
            Destinations = new WeightedList<ConversionDestination>();
        }
    }
}
