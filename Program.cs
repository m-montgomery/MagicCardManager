using System.CommandLine;

namespace Magic
{
    internal class Program
    {
        static string DataFile = "";
        static string InputFile = "";
        static string OutputFile = "";

        static void Main(string[] args)
        {
            // dotnet run -- -h

            Console.WriteLine("Magic Card Manager running\n");


            // define command line usage
            var dataSourceOption = new Option<FileInfo?>(
                name: "--data",
                description: "Data file for Magic cards (Scryfall JSON)"
            );
            var cardInputOption = new Option<FileInfo?>(
                name: "--input",
                description: "Source file for user cards (TCG CSV)"
            );
            var cardOutputOption = new Option<FileInfo>(
                name: "--output",
                description: "Output file for organized user cards (CSV)",
                getDefaultValue: () => new FileInfo("results.csv")
            );


            var rootCommand = new RootCommand("Magic card manager");

            rootCommand.AddOption(dataSourceOption);
            rootCommand.AddOption(cardInputOption);
            rootCommand.AddOption(cardOutputOption);

            rootCommand.SetHandler((data, input, output) => 
                {
                    if (ProcessArgs(data, input, output))
                        RunCardManagement();
                },
                dataSourceOption, cardInputOption, cardOutputOption
            );

            rootCommand.Invoke(args);
        }

        static void PrintWarning(string message, bool includeUsage = false)
        {
            Console.WriteLine("Warning: " + message);
            
            if (includeUsage)
                Console.WriteLine("Run program with -h for usage directions.");
        }

        static bool ProcessArgs(FileInfo? data, FileInfo? input, FileInfo output)
        {
            // verify input source
            if (input == null)
            {
                PrintWarning("Input file for user cards is a required argument.", includeUsage: true);
                return false;
            }
            if (!File.Exists(input.FullName)) {
                PrintWarning($"Input file {input.Name} not found.", includeUsage: true);
                return false;
            }
            InputFile = input.FullName;


            // verify data source
            if (data == null)
            {
                PrintWarning("Data file for cards is a required argument.", includeUsage: true);
                return false;
            }
            if (!File.Exists(data.FullName)) 
            {
                PrintWarning($"Data file {data.Name} not found.", includeUsage: true);
                return false;
            }
            DataFile = data.FullName;


            OutputFile = output.FullName;

            return true;
        }

        static void RunCardManagement() 
        {
            var manager = new CardManager(JSONManager.Instance, CSVManager.Instance, CSVManager.Instance);

            // import cards from user's TCG data
            if (!manager.ImportUserCards(InputFile)) 
            {
                PrintWarning("No user data to merge; exiting program.");
                return;
            }
            
            // import all card data from Scryfall
            if (!manager.ImportCardData(DataFile)) 
            {
                PrintWarning("No source data to reference; exiting program.");
                return;
            }

            // match up user cards with Scryfall data; export to file
            manager.MatchCards(OutputFile);
        }
    }
}