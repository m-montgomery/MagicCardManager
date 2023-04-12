namespace Magic;

class HTTPHandler
{
    public static HTTPHandler Instance { get; } = new HTTPHandler();

    private HttpClient Client;

    private HTTPHandler() 
    {
        Client = new HttpClient();
    }

    public async Task<string> Get(string uri)
    {
        try
        {
            Console.WriteLine($"GET {uri}");

            var response = await Client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            Console.WriteLine("GET failed - " + response.StatusCode);
            return "";
        }
        catch (Exception e)
        {
            Console.WriteLine("GET failed");
            Console.WriteLine(e);
            return "";
        }
    }

    public async Task<bool> DownloadFile(string uri, string destination)
    {
        try
        {
            Console.WriteLine($"Downloading file from {uri}");
            var stream = await Client.GetStreamAsync(uri);

            using (var fstream = File.Create(destination))
            {
                await stream.CopyToAsync(fstream);
                Console.WriteLine($"Downloaded file to {destination}");
                return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to download file");
            Console.WriteLine(e);
            return false;
        }
    }
}
