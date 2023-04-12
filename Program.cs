namespace Magic
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // dotnet run -- [arg0] .. [argN]
            Console.WriteLine("Magic Card Manager running\n");

            var manager = new CardManager(JSONManager.Instance, CSVManager.Instance, CSVManager.Instance);

            // import cards from user's TCG data
            if (!manager.ImportUserCards(args[0])) 
            {
                Console.WriteLine("No user data to merge; exiting");
                return;
            }
            
            // import all card data from Scryfall
            if (!manager.ImportCardData(args[1])) 
            {
                Console.WriteLine("No source data to reference; exiting");
                return;
            }

            // match up user cards with Scryfall data; export to file
            manager.MatchCards("results.csv");
        }
    }
}