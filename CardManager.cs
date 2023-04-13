namespace Magic;

public class CardManager
{
    private IDataManager CardDataInputManager;
    private IDataManager UserCardInputManager;
    private IDataManager OutputManager;

    private List<Card> UserCards;
    private Dictionary<string, List<Card>> ScryfallData; // key: set name

    public CardManager(IDataManager dataInputManager, IDataManager inputManager, IDataManager outputManager)
    {
        CardDataInputManager  = dataInputManager;
        UserCardInputManager = inputManager;
        OutputManager = outputManager;

        UserCards = new List<Card>();
        ScryfallData = new Dictionary<string, List<Card>>();
    }

    public bool ImportUserCards(string source) 
    {
        // import TCG
        Console.WriteLine("\nImporting cards from user collection at " + source);
        var userCards = UserCardInputManager.Import(source).ToList();
        Console.WriteLine($"\nLoaded {userCards.Count} card entries");

        // collapse similar cards into single card object
        var totalCardCount = 0;
        foreach (var group in userCards.GroupBy(card => card.UniqueID()))
        {
            var uniqueCardSet = group.ToList();
            var uniqueCard = uniqueCardSet.First();

            // tally card count and foiled card count
            uniqueCard.Count = uniqueCardSet.Where(card => card.Count > 0).Sum(card => card.Count);
            uniqueCard.CountFoiled = uniqueCardSet.Where(card => card.CountFoiled > 0).Sum(card => card.CountFoiled);

            UserCards.Add(uniqueCard);
            totalCardCount += uniqueCard.Count + uniqueCard.CountFoiled;
        }
        Console.WriteLine($"Loaded {totalCardCount} total cards");
        
        return userCards.Any();
    }
    
    public bool ImportCardData(string source) 
    {
        // import Scryfall
        Console.WriteLine("\nImporting cards from data source " + source);
        var cardData = CardDataInputManager.Import(source).ToList();
        Console.WriteLine($"\nLoaded {cardData.Count} cards");
        
        // organize for search improvement
        ScryfallData = cardData.GroupBy(card => card.Set.ToUpper()).ToDictionary(g => g.Key, g => g.ToList());
        
        return cardData.Any();
    }

    public bool DownloadCardData(string dataFile)
    {
        var task = ScryfallAPIHandler.DownloadCardData(dataFile);
        task.Wait();
        return task.Result;
    }

    public bool MatchCards(string outputFile, bool downloadImages) 
    {
        Console.WriteLine("\nMatching cards...\n");

        var matchedCards = new Dictionary<string, Card>();
        var matchlessCards = new Dictionary<string, Card>();

        // match up TCG cards with Scryfall cards
        foreach (var card in UserCards)
        {
            // search for match from Scryfall data
            var setName = card.Set.ToUpper();
            var setNames = new List<string> { setName, $"{setName} TOKENS", $"{setName} PROMOS" };
            Card? match = SearchForMatch(card, setNames);

            // store data for export
            if (match != null)
            {
                var cardMatch = ((Card)match).Copy();
                cardMatch.Count = card.Count;
                cardMatch.CountFoiled = card.CountFoiled;
                
                matchedCards[cardMatch.UniqueID()] = cardMatch;
            }
            else
            {
                matchlessCards[card.UniqueID()] = card;
            }
        }

        Console.WriteLine($"\nMatched {matchedCards.Sum(entry => entry.Value.Count + entry.Value.CountFoiled)} cards; " +
                            $"missing matches for {matchlessCards.Sum(entry => entry.Value.Count + entry.Value.CountFoiled)} cards");

        // export cards to file
        var exported = ExportCards(outputFile, matchedCards, matchlessCards);

        // download images for matched cards
        if (downloadImages && !DownloadCardImages(matchedCards, outputFile))
            Console.WriteLine("\nWarning: Failed to download images");

        return exported;
    }

    private Card? SearchForMatch(Card card, List<string> sets)
    {
        foreach (var set in sets)
        {
            // find card set
            if (!ScryfallData.TryGetValue(set, out var matchedSet))
            {
                Console.WriteLine($"Found no card set '{set}'");
                continue;
            }
            
            // find card match
            var finalists = matchedSet.Where(c => card.Matches(c, ignoreSet: true)).ToList();
            if (finalists.Any())
            {
                return finalists.FirstOrDefault();
            }
            Console.WriteLine($"Found 0 candidate(s) for card '{card.Name}' in set '{set}'");
        }
        return null;
    }

    private bool ExportCards(string outputFile, Dictionary<string, Card> matchedCards, Dictionary<string, Card> matchlessCards)
    {
        // init output folder
        var outputFileInfo = new FileInfo(outputFile);
        if (!string.IsNullOrEmpty(outputFileInfo.DirectoryName))
            Directory.CreateDirectory(outputFileInfo.DirectoryName);

        // export matches
        var exportSuccess = false;
        if (matchedCards.Any())
        {
            exportSuccess = OutputManager.Export(matchedCards, outputFile);
            if (exportSuccess)
                Console.WriteLine("\nExported matched cards to " + outputFile);
        }

        // export match failures
        if (matchlessCards.Any())
        {
            var filename = outputFileInfo.Name;
            var filepath = outputFileInfo.DirectoryName ?? "";
            var failedOutputFile = Path.Combine(filepath, "failed_" + filename);

            if (OutputManager.Export(matchlessCards, failedOutputFile))
                Console.WriteLine("\nExported matchless cards to " + failedOutputFile);
        }

        return exportSuccess;
    }

    private bool DownloadCardImages(Dictionary<string, Card> cards, string outputFile)
    {
        var output = new FileInfo(outputFile);
        var path = Path.Combine(output.DirectoryName ?? "", "Images");

        var task = ScryfallAPIHandler.DownloadCardImages(cards.Values, path);
        task.Wait();
        return task.Result;
    }
}