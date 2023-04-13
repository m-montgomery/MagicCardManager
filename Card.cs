using Newtonsoft.Json;

namespace Magic;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Special,
    Mythic,
    Bonus
}

public class Card 
{
    [JsonProperty("name")]
    public string Name {get; set;}

    [JsonProperty("type_line")]
    public string TypeLine {get; set;}

    [JsonProperty("set_name")]
    public string Set {get; set;}
    
    [JsonProperty("set")]
    public string SetCode {get; set;}

    [JsonProperty("collector_number")]
    public string SetID {get; set;}

    public string Variant {get; set;}

    [JsonProperty("rarity")]
    public Rarity Rarity {get; set;}

    [JsonProperty("foil")]
    public bool Foiled {get; set;}

    [JsonProperty("mana_cost")]
    public string Mana {get; set;}

    [JsonProperty("image_uris")]
    public Dictionary<string, string> ImageUris {get; set;}

    public Card() 
    {
        Name = "";
        Set = "";
        SetCode = "";
        SetID = "";
        Variant = "";
        TypeLine = "";
        Mana = "";
        Foiled = false;
        Rarity = Rarity.Common;
        ImageUris = new Dictionary<string, string>();
    }

    public string UniqueID()
    {
        // TCG version
        if (SetCode == "")
            return $"{Set}{Name}{Variant}";
        
        // Scryfall version
        else
            return $"{SetCode}{SetID}{Variant}";
    }

    public override string ToString()
    {
        var props = new List<string> {
            $"Set: {Set}",
            $"SetCode: {SetCode}",
            $"SetID: {SetID}",
            $"Variant: {Variant}",
            $"Name: {Name}",
            $"Rarity: {Rarity}",
            $"Mana: {Mana}",
            $"TypeLine: {TypeLine}",
            $"Foiled: {Foiled}",
        };
        return string.Join(", ", props);
    }

    public Card Copy()
    {
        return new Card() {
            Set = this.Set,
            SetCode = this.SetCode,
            SetID = this.SetID,
            Variant = this.Variant,
            Name = this.Name,
            Rarity = this.Rarity,
            Mana = this.Mana,
            TypeLine = this.TypeLine,
            Foiled = this.Foiled,
            ImageUris = new Dictionary<string, string>(this.ImageUris),
        };
    }

    public bool Matches(Card other, bool ignoreSet = false) 
    {
        // function assumes this card is TCG and other card is Scryfall

        // set comparison functionality needed for more general usage
        if (!ignoreSet && !String.Equals(Set, other.Set, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!NameMatches(other.Name))
            return false;
        
        if (!VariantMatches(other))
            return false;

        return true;
    }
    
    private bool NameMatches(string sfName)
    {
        if (String.Equals(Name, sfName, StringComparison.OrdinalIgnoreCase))
            return true;

        // handle special case of 2-sided name
        if (sfName.Contains(" // "))
        {
            var parts = sfName.Split(" // ");
            if (String.Equals(Name, parts[0], StringComparison.OrdinalIgnoreCase) ||
                String.Equals(Name, parts[1], StringComparison.OrdinalIgnoreCase))
            return true;
        }

        return false;
    }

    private bool VariantMatches(Card sfCard)
    {
        // TCG is inconsistent about variant vs set ID vs other info in this field
        // so check several comparisons

        var paddedVarient = Variant.PadLeft(3, '0');
        if (String.Equals(Variant, sfCard.Variant, StringComparison.OrdinalIgnoreCase) ||
            String.Equals(paddedVarient, sfCard.Variant, StringComparison.OrdinalIgnoreCase))
            return true;
        
        // if using TCG's "variant" field as set ID, only match on non-variant Scryfall
        if (String.Equals(Variant, sfCard.SetID, StringComparison.OrdinalIgnoreCase) ||
            String.Equals(paddedVarient, sfCard.SetID, StringComparison.OrdinalIgnoreCase))
            return sfCard.Variant == "";
        
        return false;
    }
}