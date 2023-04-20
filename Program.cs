using System.CommandLine;

namespace Magic;

internal class Program
{
    static string DataFile = "";
    static string InputFile = "";
    static string OutputFile = "";
    static string CorrectionFile = "";

    static bool DownloadData = false;
    static bool DownloadImages = false;

    static void Main(string[] args)
    {
        // dotnet run -- -h

        Console.WriteLine("Magic Card Manager running\n");


        // define command line usage
        var dataSourceOption = new Option<FileInfo?>(
            name: "--data",
            description: "Scryfall JSON card data file. (Used as filename if --download selected)"
        );
        var dataDownloadOption = new Option<bool>(
            name: "--download",
            description: "Download new bulk data file from Scryfall? (True if --data not provided)",
            getDefaultValue: () => false
        );
        var imageDownloadOption = new Option<bool>(
            name: "--download-images",
            description: "Download card images from Scryfall? (Stored in output folder)",
            getDefaultValue: () => false
        );
        var cardInputOption = new Option<FileInfo?>(
            name: "--input",
            description: "TCG CSV user cards file."
        );
        var correctionInputOption = new Option<FileInfo?>(
            name: "--corrections",
            description: "CSV with corrections for TCG user cards file."
        );
        var cardOutputOption = new Option<FileInfo>(
            name: "--output",
            description: "CSV output file for organized user cards.",
            getDefaultValue: () => new FileInfo("output/results.csv")
        );


        var rootCommand = new RootCommand("Magic card manager");

        rootCommand.AddOption(dataSourceOption);
        rootCommand.AddOption(dataDownloadOption);
        rootCommand.AddOption(imageDownloadOption);
        rootCommand.AddOption(cardInputOption);
        rootCommand.AddOption(correctionInputOption);
        rootCommand.AddOption(cardOutputOption);

        rootCommand.SetHandler((data, download, images, input, corrections, output) => 
            {
                if (ProcessArgs(data, download, images, input, corrections, output))
                    RunCardManagement();
            },
            dataSourceOption, dataDownloadOption, imageDownloadOption, cardInputOption, correctionInputOption, cardOutputOption
        );

        rootCommand.Invoke(args);
    }

    static void PrintWarning(string message, bool includeUsage = false)
    {
        Console.WriteLine("\nWarning: " + message);
        
        if (includeUsage)
            Console.WriteLine("Run program with -h for usage directions.");
    }

    static bool ProcessArgs(FileInfo? data, bool download, bool images, FileInfo? input, FileInfo? corrections, FileInfo output)
    {
        OutputFile = output.FullName;
        DownloadImages = images;

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

        // verify corrections
        if (corrections != null)
        {
            if (!File.Exists(corrections.FullName)) {
                PrintWarning($"Corrections file {corrections.Name} not found.", includeUsage: true);
                return false;
            }
            CorrectionFile = corrections.FullName;
        }

        // verify data source
        DownloadData = download;
        if (data == null)
        {
            DownloadData = true; // default to downloading if no data file provided
        }
        else
        {
            DataFile = data.FullName;

            // if not downloading, ensure data is valid file
            if (!DownloadData && !File.Exists(data.FullName)) {
                PrintWarning($"Data file {data.Name} not found.", includeUsage: true);
                return false;
            }
        }
        if (string.IsNullOrEmpty(DataFile))
            DataFile = ScryfallAPIHandler.DefaultFilePath;

        return true;
    }

    static void RunCardManagement() 
    {
        var cardManager = new CardManager(JSONManager.Instance, CSVManager.Instance, CSVManager.Instance);

        // import cards from user's TCG data
        if (!cardManager.ImportUserCards(InputFile, CorrectionFile)) 
        {
            PrintWarning("No user data to merge; exiting program.");
            return;
        }
        
        // import all card data from Scryfall
        if (DownloadData && !cardManager.DownloadCardData(DataFile))
        {
            PrintWarning("No source data available; exiting program.");
            return;
        }
        if (!cardManager.ImportCardData(DataFile)) 
        {
            PrintWarning("No source data to reference; exiting program.");
            return;
        }

        // match up user cards with Scryfall data; export to file
        cardManager.MatchCards(OutputFile, DownloadImages);
    }
}