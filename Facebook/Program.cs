namespace Facebook;

class Program
{
    static async Task Main(string[] args)
    {
        string faceBookAccessToken = Environment.GetEnvironmentVariable("FACEBOOK_ACCESS_TOKEN") ?? "";
        string pageId = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ID") ?? "";
        string message = "Hello, Facebook! This is a test post.";

        var fbClient = new FacebookService.Client(faceBookAccessToken, pageId);
        
       byte[] image = File.ReadAllBytes("/Users/hinnerk/Downloads/redkonf185.jpg");

        bool success = await fbClient.PostImageAsync(message, image);
        Console.WriteLine(success ? "Posted to Facebook successfully!" : "Failed to post to Facebook.");
    }
}
