namespace Magic;

public class CardCorrection
{
    public string Name {get; set;}
    public string Set {get; set;}
    public string Variant {get; set;}
    public string CorrectedName {get; set;}
    public string CorrectedSet {get; set;}
    public string CorrectedVariant {get; set;}

    public CardCorrection()
    {
        Name = "";
        Set = "";
        Variant = "";
        CorrectedName = "";
        CorrectedSet = "";
        CorrectedVariant = "";
    }
}