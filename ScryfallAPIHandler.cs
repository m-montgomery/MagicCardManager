using Newtonsoft.Json.Linq;

namespace Magic
{
    public class ScryfallAPIHandler
    {
        private readonly string BaseURI = "https://api.scryfall.com";
        private readonly string URI_BulkDataIndex = "/bulk-data";
        private readonly string CardDataID = "default_cards";

        public string DefaultFilePath => "ScryfallCardData.json";

        private async Task<string> GetBulkDataUri()
        {
            var bulkDataInfo = await HTTPHandler.Instance.Get(BaseURI + URI_BulkDataIndex);
            
            try
            {
                var json = JObject.Parse(bulkDataInfo);

                if (json.TryGetValue("data", out var dataArray))
                {
                    var cardData = dataArray
                        .Where(data => data["type"]?.ToString() == CardDataID)
                        .FirstOrDefault();
                    
                    if (cardData != null)
                        return cardData["download_uri"]?.ToString() ?? "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not parse bulk data from Scryfall");
                Console.WriteLine(e);
            }
            return "";
        }

        public async Task<bool> DownloadCardData(string destination)
        {
            Console.WriteLine("\nDownloading data from Scryfall...");

            // get URI for latest bulk data file
            var uri = await GetBulkDataUri();
            if (string.IsNullOrEmpty(uri))
                return false;

            // get latest bulk data file
            return await HTTPHandler.Instance.DownloadFile(uri, destination);
        }
    }
}