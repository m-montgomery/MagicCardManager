using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Magic;

public class JSONManager : IDataManager 
{    
    public static JSONManager Instance { get; } = new JSONManager();

    private string SetIDRegex = @"(\d{1,3})(.*)"; // valid for core sets

    private JSONManager() {}

    public ICollection<Card> Import(string source) 
    {
        try
        {   
            var data = File.ReadAllText(source);
            var results = JsonConvert.DeserializeObject<List<Card>>(data) ?? new List<Card>();

            foreach (var result in results)
            {
                ProcessCard(result);
            }

            return results;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to import from JSON");
            Console.WriteLine(e);
            return new List<Card>();
        }
    }        

    private void ProcessCard(Card card)
    {
        // format set code
        card.SetCode = card.SetCode.ToUpper();
        if (card.SetCode.Length > 3)
            card.SetCode = card.SetCode.Substring(card.SetCode.Length - 3); // remove extraneous prefixes, e.g. T for tokens

        // parse variants
        var matches = Regex.Matches(card.SetID, SetIDRegex);
        foreach (Match match in matches)
        {
            card.SetID = match.Groups[1].Value.PadLeft(3, '0');
            card.Variant = match.Groups[2].Value;
        }
    }

    public ICollection<CardCorrection> ImportCorrections(string source)
    {
        // not implemented
        return new List<CardCorrection>();
    }

    public bool Export(IDictionary<string, Card> cards, string destination) 
    {
        // not implemented
        return false;
    }
}
