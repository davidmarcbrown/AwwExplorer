using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml.Data;

namespace Aww_Explorer.DataSources
{
    class Posts: ObservableCollection<Post>, ISupportIncrementalLoading
    {
        private const int LIMIT = 50;
        private string subreddit;
        private string after;
        private HttpClient httpClient;
        private bool busy = false;
        private bool hasMoreItems = true;

        public Posts(string subreddit)
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("user-agent", "A demo app written with .Net");
            
            this.subreddit = subreddit;
        }

        public bool HasMoreItems
        {
            get { return hasMoreItems; }
        }

        public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (busy)
            {
                throw new InvalidOperationException("Only one operation in flight at a time");
            }

            busy = true;

            return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
        }

        private async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                await addPosts();

                return new LoadMoreItemsResult();
            }
            finally
            {
                busy = false;
            }
        }

        public async Task<bool> addPosts(){
            string requestUrl = "http://reddit.com/r/" + subreddit + ".json" +
                "?limit=" + LIMIT +
                (!string.IsNullOrWhiteSpace(after) ? "&after=" + after : string.Empty);

            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            string jsonText = await response.Content.ReadAsStringAsync();

            JsonValue page = JsonValue.Parse(jsonText);

            string kind = page.GetObject().GetNamedString("kind");

            after = page.GetObject().GetNamedObject("data").GetNamedString("after");

            if (after != null)
                hasMoreItems = true;
            else
                hasMoreItems = false;

            if (kind.Equals("Listing"))
            {
                JsonArray children = page.GetObject().GetNamedObject("data").GetNamedArray("children");
                int count = children.Count();

                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Post post = new Post();

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
                            this.Add(post);
                        }
                    }
                }
            }

            return true;
        }
    }
}
