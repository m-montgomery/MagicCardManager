using System.Text;
using System.Text.RegularExpressions;
using TinyCsvParser;
using TinyCsvParser.Mapping;

namespace Magic 
{
    public class CSVManager : IDataManager 
    {
        public static CSVManager Instance { get; } = new CSVManager();
        
        public static readonly string CardOutputHeaders = "Set,Num,#,# Foil,Name,Rarity,Mana Cost,Type Line";

        private readonly string TCGNameRegex = @"([a-zA-Z\-/', ]+[a-zA-Z])( ?\([^)]+\))?( - Full Art)?( - \[Foil\])?";
        private readonly string TCGChecklistRegex = @"Check ?[lL]ist Card - ([a-zA-Z\-/', ]+[a-zA-Z])";
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

        public ICollection<Card> Import(string source) 
        {
            if (!File.Exists(source)) {
                Console.WriteLine(source + " file not found");
                return new List<Card>();
            }

            try
            {
                // read entries from TCG CSV with preliminary mapping
                var parserOptions = new CsvParserOptions(true, ',');
                var parser = new CsvParser<TCGCard>(parserOptions, new TCGCardMap());

                var parsedResults = parser.ReadFromFile(source, Encoding.ASCII);

                // convert CSV entries to Card object(s)
                var cards = parsedResults
                    .Where(c => c.IsValid && c.Result.Count > 0)
                    .SelectMany(c => ProcessCard(c.Result))
                    .ToList();

                return cards;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to import from CSV", e);
                return new List<Card>();
            }
        }

        private ICollection<Card> ProcessCard(TCGCard entry)
        {
            var name = entry.Name.Replace("Token", "").Trim(); // ignore 'Token'
            var set = entry.Set.Trim();
            var variant = "";
            var foiled = false;

            // checklist card special name case
            var match = Regex.Match(name, TCGChecklistRegex);
            if (match.Success)
            {
                name = match.Groups[1].Value + " Checklist";
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

            // generate cards
            var cards = new List<Card>();
            for (int _ = 0; _ < entry.Count; _++)
            {
                cards.Add(new Card()
                {
                    Name = name,
                    Set = set,
                    Variant = variant,
                    Foiled = foiled,
                });
            }
            return cards;
        }

        public bool Export(IDictionary<string, List<Card>> cards, string destination) 
        {
            try 
            {
                var lines = cards
                    .Select(c => CardToCSV(c.Key, cards))
                    .Prepend(CardOutputHeaders);
                
                File.WriteAllLines(destination, lines);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to export to CSV", e);
                return false;
            }
        }

        private string CardToCSV(string key, IDictionary<string, List<Card>> cards)
        {
            var uniqueCardset = cards[key];
            var cardTemplate = uniqueCardset.FirstOrDefault();
            if (cardTemplate == null)
                return "";
            
            var set = cardTemplate.SetCode == "" ? cardTemplate.Set : cardTemplate.SetCode; // fall back on set name for matchless cards
            var setID = cardTemplate.SetID + cardTemplate.Variant;
            var rarity = cardTemplate.Rarity.ToString().Substring(0, 1);
            var foiledCount = uniqueCardset.Where(card => card.Foiled).Count();

            return $"{set},{setID},{uniqueCardset.Count - foiledCount},{foiledCount}," +
                   $"\"{cardTemplate.Name}\",{rarity},{cardTemplate.Mana},\"{cardTemplate.TypeLine}\"";
        }
    }
}