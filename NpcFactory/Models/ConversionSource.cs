using Mutagen.Bethesda.Fallout4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    struct ConversionSource
    {
        public SearchType SearchType;
        public string EditorID;

        public ConversionSource(SearchType searchType, string editorId)
        {
            SearchType = searchType;
            EditorID = editorId;
        }
    }
}
