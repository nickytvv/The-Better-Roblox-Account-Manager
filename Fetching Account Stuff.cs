using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Account_Manager
{
    public partial class Form1 : Form
    {
        private async Task FetchAndDisplayFriends(string identifier, bool isUsername)
        {
            string userId = identifier;

            if (isUsername)
            {
                userId = await FetchUserIdByUsername(identifier);
                if (userId == null)
                {
                    AddToConsole("> Failed to fetch user ID. Please try again.", Color.Red);
                    return;
                }
            }

            string url = $"https://friends.roblox.com/v1/users/{userId}/friends";
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(content);
                        JArray friendsArray = (JArray)json["data"];
                        friendsListView.Items.Clear();

                        foreach (var friend in friendsArray)
                        {
                            string friendName = (string)friend["name"];
                            string friendId = (string)friend["id"];
                            string avatarUrl = await FetchAvatarUrl(friendId);
                            ListViewItem item = new ListViewItem(new[] { friendName, friendId, avatarUrl });
                            friendsListView.Items.Add(item);
                        }
                    }
                    else
                    {
                        AddToConsole("> Failed to fetch friend list. Please try again.", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex.Message}", Color.Red);
                }
            }
        }

        private async Task<string> FetchUserIdByUsername(string username)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync($"https://www.roblox.com/users/profile?username={username}");
                    if (response.IsSuccessStatusCode)
                    {
                        string url = response.RequestMessage.RequestUri.ToString();
                        Regex regex = new Regex(@"\d+");
                        Match match = regex.Match(url);
                        if (match.Success)
                        {
                            string userId = match.Value;
                            return userId;
                        }
                        else
                        {
                            AddToConsole("> Failed to extract user ID from URL.", Color.Red);
                        }
                    }
                    else
                    {
                        AddToConsole("> Failed to fetch user profile. Please try again.", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex.Message}", Color.Red);
                }
            }
            return null;
        }


        private async Task<string> FetchAvatarUrl(string userId)
        {
            string url = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId},0&size=48x48&format=Png&isCircular=false";
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    if (json["data"] is JArray data && data.Count > 0)
                    {
                        string imageUrl = (string)data[0]["imageUrl"];
                        return imageUrl;
                    }
                }
            }
            return "Unknown";
        }
    }
}
