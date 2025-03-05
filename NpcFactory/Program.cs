namespace NpcFactory
{
    using Mutagen.Bethesda;
    using Mutagen.Bethesda.Environments;
    using Mutagen.Bethesda.Fallout4;
    using Mutagen.Bethesda.Plugins;
    using Noggog;
    using NpcFactory.Models;
    using NpcFactory.Services;
    using System.Collections.Generic;
    using System.Reflection;
    using static Mutagen.Bethesda.Fallout4.Package;
    using System.Text;

    static internal class Program
    {
        private static IGameEnvironment<IFallout4Mod, IFallout4ModGetter>? _game;
        public static IGameEnvironment<IFallout4Mod, IFallout4ModGetter> Game
        {
            get 
            {
                _game ??= GameEnvironment.Typical.Fallout4(Fallout4Release.Fallout4);
                return _game;
            }
        }

        private static Dictionary<string, RaceOptions>? _races;
        public static Dictionary<string, RaceOptions> Races
        {
            get
            {
                _races ??= Game.LoadOrder.PriorityOrder.Race().WinningContextOverrides()
                    .ToDictionary(x => x.Record.EditorID!, x => new RaceOptions(x.Record));

                return _races;
            }
        }

        private static Dictionary<string, INpcGetter>? _actors;
        public static Dictionary<string, INpcGetter> Actors
        {
            get
            {
                _actors ??= Game.LoadOrder.PriorityOrder.Npc().WinningContextOverrides().ToDictionary(x => x.Record.EditorID!, x => x.Record);
                return _actors;
            }
        }

        private static List<string> MessagesAlreadyShown = new List<string>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static Fallout4Mod GeneratedMod;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        
        private static uint ActorsConverted;

        public static string? ProcessStartArgs(string[] args)
        {
            string? planFilePath = null;

            for (var i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg[0] != '-')
                {
                    planFilePath = arg;
                    continue;
                }

                if (args[i] == "-seed")
                {
                    var seedToBytes = BitConverter.ToInt32(Encoding.UTF8.GetBytes(args[++i]));
                    Program.Random = new Random(seedToBytes);
                }

                else
                {
                    if (args[i] == "-races")
                    {
                        Races.Keys.Order().ForEach(editorId => Console.WriteLine(editorId));
                        return null;
                    }

                    if (args[i] == "-headparts")
                    {
                        var race = Races.GetValueOrDefault(args[++i]);

                        if (race == null)
                        {
                            Console.Error.WriteLine("Race not found...");
                            return null;
                        }

                        race.HeadParts.Select(x => x.EditorID).Order().ForEach(x => Console.WriteLine(x));
                        return null;
                    }
                }
            }

            while (!File.Exists(planFilePath))
            {
                Console.WriteLine("Please specify a plan file. (it will be JSON):");
                planFilePath = Console.ReadLine();
            }

            return planFilePath!;
        }

        public static Random Random = Random.Shared;

        private static string OutputPath = "";

        static void Main(string[] args)
        {
            var planFilePath = ProcessStartArgs(args);

            if (planFilePath == null)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }
            else
            {
                Console.WriteLine($"Input file: {planFilePath}");
            }

            ConversionService.ConfigurationScanned += ConversionService_ConfigurationScanned;
            ConversionService.ActorConverting += ConversionService_ActorConverting;
            ConversionService.ActorConverted += ConversionService_ActorConverted;
            ConversionService.LookupFailed += ConversionService_LookupFailed;
            ConversionService.Finished += ConversionService_Finished;
            ConversionService.ExecutePlan(planFilePath);
        }

        private static void ConversionService_ConfigurationScanned(OutputConfiguration config)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            Program.OutputPath = Path.GetFullPath(config.FileName);

            GeneratedMod = new Fallout4Mod(config.ModKey, Fallout4Release.Fallout4);
            GeneratedMod.ModHeader.Author = config.Author;
            GeneratedMod.ModHeader.Description = $"Plugin generated via {assemblyName.Name} v{assemblyName.Version!.Major}.{assemblyName.Version!.Minor}";

            if (File.Exists(Program.OutputPath))
            {
                File.Delete(Program.OutputPath);
            }
        }

        private static void ConversionService_ActorConverting(INpc actor)
        {
            Console.WriteLine($"Converting {actor.EditorID}...");
        }

        private static void ConversionService_ActorConverted(INpc modifiedActor)
        {
            GeneratedMod.Npcs.GetOrAddAsOverride(modifiedActor);
            ActorsConverted++;
        }

        private static void ConversionService_Finished()
        {
            Console.WriteLine($"Finished! Converted {ActorsConverted} actor(s). Writing plugin...");

            using (var outputStream = File.Open(Program.OutputPath, FileMode.Create))
            {
                GeneratedMod.WriteToBinary(outputStream);
            }

            Console.WriteLine($"Plugin written to: {Program.OutputPath}");
            Console.WriteLine($"Press any key to exit.");
            Console.ReadKey();
        }

        private static void ConversionService_LookupFailed(string message)
        {
            if (!MessagesAlreadyShown.Contains(message))
            {
                Console.WriteLine($"{message} It will not be included in the conversion.");
                MessagesAlreadyShown.Add(message);
            }
        }
    }
}
