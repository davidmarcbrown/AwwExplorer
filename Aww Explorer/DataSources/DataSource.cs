using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Aww_Explorer.DataSources
{
    public class RedditGridViewItem
    {
        public string ID {get; set;}
        public string Title { get; set; }
        public string Link { get; set; }
        public string Thumbnail { get; set; }
        public string Author { get; set; }
    }

    public class DataSource
    {
        public ObservableCollection<RedditGridViewItem> Posts;
        private HttpClient httpClient;

        public DataSource()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("user-agent", "A demo app written with .Net");

            Posts = new ObservableCollection<RedditGridViewItem>();
        }

        public async void PopulatePosts(string subreddit)
        {
            string requestUrl = "http://reddit.com/r/" + subreddit + ".json";
            string jsonText;

            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            jsonText = await response.Content.ReadAsStringAsync();

            JsonValue page = JsonValue.Parse(jsonText);

            string kind = page.GetObject().GetNamedString("kind");

            if (kind.Equals("Listing"))
            {
                JsonArray children = page.GetObject().GetNamedObject("data").GetNamedArray("children");
                int count = children.Count();

                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        RedditGridViewItem post = new RedditGridViewItem();

                        JsonObject jsonPost = children.GetObjectAt((uint)i).GetNamedObject("data");
                        bool isSelf = jsonPost.GetNamedBoolean("is_self");
                        string thumb = jsonPost.GetNamedString("thumbnail");

                        if (!isSelf && !thumb.Equals("default"))
                        {
                            post.Title = jsonPost.GetNamedString("title");
                            post.ID = jsonPost.GetNamedString("id");
                            post.Author = jsonPost.GetNamedString("author");
                            post.Thumbnail = thumb;
                            post.Link = jsonPost.GetNamedString("url");
                            Posts.Add(post);
                        }
                    }
                }
            }
        }
    }
}
