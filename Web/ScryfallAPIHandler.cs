using Newtonsoft.Json.Linq;

namespace Magic;

public static class ScryfallAPIHandler
{
    private static readonly string BaseURI = "https://api.scryfall.com";
    private static readonly string URI_BulkDataIndex = "/bulk-data";

    private static readonly string CardDataID = "default_cards";
    private static readonly string CardImageID = "large";

    public static string DefaultFilePath => "ScryfallCardData.json";

    private static async Task<string> GetBulkDataUri()
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

    public static async Task<bool> DownloadCardData(string destination)
    {
        Console.WriteLine("\nDownloading data from Scryfall...");

        // get URI for latest bulk data file
        var uri = await GetBulkDataUri();
        if (string.IsNullOrEmpty(uri))
            return false;

        // get latest bulk data file
        return await HTTPHandler.Instance.DownloadFile(uri, destination);
    }

    public static async Task<bool> DownloadCardImages(ICollection<Card> cards, string path)
    {
        Console.WriteLine("\nDownloading images from Scryfall...");

        if (!PrepareImageFolders(cards, path))
            return false;

        var toDownload = PrepareImagesForDownload(cards, path, out int skipped);

        // download all images
        var downloaded = 0;
        var semaphore = new SemaphoreSlim(initialCount: 10, maxCount: 10);
        var tasks = toDownload.Select(async entry =>
        {
            await semaphore.WaitAsync();

            try
            {
                if (await HTTPHandler.Instance.DownloadFile(entry.Value, entry.Key))
                    Interlocked.Increment(ref downloaded);
                
                await Task.Delay(1000); // rate-limit per Scryfall request
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        
        Console.WriteLine($"Downloaded {downloaded} images; skipped {skipped} already present");

        return downloaded + skipped > 0;
    }

    private static bool PrepareImageFolders(ICollection<Card> cards, string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            // one folder per set
            var cardSets = cards.Select(c => c.SetCode).Distinct();
            foreach (var cardSet in cardSets)
            {
                Directory.CreateDirectory(Path.Combine(path, cardSet));
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to create image directories");
            Console.WriteLine(e);
            return false;
        }
    }

    private static Dictionary<string, string> PrepareImagesForDownload(ICollection<Card> cards, string path, out int skipped)
    {
        skipped = 0;
        var toDownload = new Dictionary<string, string>();
        
        foreach (Card card in cards)
        {
            var imageName = $"{card.SetCode}_{card.SetID}{card.Variant}.jpg";
            var fileName = Path.Combine(path, card.SetCode, imageName);

            // skip duplicate cards
            if (toDownload.ContainsKey(fileName)) 
                continue;

            // don't overwrite already-downloaded images
            if (File.Exists(fileName)) 
            {
                skipped++;
                continue;
            }

            // determine image URI
            var imageUri = card.ImageUris
                .Where(uri => uri.Key == CardImageID)
                .Select(uri => uri.Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(imageUri))
                Console.WriteLine($"No image URI found for card {imageName}");
            else
                toDownload.Add(fileName, imageUri);
        }

        return toDownload;
    }
}
