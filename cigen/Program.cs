using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace cigen
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            string command = null;
            object commandOptions = null;

            if (!Parser.Default.ParseArguments(args, options, (c, co) => {command = c; commandOptions = co;}))
            {
                Environment.Exit(Parser.DefaultExitCodeFail);
            }

            if (command == "add")
            {
                CreateCustomCulture((AddOptions) commandOptions);
                Console.WriteLine("New culture '{0}' successfully created.", ((AddOptions) commandOptions).Name);
            }
            if (command == "delete")
            {
                DeleteCustomCulture((DeleteOptions) commandOptions);
                Console.WriteLine("Culture '{0}' successfully deleted.", ((DeleteOptions) commandOptions).Name);
            }
            if (command == "list")
            {
                var cultures = GetCultures((ListOptions) commandOptions).ToList();

                foreach (var culture in cultures)
                {
                    Console.WriteLine("{0,-16}{1,-52}{2}", culture.Name, culture.EnglishName,
                        culture.CultureTypes.HasFlag(CultureTypes.UserCustomCulture) ? "custom" : "");
                }

                if (!cultures.Any())
                {
                    Console.WriteLine("No {0}cultures registered in the system.", ((ListOptions) commandOptions).Custom ? "custom " : "");
                }
            }
        }

        private static void CreateCustomCulture(AddOptions opts)
        {
            var baseCulture = new CultureInfo(opts.BaseCulture);
            var baseRegion = new RegionInfo(opts.BaseRegion);

            var builder = new CultureAndRegionInfoBuilder(opts.Name, CultureAndRegionModifiers.None);

            builder.LoadDataFromCultureInfo(baseCulture);
            builder.LoadDataFromRegionInfo(baseRegion);

            builder.CultureEnglishName = opts.EnglishName;
            builder.CultureNativeName = opts.NativeName;

            if (CultureExists(opts.Name))
            {
                CultureAndRegionInfoBuilder.Unregister(opts.Name);
            }

            builder.Register();
        }

        private static void DeleteCustomCulture(DeleteOptions opts)
        {
            if (CultureExists(opts.Name))
            {
                CultureAndRegionInfoBuilder.Unregister(opts.Name);
            }
        }

        private static IEnumerable<CultureInfo> GetCultures(ListOptions opts)
        {
            var filter = opts.Custom ? CultureTypes.UserCustomCulture : CultureTypes.AllCultures;

            return CultureInfo.GetCultures(filter);
        }

        private static bool CultureExists(string customCultureName)
        {
            return CultureInfo.GetCultures(CultureTypes.UserCustomCulture)
                .Any(ci => ci.Name == customCultureName);
        }
    }

    class AddOptions
    {
        [Option('n', "name", Required = true, HelpText = "Target culture name, eg 'en-DK'")]
        public string Name { get; set; }

        [Option('b', "base", Required = true, HelpText = "Base culture (prototype), eg 'en' or 'en-DA'")]
        public string BaseCulture { get; set; }

        [Option('r', "region", Required = true, HelpText = "Base region (prototype), eg 'da-DK'")]
        public string BaseRegion { get; set; }

        [Option("english-name", Required = true, HelpText = "English name for the target culture, eg 'English (Denmark)'")]
        public string EnglishName { get; set; }

        [Option("native-name", Required = true, HelpText = "Native name for the target culture, eg 'English (Danmark)'")]
        public string NativeName { get; set; }
    }

    class DeleteOptions
    {
        [Option('n', "name", Required = true, HelpText = "Target culture name, eg 'en-DK'")]
        public string Name { get; set; }
    }

    class ListOptions
    {
        [Option('c', "custom", HelpText = "List only custom cultures")]
        public bool Custom { get; set; }
    }

    class Options
    {
        [VerbOption("add", HelpText = "Register a new culture in Windows")]
        public AddOptions AddVerb { get; set; }

        [VerbOption("delete", HelpText = "Delete a culture in Windows")]
        public DeleteOptions DeleteVerb { get; set; }

        [VerbOption("list", HelpText = "List all cultures registered in Windows")]
        public ListOptions ListVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string command)
        {
            object opts = this;
            var intro = "\nDecide what you want to do:";
            var example = "Examples:\n\n" +
                          "  1) Register an English culture for Denmark in Windows:\n" +
                          "     cigen add -n en-DK -b en -r da-DK --english-name \"English (Denmark)\" --native-name \"English (Danmark)\"\n\n" +
                          "  2) Delete an English culture for Denmark from Windows:\n" +
                          "     cigen delete -n en-DK\n\n" +
                          "  3) List all custom cultures registered in Windows:\n" +
                          "     cigen list --custom\n";

            if (command == "add")
            {
                opts = AddVerb;
                intro = "\nTo register a new culture, specify its base cultures and display names:";
            }
            if (command == "delete")
            {
                opts = DeleteVerb;
                intro = "\nTo delete a culture, specify its name:";
            }
            if (command == "list")
            {
                opts = ListVerb;
            }

            var help = HelpText.AutoBuild(opts, current =>
                {
                    current.MaximumDisplayWidth = int.MaxValue;
                    HelpText.DefaultParsingErrorsHandler(this, current);
                }, verbsIndex: true);

            help.AddPreOptionsLine(intro);
            help.AddPostOptionsLine(example);

            return help;
        }
    }
}
