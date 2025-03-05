using Mutagen.Bethesda;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Strings;
using Noggog;
using NpcFactory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static Mutagen.Bethesda.Fallout4.Fallout4ModHeader;

namespace NpcFactory.Services
{
    static class ConversionService
    {
        public static event Action<OutputConfiguration>? ConfigurationScanned;
        public static event Action<INpc>? ActorConverting;
        public static event Action<INpc>? ActorConverted;
        public static event Action? Finished;
        public static event Action<string>? LookupFailed;

        private static List<ConversionPlan> MakePlans(string filePath)
        {
            var goals = new List<ConversionPlan>();

            var file = CompileFile(filePath).AsObject().ToDictionary();

            var outputConfig = new OutputConfiguration();
            if (file.ContainsKey("Plugin"))
            {
                var pluginConfig = file["Plugin"]?.AsObject();
                outputConfig.Author = (string)pluginConfig["Author"];
                outputConfig.Suffix = (string)pluginConfig["Suffix"];
            }

            ConfigurationScanned?.Invoke(outputConfig);

            var inputGoals = file["Goals"].AsArray();

            foreach (var rawGoal in inputGoals)
            {
                var goal = new ConversionPlan();

                foreach (var source in rawGoal!["From"]!.AsArray())
                {
                    SearchType search = Enum.Parse<SearchType>(source!["Search"]!.GetValue<string>());
                    var editorId = source!["For"]!.GetValue<string>();
                    goal.Sources.Add(new ConversionSource(search, editorId));
                }

                foreach (var destination in rawGoal!["To"]!.AsArray())
                {
                    var raceEditorId = (string)destination!["Race"]!;
                    var destProbabilityWeight = (uint)destination["ProbabilityWeight"]!;

                    var race = Program.Races.GetOrDefault(raceEditorId);
                    if (race == null)
                    {
                        LookupFailed?.Invoke($"Could not locate race \"{raceEditorId}\".");
                        continue;
                    }

                    var conversionDest = new ConversionDestination()
                    {
                        Race = race.Race
                    };

                    var headParts = destination["Head"];
                    if (headParts != null)
                    {
                        foreach (var genderGroup in headParts.AsObject())
                        {
                            ProcessForGender(genderGroup.Key, conversionDest.HeadParts, (headParts, gender) =>
                            {
                                foreach (var partGroup in genderGroup.Value.AsObject())
                                {
                                    var typeEnum = Enum.Parse<HeadPart.TypeEnum>(partGroup.Key);

                                    if (!headParts.TryGetValue(typeEnum, out var headPartItems))
                                    {
                                        headPartItems = new WeightedList<IHeadPartGetter>();
                                        headParts[typeEnum] = headPartItems;
                                    }

                                    foreach (var partDescription in partGroup.Value.AsArray())
                                    {
                                        var partEditorId = (string)partDescription["Name"]!;
                                        var headPart = partEditorId == null ? null : Program.Races[race.Race.EditorID!].HeadParts.FirstOrDefault(x => x.EditorID == partEditorId);

                                        if (headPart == null && partEditorId != null)
                                        {
                                            LookupFailed?.Invoke($"Unknown HeadPart \"{partEditorId}\".");
                                        }
                                        else
                                        {
                                            var probabilityWeight = (uint)partDescription["ProbabilityWeight"]!;
                                            headPartItems.Add(headPart, probabilityWeight);
                                        }
                                    }
                                }
                            });
                        }
                    }

                    var bodies = destination["Body"];
                    if (bodies != null)
                    {
                        foreach (var genderGroup in bodies.AsObject())
                        {
                            ProcessForGender(genderGroup.Key, conversionDest.Bodies, (bodies, gender) =>
                            {
                                foreach (var partGroup in genderGroup.Value.AsArray())
                                {
                                    if (partGroup?["Weight"] == null)
                                    {
                                        continue;
                                    }

                                    var skinEditorId = (string)partGroup?["Skin"];
                                    IArmorGetter? skin = null;

                                    if (skinEditorId != null)
                                    {
                                        skin = race.Skins.FirstOrDefault(x => x.EditorID == skinEditorId);

                                        if (skin == null)
                                        {
                                            LookupFailed?.Invoke($"Unknown skin \"{skinEditorId}\".");
                                        }
                                    }

                                    var body = new ActorBody()
                                    {
                                        Weight = partGroup["Weight"].Deserialize<NpcWeight>(),
                                        Skin = skin
                                    };

                                    bodies.Add(body, (uint)partGroup["ProbabilityWeight"]);
                                }
                            });
                        }
                    }

                    var hairColors = destination["HairColors"];
                    if (hairColors != null)
                    {
                        foreach (var genderGroup in hairColors.AsObject())
                        {
                            ProcessForGender(genderGroup.Key, conversionDest.HairColors, (hairColors, gender) =>
                            {
                                foreach (var partGroup in genderGroup.Value.AsObject())
                                {
                                    var hairColorEditorId = partGroup.Key;
                                    var hairColorProbabilityWeight = (uint)partGroup.Value;

                                    var hairColorRecord = race.HairColors[gender].FirstOrDefault(x => x.EditorID == hairColorEditorId);
                                    if (hairColorRecord == null)
                                    {
                                        LookupFailed?.Invoke($"Failed to find hair color {hairColorEditorId}.");
                                        continue;
                                    }

                                    hairColors.Add(hairColorRecord, hairColorProbabilityWeight);
                                }
                            });
                        }
                    }

                    var tints = destination["TintLayers"];
                    if (tints != null)
                    {
                        foreach (var genderGroup in tints.AsObject())
                        {
                            ProcessForGender(genderGroup.Key, conversionDest.TintSlots, (tintSlots, gender) =>
                            {
                                foreach (var tintLayer in genderGroup.Value.AsObject())
                                {
                                    var tintGroup = race.FaceTintLayers[gender].FirstOrDefault(x => x.Name.Lookup(Language.English) == tintLayer.Key);
                                    if (tintGroup == null)
                                    {
                                        LookupFailed?.Invoke($"Failed to locate tint group \"{tintLayer.Key}\".");
                                        continue;
                                    }

                                    if (!tintSlots.TryGetValue(tintGroup, out var tintSlotGroup))
                                    {
                                        tintSlotGroup = new();

                                        //tintGroup = Face Paint
                                        tintSlots[tintGroup] = tintSlotGroup;
                                    }

                                    foreach (var tintOptionTargetConfig in tintLayer.Value.AsObject())
                                    {
                                        var tintName = tintOptionTargetConfig.Key;
                                        var tintProperties = tintOptionTargetConfig.Value.AsObject();
                                        var tintChance = tintProperties.GetOrDefault<uint>("Chance");
                                        var tintOptions = tintProperties.GetOrDefault<JsonNode>("Options")?.AsArray();

                                        var weightedList = new WeightedList<NpcFaceTintingLayer>();
                                        tintSlotGroup.Add(weightedList, tintChance);

                                        if (tintOptions == null)
                                        {
                                            continue;
                                        }

                                        // GridIron
                                        var tintOption = tintGroup.Options.FirstOrDefault(x => x.Name.Lookup(Language.English) == tintName);

                                        // Male > Face Paint > Grid Iron > Some Color

                                        //tintSlotGroup = new RandomizedList<NpcFaceTintingLayer>();

                                        // Parse colors.
                                        foreach (var tintOptionConfig in tintOptions)
                                        {
                                            var tintOptionConfigProperties = tintOptionConfig.AsObject();
                                            var tintOptionValue = tintOptionConfigProperties.GetOrDefault<float>("Value");
                                            var tintOptionWeight = tintOptionConfigProperties.GetOrDefault<uint>("ProbabilityWeight");

                                            var colorParams = tintOptionConfig["Color"]?.AsArray().Select(x => (int)x).ToArray();
                                            System.Drawing.Color? tintColor = colorParams == null ? null : System.Drawing.Color.FromArgb(colorParams[0], colorParams[1], colorParams[2]);

                                            if (tintOption == null)
                                            {
                                                LookupFailed?.Invoke($"Failed to locate tint option \"{tintName}\" in group \"{tintGroup.Name}\".");
                                                continue;
                                            }

                                            var actorTintLayer = new NpcFaceTintingLayer
                                            {
                                                Index = tintOption.Index,
                                                Value = tintOptionValue,
                                                DataType = NpcFaceTintingLayer.Type.Value
                                            };

                                            if (tintColor != null)
                                            {
                                                actorTintLayer.Color = (System.Drawing.Color)tintColor;
                                                actorTintLayer.DataType = NpcFaceTintingLayer.Type.ValueAndColor;
                                            }

                                            weightedList.Add(actorTintLayer, tintOptionWeight);
                                            //tintSlotGroup.Add(actorTintLayer, tintChance);
                                        }
                                    }
                                }
                            });
                        }
                    }

                    var faceMorphs = destination["FaceMorphs"];
                    if (faceMorphs != null)
                    {
                        foreach (var genderGroup in faceMorphs.AsObject())
                        {
                            ProcessForGender(genderGroup.Key, conversionDest.FaceMorphs, (faceMorphs, gender) =>
                            {
                                foreach (var faceMorphConfig in genderGroup.Value.AsObject())
                                {
                                    // The replace call is to fix "Jowls -  Lower" (two spaces)
                                    var faceMorph = race.FaceMorphs[gender].FirstOrDefault(x => x.Name.Lookup(Language.English).Replace("  ", " ") == faceMorphConfig.Key);

                                    if (faceMorph == null)
                                    {
                                        LookupFailed?.Invoke($"Unknown face morph \"{faceMorphConfig.Key}\".");
                                        continue;
                                    }

                                    var faceMorphAttributes = faceMorphConfig.Value.AsObject();

                                    var posMin = faceMorphAttributes["Position"]["Min"].AsArray().Select(x => (float)x).ToArray();
                                    var posMax = faceMorphAttributes["Position"]["Max"].AsArray().Select(x => (float)x).ToArray();
                                    var rotMin = faceMorphAttributes["Rotation"]["Min"].AsArray().Select(x => (float)x).ToArray();
                                    var rotMax = faceMorphAttributes["Rotation"]["Max"].AsArray().Select(x => (float)x).ToArray();
                                    var scaleMin = (float)faceMorphAttributes["Scale"]["Min"];
                                    var scaleMax = (float)faceMorphAttributes["Scale"]["Max"];

                                    var actorFaceMorph = new ActorMorph();

                                    actorFaceMorph.Position = new P3Range(
                                        posMin[0], posMin[1], posMin[2],
                                        posMax[0], posMax[1], posMax[2]
                                    );

                                    actorFaceMorph.Rotation = new P3Range(
                                        rotMin[0], rotMin[1], rotMin[2],
                                        rotMax[0], rotMax[1], rotMax[2]
                                    );

                                    actorFaceMorph.Scale = new FloatRange(scaleMin, scaleMax);

                                    faceMorphs[faceMorph] = actorFaceMorph;
                                }
                            });
                        }
                    }

                    goal.Destinations.Add(conversionDest, destProbabilityWeight);
                }

                if (!goal.Destinations.IsEmpty)
                {
                    goals.Add(goal);
                }
            }

            return goals;
        }

