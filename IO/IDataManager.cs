namespace Magic;

public interface IDataManager {
    
    ICollection<Card> Import(string source);

    ICollection<CardCorrection> ImportCorrections(string source);

    bool Export(IDictionary<string, Card> cards, string destination);
}
