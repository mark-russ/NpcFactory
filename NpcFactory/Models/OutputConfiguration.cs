using Mutagen.Bethesda.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    public class OutputConfiguration
    {
        public string? Author;
        public string? Suffix;

        public string FileName
        {
            get
            {
                var fileName = "npc_converter_generated";

                if (Suffix != null)
                {
                    fileName = $"{fileName}_{Suffix}";
                }

                return $"{fileName}.esp";
            }
        }

        public ModKey ModKey
        {
            get
            {
                return ModKey.FromNameAndExtension(FileName);
            }
        }
    }
}