        private static T? GetOrDefault<T>(this JsonObject node, string key, T? defaultValue = default)
        {
            if (node.ContainsKey(key)) {
                return node[key].Deserialize<T>();
            }

            return defaultValue;
        }

        private static JsonNode CompileFile(string filePath)
        {
            var fileContents = File.ReadAllText(filePath);
            var fileJson = JsonNode.Parse(fileContents, new(), new() { CommentHandling = JsonCommentHandling.Skip })!.AsObject().ToDictionary();

            if (fileJson!.ToDictionary().TryGetValue("Variables", out var variables))
            {
                foreach (var variableObject in variables.AsObject())
                {
                    fileContents = fileContents.Replace($"\"${variableObject.Key}\"", variableObject.Value.ToString());
                }
            }

            return JsonNode.Parse(fileContents);
        }

        private static void ProcessForGender<T>(string genderRaw, IDictionary<Gender, T> genderedDictionary, Action<T, Gender> processFunc) where T : new()
        {
            if (genderRaw == "Unisex")
            {
                foreach (var gender in Enum.GetValues<Gender>())
                {
                    if (!genderedDictionary.TryGetValue(gender, out var genderedDictionaryItem))
                    {
                        genderedDictionaryItem = new T();
                        genderedDictionary[gender] = genderedDictionaryItem;
                    }
                    
                    processFunc(genderedDictionaryItem, gender);
                }
            }
            else
            {
                var gender = Enum.Parse<Gender>(genderRaw);

                if (!genderedDictionary.TryGetValue(gender, out var genderedDictionaryItem))
                {
                    genderedDictionaryItem = new T();
                    genderedDictionary[gender] = genderedDictionaryItem;
                }

                processFunc(genderedDictionaryItem, gender);
            }
        }

