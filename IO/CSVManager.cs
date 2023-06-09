using System.Text;
using System.Text.RegularExpressions;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace Magic;

public class CSVManager : IDataManager 
{
    public static CSVManager Instance { get; } = new CSVManager();
    
    public static readonly string CardOutputHeaders = "Set,Num,#,# Foil,Name,Rarity,Mana Cost,Type Line";

    private readonly string TCGNameRegex = @"([a-zA-Z\-/', ]+[a-zA-Z]-?)( ?\([^)]+\))?( - Full Art)?( - \[Foil\])?";
    private readonly string TCGChecklistRegex = @"Check ?[lL]ist Card - (\([^)]+\))? ?([0-9a-zA-Z\-/', ]+[0-9a-zA-Z])";
    private readonly string TCGVariantRegex = @"\(([^)]+)\)";


    private CSVManager() {}

    // TCG's basic CSV format
    internal class TCGCard
    {
        public int Count {get; set;}
        public string Name {get; set;} = "";
        public string Set {get; set;} = "";
    }

    // mapping for CSV parser
    internal class TCGCardMap : CsvMapping<TCGCard>
    {
        public TCGCardMap() : base()
        {
            MapProperty(0, x => x.Count);
            MapProperty(3, x => x.Name);
            MapProperty(5, x => x.Set);
        }
    }
    internal class TCGCardCorrectionMap : CsvMapping<CardCorrection>
    {
        public TCGCardCorrectionMap() : base()
        {
            MapProperty(0, x => x.Name);
            MapProperty(1, x => x.Set);
            MapProperty(2, x => x.Variant);
            MapProperty(3, x => x.CorrectedName);
            MapProperty(4, x => x.CorrectedSet);
            MapProperty(5, x => x.CorrectedVariant);
        }
    }

    public ICollection<Card> Import(string source) 
    {
        try
        {
            // read entries from TCG CSV with preliminary mapping
            var parserOptions = new CsvParserOptions(true, ',');
            var parser = new CsvParser<TCGCard>(parserOptions, new TCGCardMap());

            var parsedResults = parser.ReadFromFile(source, Encoding.UTF8);

            // convert CSV entries to Card objects
            var cards = parsedResults
                .Where(c => c.IsValid && c.Result.Count > 0)
                .Select(c => ProcessCard(c.Result))
                .ToList();

            return cards;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to import from CSV");
            Console.WriteLine(e);
            return new List<Card>();
        }
    }

    private Card ProcessCard(TCGCard entry)
    {
        var name = entry.Name.Replace("Token", "").Trim(); // ignore 'Token'
        var set = entry.Set.Trim();
        var variant = "";
        var foiled = false;
        var count = entry.Count;
        var foiledCount = 0;

        // checklist card special name case
        var match = Regex.Match(name, TCGChecklistRegex);
        if (match.Success)
        {
            name = match.Groups[2].Value + " Checklist";

            var variantMatch = Regex.Match(match.Groups[1].Value, TCGVariantRegex);
            if (variantMatch.Success)
                variant = variantMatch.Groups[1].Value;
        }
        else
        {
            // parse name from TCG
            match = Regex.Match(name, TCGNameRegex);
            if (match.Success)
            {
                name = match.Groups[1].Value;
                foiled = match.Groups[4].Success;
                
                // parse variant / set name / set ID (TCG is inconsistent on this field)
                var variantMatch = Regex.Match(match.Groups[2].Value, TCGVariantRegex);
                if (variantMatch.Success)
                    variant = variantMatch.Groups[1].Value;
            }
        }

        // track foiled cards as separate count
        if (foiled)
        {
            count = 0;
            foiledCount = entry.Count;
        }

        return new Card()
        {
            Name = name,
            Set = set,
            Variant = variant,
            Count = count,
            CountFoiled = foiledCount,
        };
    }

    public ICollection<CardCorrection> ImportCorrections(string source)
    {
        try
        {
            // read entries from CSV
            var parserOptions = new CsvParserOptions(true, ',');
            var parser = new CsvParser<CardCorrection>(parserOptions, new TCGCardCorrectionMap());

            var parsedResults = parser.ReadFromFile(source, Encoding.UTF8);

            // convert CSV entries to CardCorrection objects
            var corrections = parsedResults
                .Where(c => c.IsValid)
                .Select(c => c.Result)
                .ToList();

            ProcessCorrections(corrections);

            return corrections;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to import corrections from CSV");
            Console.WriteLine(e);
            return new List<CardCorrection>();
        }
    }

    private void ProcessCorrections(ICollection<CardCorrection> corrections)
    {
        foreach (var correction in corrections)
        {
            if (string.IsNullOrEmpty(correction.CorrectedName))
                correction.CorrectedName = correction.Name;
            if (string.IsNullOrEmpty(correction.CorrectedSet))
                correction.CorrectedSet = correction.Set;
        }
    }

    public bool Export(IDictionary<string, Card> cards, string destination) 
    {
        try 
        {
            var lines = cards
                .OrderBy(c => c.Key)
                .Select(c => CardToCSV(c.Value))
                .Prepend(CardOutputHeaders);
            
            File.WriteAllLines(destination, lines);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to export to CSV");
            Console.WriteLine(e);
            return false;
        }
    }

    private string CardToCSV(Card card)
    {
        var set = card.SetCode == "" ? card.Set : card.SetCode; // fall back on set name for matchless cards
        var setID = card.SetID + card.Variant;
        var rarity = card.Rarity.ToString().Substring(0, 1);

        return $"{set},{setID},{card.Count},{card.CountFoiled}," +
                $"\"{card.Name}\",{rarity},{card.Mana},\"{card.TypeLine}\"";
    }
}