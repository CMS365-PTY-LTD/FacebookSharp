﻿using FacebookSharp.Entities;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Globalization;

namespace FacebookSharp.Controllers
{
    class PageController
    {
        private string accessToken;
        public PageController(string longLivedAccessToken)
        {
            accessToken = longLivedAccessToken;
        }
        public async Task<JObject> GetPageDetailsAsync(string pageId)
        {
            try
            {
                var options = new RestClientOptions("https://graph.facebook.com")
                {
                    MaxTimeout = -1,
                };
                var client = new RestClient(options);
                var request = new RestRequest($"/{pageId}?access_token={accessToken}&fields=name,about", Method.Get);
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    if (string.IsNullOrEmpty(response.Content))
                    {
                        throw new Exception("No content found.");
                    }
                    return JObject.Parse(response.Content);
                }
                throw new Exception($"Error, {response.Content}");
            }
            catch (Exception ex)
            {
                return new JObject(new { message = ex.Message });
            }
        }
        private string formatImageLinks(IEnumerable<string> imageIds)
        {
            var formattedStringArray = imageIds.Select((r, i) => "attached_media[" + i + "]={\"media_fbid\":\"" + r + "\"}");
            return string.Join("&", formattedStringArray);
        }
        public async Task<JObject> PostFeedAsync(string pageId, PageFeedRequestContent pageFeedRequestContent)
        {
            try
            {
                if (pageFeedRequestContent==null ||
                    ((pageFeedRequestContent.PhotoUrls==null || pageFeedRequestContent.PhotoUrls.Any()==false) && (pageFeedRequestContent.MessageLines==null || pageFeedRequestContent.MessageLines.Any()==false)))
                {
                    throw new Exception("you must pass some content to post");
                }
                List<string> tempPictureUploadIds = new List<string>();
                if (pageFeedRequestContent.PhotoUrls!=null && pageFeedRequestContent.PhotoUrls.Any())
                {
                    var photo = new Photo(accessToken);
                    foreach (var photoUrl in pageFeedRequestContent.PhotoUrls)
                    {
                        tempPictureUploadIds.Add(await photo.UploadImageTempAsync(pageId, photoUrl));
                    }
                }
                string formattedString = formatImageLinks(tempPictureUploadIds.ToArray());
                var options = new RestClientOptions("https://graph.facebook.com")
                {
                    MaxTimeout = -1,
                };
                var client = new RestClient(options);
                var request = new RestRequest($"/{pageId}/feed?access_token={accessToken}&message={string.Join("%0A", pageFeedRequestContent.MessageLines)}&{formattedString}", Method.Post);
                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    if (string.IsNullOrEmpty(response.Content))
                    {
                        throw new Exception("No content found.");
                    }
                    return JObject.Parse(response.Content);
                }
                throw new Exception($"Error, {response.Content}");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        class Photo
        {
            private string accessToken;
            public Photo(string longLivedAccessToken)
            {
                accessToken = longLivedAccessToken;
            }
            public async Task<string> UploadImageTempAsync(string pageId, string photoUrl)
            {
                try
                {
                    var options = new RestClientOptions("https://graph.facebook.com")
                    {
                        MaxTimeout = -1,
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest($"/me/photos?access_token={accessToken}&url={photoUrl}&published=false", Method.Post);
                    var response = await client.ExecuteAsync(request);
                    if (response.IsSuccessful)
                    {
                        if (string.IsNullOrEmpty(response.Content))
                        {
                            throw new Exception("No content found.");
                        }
                        var id = JObject.Parse(response.Content)["id"];

                        return id == null ? "" : id.ToString();
                    }
                    throw new Exception($"Error, {response.Content}");
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }
    }
}