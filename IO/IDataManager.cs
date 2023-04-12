namespace Magic 
{
    public interface IDataManager {
        
        ICollection<Card> Import(string source);

        bool Export(IDictionary<string, List<Card>> cards, string destination);
    }
}