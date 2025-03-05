using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using NexusMods.Paths.Trees.Traits;
using Noggog;
using Reloaded.Memory.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    class RaceOptions(IRaceGetter race)
    {
        public readonly IRaceGetter Race = race;

        private List<IHeadPartGetter>? _headParts;
        public List<IHeadPartGetter> HeadParts
        {
            get
            {
                if (_headParts == null)
                {
                    var headParts = Program.Game.LoadOrder.PriorityOrder.HeadPart().WinningContextOverrides();
                    _headParts = headParts.Where(x =>
                    {
                        x.Record.ValidRaces.TryResolve(Program.Game.LinkCache, out var i);
                        return i?.Items.Contains(Race.FormKey) ?? false;
                    }).Select(x => x.Record).ToList();
                }

                return _headParts;
            }
        }
        
        private Dictionary<Gender, List<IColorRecordGetter>>? _hairColors;
        public Dictionary<Gender, List<IColorRecordGetter>> HairColors
        {
            get
            {
                if (_hairColors == null)
                {
                    _hairColors = new()
                    {
                        { Gender.Male, new() },
                        { Gender.Female, new() }
                    };

                    Race.HeadData.Male.AvailableHairColors.ForEach(x => _hairColors[Gender.Male].Add(x.Resolve(Program.Game.LinkCache)));
                    Race.HeadData.Female.AvailableHairColors.ForEach(x => _hairColors[Gender.Female].Add(x.Resolve(Program.Game.LinkCache)));
                }

                return _hairColors;
            }
        }

        public Dictionary<Gender, List<IColorRecordGetter>> TintColors
        {
            get
            {
                return HairColors; // For now I'm just going to alias TintColors to HairColors.
            }
        }

        private Dictionary<Gender, IEnumerable<ITintGroupGetter>>? _faceTintLayers;
        public Dictionary<Gender, IEnumerable<ITintGroupGetter>> FaceTintLayers
        {
            get
            {
                if (_faceTintLayers == null)
                {
                    _faceTintLayers = new();
                    _faceTintLayers[Gender.Male] = Race.HeadData.Male.TintLayers.ToArray();
                    _faceTintLayers[Gender.Female] = Race.HeadData.Female.TintLayers.ToArray();
                }

                return _faceTintLayers;
            }
        }

        private Dictionary<Gender, IEnumerable<IFaceMorphGetter>>? _faceMorphs;
        public Dictionary<Gender, IEnumerable<IFaceMorphGetter>> FaceMorphs
        {
            get
            {
                if (_faceMorphs == null)
                {
                    _faceMorphs = new();
                    _faceMorphs[Gender.Male] = Race.HeadData.Male.FaceMorphs.ToArray();
                    _faceMorphs[Gender.Female] = Race.HeadData.Female.FaceMorphs.ToArray();
                }

                return _faceMorphs;
            }
        }

        private IEnumerable<IArmorGetter>? _skins;
        public IEnumerable<IArmorGetter> Skins
        {
            get
            {
                if (_skins == null)
                {
                    _skins = Program.Game.LoadOrder.PriorityOrder.Armor().WinningContextOverrides().Select(x => x.Record).Where(x => x.Race.Equals(Race));
                }

                return _skins;
            }
        }

        private Dictionary<Gender, WeightedList<IColorRecordGetter>> _randomHairColorPool = new();

        private Dictionary<Gender, Dictionary<HeadPart.TypeEnum, WeightedList<IHeadPartGetter>>> _randomHeadPartPool = new()
        {
            { Gender.Male, new() },
            { Gender.Female, new() }
        };

        public IColorRecordGetter GetRandomHairColor(Gender gender)
        {
            if (!_randomHairColorPool.TryGetValue(gender, out var hairColors))
            {
                hairColors = new WeightedList<IColorRecordGetter>();
                HairColors[gender].ForEach(x => hairColors.Add(x, 1));
                _randomHairColorPool[gender] = hairColors;
            }

            return hairColors.IsEmpty ? null : hairColors.GetRandomItem();
        }

        public IHeadPartGetter? GetRandomFallbackHeadpart(Gender gender, HeadPart.TypeEnum headpartType)
        {
            if (!_randomHeadPartPool[gender].TryGetValue(headpartType, out WeightedList<IHeadPartGetter>? value))
            {
                value = new WeightedList<IHeadPartGetter>();
                _randomHeadPartPool[gender][headpartType] = value;

                if (headpartType == HeadPart.TypeEnum.FacialHair || 
                    headpartType == HeadPart.TypeEnum.Eyebrows ||
                    headpartType == HeadPart.TypeEnum.Teeth ||
                    headpartType == HeadPart.TypeEnum.HeadRear) {
                    value.Add(null, 1); // These headparts will be allowed to have "None" selected.
                }

                HeadParts.Where(x => x.Type == headpartType).ForEach(x => value.Add(x, 1));
            }

            return value.IsEmpty ? null : value.GetRandomItem();
        }
    }
}