        public static void ExecutePlan(string filePath)
        {
            var plans = MakePlans(filePath);
            var actorBlacklist = plans.SelectMany(x => x.Sources.Where(y => y.SearchType == SearchType.Actors).Select(actor => actor.EditorID));

            foreach (var plan in plans)
            {
                foreach (var source in plan.Sources)
                {
                    if (source.SearchType == SearchType.Races)
                    {
                        Program.Actors.Values
                            .Where(actor => actor.Race.Equals(Program.Races[source.EditorID].Race) && !actorBlacklist.Contains(actor.EditorID))
                            .ForEach(x => Convert(plan.Destinations, x));
                    }
                    else if (source.SearchType == SearchType.Actors)
                    {
                        var actor = Program.Actors.GetValueOrDefault(source.EditorID);

                        if (actor == null)
                        {
                            LookupFailed?.Invoke($"Could not locate actor \"{source.EditorID}\".");
                            continue;
                        }

                        Convert(plan.Destinations, actor);
                    }
                }
            }

            Finished?.Invoke();
        }

        private static void Convert(WeightedList<ConversionDestination> destinations, INpcGetter npc)
        {
            var destination = destinations.GetRandomItem();

            var generated = npc.DeepCopy();
            ActorConverting?.Invoke(generated);
            generated.HeadParts.Clear();

            var actorGender = npc.Flags.HasFlag(Npc.Flag.Female) ? Gender.Female : Gender.Male;
            var raceData = Program.Races[destination.Race.EditorID];

            if (destination.HeadParts.ContainsKey(actorGender))
            {
                foreach (var headPartGroup in destination.HeadParts[actorGender])
                {
                    if (!headPartGroup.Value.IsEmpty)
                    {
                        var randomHeadPartOfThisCategory = headPartGroup.Value.GetRandomItem();

                        if (randomHeadPartOfThisCategory != null)
                        {
                            Console.WriteLine($"\t{headPartGroup.Key}: {randomHeadPartOfThisCategory.EditorID}");
                            generated.HeadParts.Add(randomHeadPartOfThisCategory);
                        }
                    }
                }
            }
            else
            {
                foreach (var item in Enum.GetValues<HeadPart.TypeEnum>())
                {
                    var headPart = raceData.GetRandomFallbackHeadpart(actorGender, item);

                    if (headPart != null)
                    {
                        generated.HeadParts.Add(headPart);
                    }
                }
            }

            {
                generated.Skin.Clear();

                if (destination.Bodies.Count > 0)
                {
                    var bodies = destination.Bodies.GetValueOrDefault(actorGender);

                    var body = bodies == null || bodies.IsEmpty ? null : bodies.GetRandomItem();

                    if (body != null)
                    {
                        generated.Weight = new()
                        {
                            Thin = body.Weight.Thin,
                            Fat = body.Weight.Fat,
                            Muscular = body.Weight.Muscular,
                        };

                        if (body.Skin != null)
                        {
                            Console.WriteLine($"\tSkin: {body.Skin.EditorID}");
                            generated.Skin.SetTo(body.Skin);
                        }

                        Console.WriteLine($"\tBody Weight: {{{generated.Weight.Thin}, {generated.Weight.Muscular}, {generated.Weight.Fat}}}");
                    }
                }
            }

            {   // HairColor
                generated.HairColor.Clear();
                generated.FacialHairColor.Clear();
                generated.FacialMorphIntensity = 1.0f;

                if (destination.HairColors.Count > 0)
                {
                    if (destination.HairColors.TryGetValue(actorGender, out var hairColor))
                    {
                        generated.HairColor.SetTo(hairColor.GetRandomItem());
                    }
                }

                if (generated.HairColor.IsNull)
                {
                    var randomColor = raceData.GetRandomHairColor(actorGender);
                    generated.HairColor.SetTo(randomColor);
                    generated.FacialHairColor.SetTo(randomColor);
                }
            }

            {   // FaceTintingLayers
                generated.FaceTintingLayers.Clear();

                if (destination.TintSlots.ContainsKey(actorGender))
                {
                    foreach (var tintLayer in destination.TintSlots[actorGender])
                    {
                        tintLayer.Value.GetRandomItems().Select(x => x.GetRandomItem()).ForEach(x => {
                            Console.WriteLine($"\tFace Tint: {tintLayer.Key.Name.String}, {x.Color}");
                            generated.FaceTintingLayers.Add(x);
                        });
                    }
                }
            }

            {   // FaceMorphs
                generated.FaceMorphs.Clear();
                var headData = actorGender == Gender.Male ? raceData.Race.HeadData.Male : raceData.Race.HeadData.Female;
                foreach (var faceMorph in headData.FaceMorphs)
                {
                    var faceMorphDetails = destination.FaceMorphs.GetValueOrDefault(actorGender)?.GetValueOrDefault(faceMorph);

                    if (faceMorphDetails == null)
                    {
                        continue;
                    }

                    generated.FaceMorphs.Add(new()
                    {
                        Index = faceMorph.Index,
                        Position = faceMorphDetails.Position.GetRandom(),
                        Rotation = faceMorphDetails.Rotation.GetRandom(),
                        Scale = faceMorphDetails.Scale.GetRandom()
                    });
                }
            }

            generated.HeadTexture.Clear();
            generated.Morphs.Clear();
            generated.Race.SetTo(destination.Race);

            ActorConverted?.Invoke(generated);
        }
    }
}
