namespace SocialService;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class MastodonClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly string _apiBaseUrl;


    /// <summary>
    ///  Constructor for MastodonClient.
    /// </summary>
    /// <param name="accessToken"> from your mastodon application</param>
    /// <param name="apiBaseUrl"> url to the instance</param>
    public MastodonClient(string accessToken, string apiBaseUrl = "https://mastodon.social")
    {
        _httpClient = new HttpClient();
        _accessToken = accessToken;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
    }

    /// <summary>
    ///   Post a status to Mastodon /Text only/
    /// </summary>
    /// <param name="status"> String to be published </param> 
    /// <returns>bool</returns>
    public async Task<bool> PostStatusAsync(string status)
    {
        var requestUri = $"{_apiBaseUrl}/api/v1/statuses";
        var content = new StringContent($"{{\"status\":\"{status}\"}}", Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }


    /// <summary>
    ///  Post a status to Mastodon with a link
    /// </summary>
    /// <param name="status"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<bool> PostUrlAsync(string status, Uri url)
    {
        return await PostStatusAsync($"{status} \u26F5 {url}");
    }


    /// <summary>
    ///  Post a status to Mastodon with a single image
    /// </summary>
    /// <param name="status"></param>
    /// <param name="imageContent"></param>
    /// <param name="imageName"></param>
    /// <param name="altText"></param>
    /// <returns></returns>
    public async Task<bool> PostImageAsync(string status, Models.ImageUpload image)
    {
        var requestUri = $"{_apiBaseUrl}/api/v1/media";
        var imageExtension = image.FileName.Split('.')[^1].ToLower();
        switch (imageExtension)
        {
            case "jpg":
            case "jpeg":
                image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                break;
            case "png":
                image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                break;
            case "gif":
                image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
                break;
            default:
                return false;
        }

        var uploadContent = new MultipartFormDataContent
        {
            { new StringContent(status), "status" },
            { image.Image, "file", image.FileName }
        };

        if (!string.IsNullOrEmpty(image.AltText))
        {
            uploadContent.Add(new StringContent(image.AltText), "description");
        }

        var imageResponse = await _httpClient.PostAsync(requestUri, uploadContent);

        if (!imageResponse.IsSuccessStatusCode)
        {
            return false;
        }

        var imageId = await imageResponse.Content.ReadAsStringAsync();
        requestUri = $"{_apiBaseUrl}/api/v1/statuses";
        var content = new StringContent($"{{\"status\":\"{status}\",\"media_ids\":[{imageId}]}}", Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

    /// <summary>
    ///  Post a status to Mastodon with multiple images
    /// </summary>
    /// <param name="status"></param>
    /// <param name="Images">List of images</param>
    /// <returns></returns>
    public async Task<bool> PostMultipleImagesAsync(string status, Models.ImagesList Images)
    {
        var mediaIds = new List<string>();

        foreach (var image in Images.Images)
        {
            var requestUri = $"{_apiBaseUrl}/api/v1/media";
            var imageExtension = image.FileName.Split('.')[^1].ToLower();
            switch (imageExtension)
            {
                case "jpg":
                case "jpeg":
                    image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    break;
                case "png":
                    image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    break;
                case "gif":
                    image.Image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
                    break;
                default:
                    return false;
            }

            var uploadContent = new MultipartFormDataContent
            {
                { image.Image, "file", image.FileName }
            };

            if (!string.IsNullOrEmpty(image.AltText))
            {
                uploadContent.Add(new StringContent(image.AltText), "description");
            }

            var imageResponse = await _httpClient.PostAsync(requestUri, uploadContent);
            if (!imageResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var imageResponseContent = await imageResponse.Content.ReadAsStringAsync();
            var imageId = JsonDocument.Parse(imageResponseContent).RootElement.GetProperty("id").GetString();
            mediaIds.Add(imageId);
        }

        var mediaIdsString = string.Join(",", mediaIds);
        var statusRequestUri = $"{_apiBaseUrl}/api/v1/statuses";
        var content = new StringContent($"{{\"status\":\"{status}\",\"media_ids\":[{mediaIdsString}]}}", Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(statusRequestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }
}