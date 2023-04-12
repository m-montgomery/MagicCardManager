namespace Magic;

public class CardManager
{
    IDataManager CardDataInputManager;
    IDataManager UserCardInputManager;
    IDataManager OutputManager;

    Dictionary<string, List<Card>> UserCards;    // key: card set name + card name + card variant
    Dictionary<string, List<Card>> ScryfallData; // key: set name

    public CardManager(IDataManager dataInputManager, IDataManager inputManager, IDataManager outputManager)
    {
        CardDataInputManager  = dataInputManager;
        UserCardInputManager = inputManager;
        OutputManager = outputManager;

        UserCards = new Dictionary<string, List<Card>>();
        ScryfallData = new Dictionary<string, List<Card>>();
    }

    public bool ImportUserCards(string source) 
    {
        // import TCG
        Console.WriteLine("\nImporting cards from user collection at " + source);
        var userCards = UserCardInputManager.Import(source).ToList();
        Console.WriteLine($"\nLoaded {userCards.Count} cards");

        // organize for processing improvement
        UserCards = userCards.GroupBy(card => card.UniqueID()).ToDictionary(g => g.Key, g => g.ToList());
        
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

    public bool MatchCards(string outputFile) 
    {
        Console.WriteLine("\nMatching cards...\n");

        var matchedCards = new Dictionary<string, List<Card>>();
        var matchlessCards = new Dictionary<string, List<Card>>();

        // match up TCG cards with Scryfall cards
        foreach (var uniqueCardSet in UserCards)
        {
            // search for match from Scryfall data
            var cardTemplate = uniqueCardSet.Value.First();
            var setName = cardTemplate.Set.ToUpper();
            var setNames = new List<string> { setName, $"{setName} TOKENS", $"{setName} PROMOS" };
            Card? match = SearchForMatch(cardTemplate, setNames);

            // store data for export
            if (match != null)
            {
                var cardMatch = ((Card)match).Copy();
                matchedCards[cardMatch.UniqueID()] = GenerateMatches(cardMatch, uniqueCardSet.Value);
            }
            else
            {
                matchlessCards[uniqueCardSet.Key] = uniqueCardSet.Value.Select(card => card.Copy()).ToList();
            }
        }

        Console.WriteLine($"\nMatched {matchedCards.Sum(entry => entry.Value.Count)} cards; " +
                            $"missing matches for {matchlessCards.Sum(entry => entry.Value.Count)} cards");

        return ExportCards(outputFile, matchedCards, matchlessCards);
    }

    private List<Card> GenerateMatches(Card cardMatch, List<Card> uniqueCardSet)
    {
        var cardMatches = new List<Card>();

        // separate regular from foiled
        var cardMatchFoiled = cardMatch.Copy();
        cardMatch.Foiled = false;
        cardMatchFoiled.Foiled = true;
        var foiledCount = uniqueCardSet.Where(card => card.Foiled).Count();

        for (var _ = 0; _ < uniqueCardSet.Count - foiledCount; _++)
        {
            cardMatches.Add(cardMatch);
        }

        for (var _ = 0; _ < foiledCount; _++)
        {
            cardMatches.Add(cardMatchFoiled);
        }

        return cardMatches;
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

    private bool ExportCards(string outputFile, Dictionary<string, List<Card>> matchedCards, Dictionary<string, List<Card>> matchlessCards)
    {
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
            var filename = Path.GetFileName(outputFile);
            var filepath = Path.GetDirectoryName(outputFile) ?? "";
            var failedOutputFile = Path.Combine(filepath, "failed_" + filename);

            if (OutputManager.Export(matchlessCards, failedOutputFile))
                Console.WriteLine("\nExported matchless cards to " + failedOutputFile);
        }

        return exportSuccess;
    }
}