using Mutagen.Bethesda.Fallout4;

namespace NpcFactory.Models
{
    class ConversionDestination
    {
        public required IRaceGetter Race;
        public Dictionary<Gender, Dictionary<HeadPart.TypeEnum, WeightedList<IHeadPartGetter>>> HeadParts = new();
        public Dictionary<Gender, WeightedList<ActorBody>> Bodies = new();
        public Dictionary<Gender, WeightedList<IColorRecordGetter>> HairColors = new();
        public Dictionary<Gender, Dictionary<IFaceMorphGetter, ActorMorph>> FaceMorphs = new();
        public Dictionary<Gender, Dictionary<ITintGroupGetter, RandomizedList<WeightedList<NpcFaceTintingLayer>>>> TintSlots = new();
    }
}
