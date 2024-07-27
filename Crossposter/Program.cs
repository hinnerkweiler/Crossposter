using MastodonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MastodonPoster
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string mastodonAccessToken = null;
            string faceBookAccessToken = Environment.GetEnvironmentVariable("FACEBOOK_ACCESS_TOKEN") ?? "";
            string pageId = Environment.GetEnvironmentVariable("FACEBOOK_PAGE_ID") ?? "";
            string status = null;
            List<string> images = new List<string>();
            
            //display a help message if no arguments are provided
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Crossposter --token <access_token> [--image <image_path>]  [--image <image_path>] --status <status>");
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--token":
                        if (i + 1 < args.Length)
                        {
                            mastodonAccessToken = args[++i];
                        }
                        break;
                    case "--status":
                        status = string.Join(" ", args.Skip(i + 1));
                        i = args.Length; // End parsing as status is the last parameter
                        break;
                    case "--image":
                        if (i + 1 < args.Length)
                        {
                            images.Add(args[++i]);
                        }
                        break;
                }
            }

            mastodonAccessToken ??= Environment.GetEnvironmentVariable("MASTODON_ACCESS_TOKEN") ?? string.Empty;
            if (string.IsNullOrEmpty(mastodonAccessToken))
            {
                Console.WriteLine("Please provide an access token for your Mastodon account as an argument '--token' or set the environment variable MASTODON_ACCESS_TOKEN.");
                return;
            }

            if (string.IsNullOrEmpty(status))
            {
                Console.WriteLine("Please provide a status using the '--status' argument.");
                return;
            }

            var client = new MastodonClient(mastodonAccessToken);

            bool success;
            if (images.Count > 0)
            {
                var imageUploads = images.Select(imagePath => new MastodonService.Models.ImageUpload
                {
                    FileName = System.IO.Path.GetFileName(imagePath),
                    Image = new ByteArrayContent(System.IO.File.ReadAllBytes(imagePath))
                }).ToList();

                success = await client.PostMultipleImagesAsync(status, new MastodonService.Models.ImagesList { Images = imageUploads });
            }
            else
            {
                success = await client.PostStatusAsync(status);
            }

            Console.WriteLine(success ? "Posted successfully!" : "Failed to post.");
        }
    }
}