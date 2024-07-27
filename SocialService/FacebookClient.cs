namespace SocialService;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Models;

public class FacebookClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly string _pageId;

    /// <summary>
    /// Constructor for FacebookClient.
    /// </summary>
    /// <param name="accessToken">Access token for your Facebook page(!)</param>
    /// <param name="pageId">ID of the Facebook Page</param>
    public FacebookClient(string accessToken, string pageId)
    {
        _httpClient = new HttpClient();
        _accessToken = accessToken;
        _pageId = pageId;
    }
    
    public async Task<string> GetLongLivedAccessTokenAsync(string shortLivedAccessToken)
    {
        var requestUri = $"https://graph.facebook.com/v20.0/oauth/access_token?grant_type=fb_exchange_token&client_id={_accessToken}&client_secret={_pageId}&fb_exchange_token={shortLivedAccessToken}";
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseContent).RootElement;
        return responseJson.GetProperty("access_token").GetString() ?? string.Empty;
    }

    /// <summary>
    /// Post a status to the Facebook Page.
    /// </summary>
    /// <param name="message">The message to post</param>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> PostStatusAsync(string message)
    {
        var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/feed";
        var content = new StringContent($"{{\"message\":\"{message}\",\"access_token\":\"{_accessToken}\"}}", Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    /// <summary>
    /// Post a link to the Facebook Page.
    /// </summary>
    /// <param name="message">The message to accompany the link</param>
    /// <param name="link">The URL to post</param>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> PostLinkAsync(string message, string link)
    {
        var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/feed";
        var content = new StringContent($"{{\"message\":\"{message}\", \"link\":\"{link}\", \"access_token\":\"{_accessToken}\"}}", Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    /// <summary>
    /// Post an image to the Facebook Page.
    /// </summary>
    /// <param name="message">The message to accompany the image</param>
    /// <param name="imageUrl">The URL of the image</param>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> PostImageAsync(string message, string imageUrl)
    {
        var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/photos";
        var content = new StringContent($"{{\"message\":\"{message}\", \"url\":\"{imageUrl}\", \"access_token\":\"{_accessToken}\"}}", Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    
    public async Task<bool> PostImageAsync(string message, byte[] imageBytes)
    {
        var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/photos";
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(message), "message");
        content.Add(new ByteArrayContent(imageBytes), "image", "image.jpg");
        content.Add(new StringContent(_accessToken), "access_token");
        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    } 
    
    public async Task<string> GetPageIdAsync()
    {
        var requestUri = $"https://graph.facebook.com/v20.0/me/accounts?access_token={_accessToken}";
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseContent).RootElement;
        return responseJson.GetProperty("data")[0].GetProperty("id").GetString() ?? string.Empty;
    }
    
    public async Task<string> GetPageFeedAsync()
    {
        var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/feed?access_token={_accessToken}";
        var response = await _httpClient.GetAsync(requestUri);
        var responseContent = await response.Content.ReadAsStringAsync();
        return responseContent;
    }
    
    public async Task<bool> PostImagesListAsync(string message, Models.ImagesList images)
    {
        var mediaIds = new List<string>();

        foreach (var image in images.Images)
        {
            var requestUri = $"https://graph.facebook.com/v20.0/{_pageId}/photos";
            var content = new MultipartFormDataContent
            {
                { new ByteArrayContent(await image.Image.ReadAsByteArrayAsync()), "source", image.FileName },
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("false"), "published" } // Ensure the image is not published immediately
            };

            var response = await _httpClient.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error uploading image: {response.StatusCode}");
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var mediaId = JsonDocument.Parse(responseContent).RootElement.GetProperty("id").GetString();
            mediaIds.Add(mediaId);
        }

        var mediaIdsString = string.Join(",", mediaIds.Select(id => $"{{\"media_fbid\":\"{id}\"}}"));
        var postRequestUri = $"https://graph.facebook.com/v20.0/{_pageId}/feed";
        var postContent = new StringContent($"{{\"message\":\"{message}\",\"attached_media\":[{mediaIdsString}],\"access_token\":\"{_accessToken}\"}}", Encoding.UTF8, "application/json");

        try
        {
            var postResponse = await _httpClient.PostAsync(postRequestUri, postContent);
            return postResponse.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}

