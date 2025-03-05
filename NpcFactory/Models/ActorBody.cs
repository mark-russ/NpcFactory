using Mutagen.Bethesda.Fallout4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    public class ActorBody
    {
        public NpcWeight Weight = new();
        public IArmorGetter? Skin;
    }
}
