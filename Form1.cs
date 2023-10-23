using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AccountManager
{
    public partial class Form1 : Form
    {
        private Proxy _proxyForm;
        private Dictionary<string, string> csrfTokens = new Dictionary<string, string>();
        private HttpClient httpClient = new HttpClient(); 

        private List<Color> lineColors = new List<Color>();

        private string originalMachineGuid;
        private string originalDisplayId;
        private Dictionary<string, string> originalMACAddresses = new Dictionary<string, string>();
        private string originalHardwareGUID;
        private string originalMachineId;
        private string originalBIOSReleaseDate;
        private string originalComputerName;
        private string originalMachineGUID;
        private CancellationTokenSource cts;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private int minGroupId = 150;
        private bool useProxy = false;
        private int maxGroupId = 10000000;
        HashSet<int> scrapedGroupIds = new HashSet<int>();
        object lockObj = new object();

        private Queue<int> groupIds = new Queue<int>();
        private int lastGroupId = 0;
        private int processed = 0;
        int total_count = 10;
        private bool isScraping = false;

        public Form1()
        {
            UpdateVersionLabel();
            InitializeComponent();
            CheckForUpdates();
            checkCSV();
            LoadUsernamesFromCSV(listView1);
            LoadUsernamesFromCSV(listView3);
            treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            treeView1.ImageList = imageList1;

            string legalDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LEGAL");
            string jsonFilePath = Path.Combine(legalDir, "UserAgreement.json");


            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                UserAgreement userAgreement = JsonConvert.DeserializeObject<UserAgreement>(json);

                nameTextbox.Text = userAgreement.UserName;
                nameTextbox.Enabled = false;

                spaceButton5.Enabled = true;
                spaceButton6.Enabled = true;
                spaceButton7.Enabled = true;
                spaceButton8.Enabled = true;
                spaceButton9.Enabled = true;
                spaceButton10.Enabled = true;
                signButton.Enabled = false;
            }
            else
            {
                spaceButton6.Enabled = false;
                spaceButton7.Enabled = false;
                spaceButton8.Enabled = false;
                spaceButton9.Enabled = false;
                spaceButton5.Enabled = false;
                spaceButton10.Enabled = false;
                signButton.Enabled = true;
            }
        }



        private void foreverTreeView3_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void spaceForm1_Click(object sender, EventArgs e)
        {

        }
     
        public void AddToConsole(string message, Color color)
        {
            try
            {
                if (Outputbox == null)
                {
                    MessageBox.Show("Outputbox is not initialized.");
                    return;
                }

                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                if (Outputbox.InvokeRequired)
                {
                    Outputbox.Invoke(new Action(() => AddToConsole(message, color)));
                    return;
                }
                lineColors.Add(color);

                Outputbox.SelectionStart = Outputbox.TextLength;
                Outputbox.SelectionLength = 0;

                Outputbox.SelectionColor = color;
                Outputbox.AppendText(message + "\n");
                Outputbox.SelectionColor = Outputbox.ForeColor;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        public Color GetColorForLine(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineColors.Count)
            {
                return lineColors[lineIndex];
            }
            else
            {
                return Color.Black;
            }
        }

        private void spaceButton1_Click(object sender, EventArgs e)
        {
            tabPage1.SelectedTab = tabPage4;
            spaceButton3.Focus();
            spaceButton3.BackColor = Color.LightBlue;
        }

        private void foreverCheckBox1_CheckedChanged(object sender)
        {
            if (foreverCheckBox1.Checked)
            {
                this.TopMost = true;
                AddToConsole("> TopMost = True;", Color.White);
            }
            else
            {
                this.TopMost = false;
                AddToConsole("> TopMost = false;", Color.White);
            }
        }

        public class XCsrfToken
        {
            public string Token { get; set; }
            public string Ticket { get; set; }
        }

        public async Task<(bool, string)> ValidateCookie(string cookie)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.roblox.com/mobileapi/userinfo");
                request.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            string responseBody = await reader.ReadToEndAsync();
                            JObject json = JObject.Parse(responseBody);

                            if (json.ContainsKey("UserName") && json.ContainsKey("UserID"))
                            {
                                string username = json["UserName"].ToString();
                                string userId = json["UserID"].ToString();
                                SaveToCSV(username, cookie, userId, "");

                                var csrfToken = await AuthenticateAsync(cookie);
                                AddToConsole($"Fetched token for cookie {username}: {csrfToken.Token}", Color.Blue);

                                if (!csrfTokens.ContainsKey(cookie))
                                {
                                    csrfTokens.Add(cookie, csrfToken.Token);
                                    AddToConsole("Added new token: " + csrfToken.Token, Color.Red);
                                }
                                else
                                {
                                    AddToConsole("Token already exists. Updating...", Color.Yellow);
                                    csrfTokens[cookie] = csrfToken.Token;
                                }

                                return (true, username);
                            }
                            else
                            {
                                return (false, "Invalid JSON response: Missing UserName or UserID");
                            }
                        }
                    }
                    else
                    {
                        return (false, $"HTTP Error: {response.StatusCode}");
                    }
                }
            }
            catch (WebException e)
            {
                return (false, $"Request error: {e.Message}");
            }
            catch (JsonException e)
            {
                return (false, $"JSON error: {e.Message}");
            }
            catch (Exception e)
            {
                return (false, $"An unexpected error occurred: {e.Message}");
            }
        }

        public void SaveToCSV(string username, string cookie, string userId, string notes)
        {
            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "RobloxAccounts.csv");
            List<string> lines = File.ReadAllLines(csvFilePath).ToList();

            bool userExists = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts[0] == username && parts[2] == userId)
                {
                    string existingNotes = parts.Length > 3 ? parts[3] : "";
                    string newNotes = string.IsNullOrEmpty(notes) ? existingNotes : notes;
                    lines[i] = $"{username},{cookie},{userId},{newNotes}";
                    userExists = true;
                    AddToConsole("> Account already added. Updating details.", Color.Yellow);
                    break;
                }
            }

            if (!userExists)
            {
                lines.Add($"{username},{cookie},{userId},{notes}");
                AddToConsole("> Account details updated successfully.", Color.Green);
            }

            File.WriteAllLines(csvFilePath, lines);
        }


        public async Task<XCsrfToken> AuthenticateAsync(string Cookie)
        {
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage csrfRequest = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v1/usernames/validate");
            csrfRequest.Headers.Add("Referer", "https://www.roblox.com/");
            csrfRequest.Headers.Add("Cookie", Cookie);
            csrfRequest.Headers.Add("User-Agent", "Roblox/WinInet");
            csrfRequest.Headers.Add("Origin", "https://roblox.com");

            HttpResponseMessage csrfResponse = await httpClient.SendAsync(csrfRequest);

            if (csrfResponse.Headers.TryGetValues("x-csrf-token", out var csrfTokens))
            {
                return new XCsrfToken
                {
                    Token = csrfTokens.FirstOrDefault()
                };
            }
            else
            {
                throw new Exception("The given header was not found.");
            }
        }
     
        public void checkCSV()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);

            if (exeDirectory == null)
            {
                AddToConsole("> Failed to get the directory path.", Color.Red);
                return;
            }

            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");

            if (!File.Exists(csvFilePath))
            {
                using (StreamWriter sw = File.CreateText(csvFilePath))
                {
                    sw.WriteLine("Username,Cookie,UserID");
                }
                AddToConsole("> Created RobloxAccounts.csv File..", Color.Green);
            }
            else
            {
                AddToConsole("> RobloxAccounts.csv File Exists.", Color.Black);
            }

        }
        private HttpClientHandler SetupProxy(string proxyLine)
        {
            var httpClientHandler = new HttpClientHandler();
            if (string.IsNullOrEmpty(proxyLine)) return httpClientHandler;
            var proxyParts = proxyLine.Split(':');
            var proxy = new WebProxy($"{proxyParts[0]}:{proxyParts[1]}", false)
            {
                Credentials = new NetworkCredential(proxyParts[2], proxyParts[3])
            };
            httpClientHandler.Proxy = proxy;
            return httpClientHandler;
        }

        public async Task<Dictionary<string, Tuple<long, string, string>>> FetchItems(HttpClient httpClient, CancellationToken token, string proxyLine = null, string category = "1", string keyword = "", int limit = 30, int retryCount = 0)
        {
            Dictionary<string, Tuple<long, string, string>> result = new Dictionary<string, Tuple<long, string, string>>();
            HashSet<string> seenItems = new HashSet<string>();
            string cursor = "";
            HttpClientHandler httpClientHandler = SetupProxy(proxyLine);

            using (HttpClient newHttpClient = new HttpClient(httpClientHandler))
            {
                while (cursor != null)
                {
                    if (token.IsCancellationRequested)
                    {
                        AddToConsole("> Scraping cancelled.", Color.Yellow);
                        return result;
                    }

                    string apiUrl = $"https://catalog.roblox.com/v1/search/items?Category={category}&salesTypeFilter=1&limit={limit}&minPrice=0&maxPrice=0";
                    if (!string.IsNullOrEmpty(keyword)) apiUrl += $"&keyword={keyword}";
                    if (!string.IsNullOrEmpty(cursor)) apiUrl += $"&cursor={cursor}";

                    HttpResponseMessage response = await newHttpClient.GetAsync(apiUrl);
                    if (response.Content == null)
                    {
                        AddToConsole("> Response content is null.", Color.Red);
                        continue;
                    }

                    string rawData = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        AddToConsole($"> Failed to fetch items. Status code: {response.StatusCode}", Color.Red);
                        continue;
                    }

                    JObject res = JObject.Parse(rawData);

                    if (res["data"] == null || !res["data"].HasValues)
                    {
                        AddToConsole("> No items found in the response.", Color.Yellow);
                        return result;
                    }

                    foreach (var item in res["data"])
                    {
                        try
                        {
                            AddToConsole("Fetching Api Call", Color.HotPink);
                            string itemType = item["itemType"]?.ToString();
                            if (itemType == "Asset")
                            {
                                long id = item["id"]?.ToObject<long>() ?? 0;
                                if (id == 0) continue;

                                HttpResponseMessage assetResponse = await newHttpClient.GetAsync($"https://economy.roblox.com/v2/assets/{id}/details");
                                if (assetResponse.Content == null) continue;

                                string assetData = await assetResponse.Content.ReadAsStringAsync();
                                JObject assetDetails = JObject.Parse(assetData);

                                string itemName = assetDetails["Name"]?.ToString();
                                if (string.IsNullOrEmpty(itemName)) continue;

                                long productId = item["productId"]?.ToObject<long>() ?? id;
                                string itemLink = $"https://www.roblox.com/catalog/{productId}/";

                                if (seenItems.Add(itemName))
                                {
                                    result[itemName] = Tuple.Create(productId, itemLink, itemType);
                                    TreeNode node = new TreeNode(itemName);
                                    node.Nodes.Add($"Link: {itemLink}");
                                    treeView1.Nodes.Add(node);
                                    AddToConsole($"Asset Name: {itemName}, Asset Link: {itemLink}", Color.Green);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
                        }
                    }
                    cursor = res["nextPageCursor"]?.ToString();
                    await Task.Delay(2000);
                }
            }
            return result;
        }




        public async Task Purchase(HttpClient httpClient, string name, long productId, string csrfToken, string cookie, string proxyLine, string selectedUsername)
        {
            AddToConsole("Purchase method called.", Color.Magenta);

            var buydata = new { collectibleItemId = productId };
            var content = new StringContent(JsonConvert.SerializeObject(buydata), Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Add("x-csrf-token", csrfToken);
            httpClient.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={cookie}");

            AddToConsole($"CSRF Token: {csrfToken}", Color.Cyan);
            AddToConsole($"Cookie: {cookie}", Color.Cyan);

            HttpClientHandler httpClientHandler = SetupProxy(proxyLine);
            if (!string.IsNullOrEmpty(proxyLine))
            {
                string[] proxyParts = proxyLine.Split(':');
                string ip = proxyParts[0];
                string port = proxyParts[1];
                string username = proxyParts[2];
                string password = proxyParts[3];
                var proxy = new WebProxy($"{ip}:{port}", false)
                {
                    Credentials = new NetworkCredential(username, password)
                };
                httpClientHandler.Proxy = proxy;
                AddToConsole($"Proxy: {proxyLine}", Color.Cyan);
            }

            try
            {
                using (HttpClient newHttpClient = new HttpClient(httpClientHandler))
                {
                    HttpResponseMessage purchaseResponse = await newHttpClient.PostAsync($"https://apis.roblox.com/marketplace-sales/v1/item/{productId}/purchase-item", content);
                    string purchaseData = await purchaseResponse.Content.ReadAsStringAsync();
                    JObject purchaseRes = JObject.Parse(purchaseData);

                    AddToConsole($"HTTP Status: {purchaseResponse.StatusCode}", Color.Cyan);
                    AddToConsole($"HTTP Content: {purchaseData}", Color.Cyan);

                    if (purchaseResponse.IsSuccessStatusCode)
                    {
                        if (purchaseRes.ContainsKey("reason") && purchaseRes["reason"].ToString() == "AlreadyOwned")
                        {
                            AddToConsole($"{name} is already owned by {selectedUsername}", Color.Yellow);
                            return;
                        }
                        AddToConsole($"Successfully purchased {name} for {selectedUsername}", Color.Green);
                    }
                    else
                    {
                        AddToConsole($"Failed to purchase {name} for {selectedUsername}. Status code: {purchaseResponse.StatusCode}", Color.Red);
                    }
                }
            }
            catch (Exception e)
            {
                AddToConsole($"Exception: {e.Message}", Color.Red);
            }
        }


        private async void spaceButton3_Click(object sender, EventArgs e)
        {
            string cookie = saveCookieTxt.Text;

            if (string.IsNullOrEmpty(cookie))
            {
                AddToConsole("> No Roblox cookie provided.", Color.Red);
                return;
            }

            var (isValid, result) = await ValidateCookie(cookie);

            if (isValid)
            {
                AddToConsole("> Cookie is valid.", Color.Green);
                ListViewItem item = new ListViewItem(result);
                listView1.Items.Add(item);

                ListViewItem item2 = (ListViewItem)item.Clone();

                ListViewItem item3 = (ListViewItem)item.Clone();
                listView3.Items.Add(item3);

            }
            else
            {
                AddToConsole($"> Cookie is invalid. Reason: {result}", Color.Red);
            }
        }


        public void LoadUsernamesFromCSV(System.Windows.Forms.ListView targetListView)
        {
            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
            ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Delete");
            deleteMenuItem.Click += (sender, e) =>
            {
                if (targetListView.SelectedItems.Count > 0)
                {
                    ListViewItem selectedItem = targetListView.SelectedItems[0];
                    string selectedUsername = selectedItem.Text;
                    targetListView.Items.Remove(selectedItem);
                    RemoveAccountFromCSV(selectedUsername);
                }
            };
            ToolStripMenuItem refreshMenuItem = new ToolStripMenuItem("Refresh");
            refreshMenuItem.Click += (sender, e) =>
            {
                targetListView.Clear();
                LoadUsernamesFromCSV(targetListView);
                AddToConsole("Cleared" + targetListView, Color.White);
            };
            ToolStripMenuItem launchInBrowserMenuItem = new ToolStripMenuItem("Launch in Browser");
            launchInBrowserMenuItem.Click += async (sender, e) =>
            {
                if (targetListView.SelectedItems.Count > 0)
                {
                    ListViewItem selectedItem = targetListView.SelectedItems[0];
                    string selectedUsername = selectedItem.Text;
                    string cookie = selectedItem.SubItems[1].Text;

                    var (isValid, username) = await ValidateCookie(cookie);
                    if (isValid)
                    {
                        Browser browserForm = new Browser();
                        browserForm.Show();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to validate cookie for {selectedUsername}");
                    }
                }
            };
            ToolStripMenuItem exportMenuItem = new ToolStripMenuItem("Export");
            exportMenuItem.Click += (sender, e) =>
            {
                if (targetListView.SelectedItems.Count > 0)
                {
                    ListViewItem selectedItem = targetListView.SelectedItems[0];
                    string selectedUsername = selectedItem.Text;
                    string cookie = selectedItem.SubItems[1].Text;

                    // Prompt the user
                    DialogResult dialogResult = MessageBox.Show("You can export the selected account as either a CSV or a JSON file. Do you want to continue?", "Export Option", MessageBoxButtons.YesNo);

                    if (dialogResult == DialogResult.Yes)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json";
                        saveFileDialog.Title = "Export Account Information";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            string filePath = saveFileDialog.FileName;
                            string fileExtension = Path.GetExtension(filePath);

                            if (fileExtension == ".csv")
                            {
                                File.WriteAllText(filePath, $"Username,Cookie\n{selectedUsername},{cookie}");
                            }
                            else if (fileExtension == ".json")
                            {
                                string jsonContent = $"{{\"Username\": \"{selectedUsername}\", \"Cookie\": \"{cookie}\"}}";
                                File.WriteAllText(filePath, jsonContent);
                            }
                        }
                    }
                }
            };



            contextMenuStrip.Items.Add(deleteMenuItem);
            contextMenuStrip.Items.Add(refreshMenuItem);
            contextMenuStrip.Items.Add(launchInBrowserMenuItem);
            contextMenuStrip.Items.Add(exportMenuItem);
            targetListView.ContextMenuStrip = contextMenuStrip;

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            if (exeDirectory == null)
            {
                AddToConsole("> Failed to get the directory path.", Color.Red);
                return;
            }
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            if (File.Exists(csvFilePath))
            {
                using (StreamReader sr = new StreamReader(csvFilePath))
                {
                    string line;
                    bool skipHeader = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (skipHeader)
                        {
                            skipHeader = false;
                            continue;
                        }
                        string[] values = line.Split(',');
                        string username = values[0];
                        string cookie = values[1];
                        string notes = values.Length > 3 ? values[3] : "";

                        ListViewItem item = new ListViewItem(new[] { username, cookie });
                        item.Tag = notes;
                        targetListView.Items.Add(item);
                    }
                }
            }
            else
            {
                AddToConsole("> RobloxAccounts.csv File Does Not Exist.", Color.Red);
            }
        }


        private void RemoveAccountFromCSV(string usernameToRemove)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            List<string> lines = File.ReadAllLines(csvFilePath).ToList();
            lines.RemoveAll(line => line.StartsWith(usernameToRemove + ","));
            File.WriteAllLines(csvFilePath, lines);
            AddToConsole("> Selected account deleted", Color.Green);
        }

        private void spaceButton11_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap img = (Bitmap)assetPreview.Image;
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\Assets");
                img.Save($"{Environment.CurrentDirectory}\\Assets\\{assetTextbox.Text}.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine("> Asset downloaded successfully.");
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }
        }

        public string GetBetween(string source, string start, string end)
        {
            int Start, End;
            if (source.Contains(start) && source.Contains(end))
                if (source.Substring(source.IndexOf(start)).Contains(end))
                    try
                    {
                        Start = source.IndexOf(start, 0) + start.Length;
                        End = source.IndexOf(end, Start);
                        return source.Substring(Start, End - Start);
                    }
                    catch (ArgumentOutOfRangeException) { return ""; }
                else return "";
            else return "";
        }


        private void spaceButton12_Click(object sender, EventArgs e)
        {
            try
            {
                string html, imagelocation = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://assetdelivery.roblox.com/v1/asset/?id={assetTextbox.Text}");
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        html = reader.ReadToEnd();
                    }
                }

                string AssetId = GetBetween(html, "<url>http://www.roblox.com/asset/?id=", "</url>");
                HttpWebRequest imagelocationreq = (HttpWebRequest)WebRequest.Create($"https://assetdelivery.roblox.com/v1/assetid/{AssetId}");
                imagelocationreq.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse iresponse = (HttpWebResponse)imagelocationreq.GetResponse())
                using (Stream istream = iresponse.GetResponseStream())
                using (StreamReader reader = new StreamReader(istream))
                {
                    imagelocation = reader.ReadToEnd();
                }

                string imageloc = GetBetween(imagelocation, "{\"location\":\"", "\"");
                HttpWebRequest imagereq = (HttpWebRequest)WebRequest.Create(imageloc);
                imagereq.AutomaticDecompression = DecompressionMethods.GZip;

                Bitmap img = new Bitmap(1, 1);
                using (HttpWebResponse iresponse = (HttpWebResponse)imagereq.GetResponse())
                using (Stream istream = iresponse.GetResponseStream())
                {
                    img = (Bitmap)Image.FromStream(istream);
                }

                if (checkBox1.Checked || checkBox2.Checked)
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if (checkBox1.Checked)
                            g.DrawImage(Properties.Resources.shirt_template, new RectangleF(0, 0, img.Width, img.Height));
                        else if (checkBox2.Checked)
                            g.DrawImage(Properties.Resources.pants_template, new RectangleF(0, 0, img.Width, img.Height));
                    }
                }
                img.Save("debug_image.png");

                assetPreview.Image = img;
                assetPreview.SizeMode = PictureBoxSizeMode.StretchImage;


                string pathString = Path.Combine(Environment.CurrentDirectory, "Assets");
                if (!Directory.Exists(pathString))
                {
                    Directory.CreateDirectory(pathString);
                }
                img.Save($"{pathString}\\{assetTextbox.Text}.png", System.Drawing.Imaging.ImageFormat.Png);

                AddToConsole("> Asset fetched and downloaded successfully.", Color.Green);
            }

            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }
        }

        private void dreamTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private bool IsNumeric(string input)
        {
            return long.TryParse(input, out _);
        }
        private async void spaceButton11_Click_1(object sender, EventArgs e)
        {
            string userInput = dreamTextBox2.Text;
            bool isUsername = !IsNumeric(userInput);
            await FetchAndDisplayFriends(userInput, isUsername);
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView2.SelectedItems[0];
                string selectedFriendName = selectedItem.SubItems[0].Text;
                string selectedFriendId = selectedItem.SubItems[1].Text;
                string selectedFriendAvatarUrl = selectedItem.SubItems[2].Text;

                AddToConsole("> Avatar URL: " + selectedFriendAvatarUrl, Color.Blue);

                foreverLabel12.Text = selectedFriendName;
                foreverLabel11.Text = selectedFriendId;

                try
                {
                    if (!string.IsNullOrEmpty(selectedFriendAvatarUrl) && selectedFriendAvatarUrl != "Unknown")
                    {
                        pictureBox1.Load(selectedFriendAvatarUrl);
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

                    }
                    else
                    {
                        AddToConsole("> Avatar URL is empty or unknown.", Color.Orange);
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"Failed to load avatar: {ex.Message}", Color.Red);
                }
            }
        }

        private void foreverLabel11_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(foreverLabel11.Text);
            AddToConsole("> Copied cookie to clipboard", Color.Green);
        }

        private async void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView3.SelectedItems[0];
                string selectedUsername = selectedItem.Text;

                string csvFilePath = "RobloxAccounts.csv";
                List<string> lines = new List<string>(File.ReadAllLines(csvFilePath));
                for (int i = 0; i < lines.Count; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts[0] == selectedUsername)
                    {
                        foreverLabel2.Text = parts[0];
                        foreverLabel5.Text = parts[2];
                        foreverLabel3.Text = parts[1];

                        string userId = parts[2];
                        string avatarUrl = await FetchAvatarUrl(userId);
                        if (avatarUrl != "Unknown")
                        {
                            using (HttpClient httpClient = new HttpClient())
                            {
                                byte[] imageData = await httpClient.GetByteArrayAsync(avatarUrl);
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    pictureBox2.Image = Image.FromStream(ms);
                                }
                            }
                            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                        }

                        if (parts.Length > 3)
                        {
                        }
                        else
                        {
                        }

                        var (isValid, _) = await ValidateCookie(parts[1]);
                        if (isValid)
                        {
                            foreverLabel18.Text = "Verified";
                            foreverLabel18.ForeColor = Color.Green;
                        }
                        else
                        {
                            foreverLabel18.Text = "Unverified";
                            foreverLabel18.ForeColor = Color.Red;

                            DialogResult dialogResult = MessageBox.Show("The cookie for this account is unverified. Would you like to update it?", "Update Cookie", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.Yes)
                            {
                                string newCookie = Interaction.InputBox("Enter the new cookie:", "Update Cookie", "");

                                var (newIsValid, newUsername) = await ValidateCookie(newCookie);
                                if (newIsValid)
                                {
                                    if (newUsername == selectedUsername)
                                    {
                                        lines[i] = $"{parts[0]},{newCookie},{parts[2]}";
                                        File.WriteAllLines(csvFilePath, lines);

                                        foreverLabel3.Text = newCookie;

                                        foreverLabel18.Text = "Verified";
                                        foreverLabel18.ForeColor = Color.Green;
                                    }
                                    else
                                    {
                                        MessageBox.Show("The new cookie does not match the selected username.", "Mismatched Cookie", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("The new cookie is invalid.", "Invalid Cookie", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        public async void spaceButton4_Click(object sender, EventArgs e)
        {

        }

        private async void spaceButton5_Click(object sender, EventArgs e)
        {
            try
            {
                AddToConsole("> Starting ProcessScsiPorts...", Color.HotPink);
                await ProcessScsiPortsAsync();
                AddToConsole("> ProcessScsiPorts completed.", Color.HotPink);
            }
            catch (Exception ex)
            {
                AddToConsole($"An error occurred: {ex.Message}", Color.Red);
            }
        }

        private void spaceButton6_Click(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey machineGuidKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", true))
                {
                    if (machineGuidKey != null)
                    {
                        originalMachineGuid = machineGuidKey.GetValue("MachineGuid")?.ToString();

                        string newMachineGuid = RandomIdprid2(10);
                        machineGuidKey.SetValue("MachineGuid", newMachineGuid);

                        string logBefore = $"Machine GUID - Before: {originalMachineGuid}";
                        string logAfter = $"Machine GUID - After: {newMachineGuid}";
                        SaveLogsAsync("ChangeMachineGuid", logBefore, logAfter);
                    }
                    else
                    {
                        AddToConsole("Machine GUID registry key not found.", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsole("An error occurred while changing the machine GUID: " + ex.Message, Color.Red);
            }
        }

        private void spaceButton7_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey displaySettings = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", true);

                if (displaySettings != null)
                {
                    originalDisplayId = displaySettings.GetValue("MRU0")?.ToString();
                    int displayId = RandomDisplayId();
                    string spoofedDisplayId = $"SpoofedDisplay{displayId}";

                    displaySettings.SetValue("MRU0", spoofedDisplayId);
                    displaySettings.SetValue("MRU1", spoofedDisplayId);
                    displaySettings.SetValue("MRU2", spoofedDisplayId);
                    displaySettings.SetValue("MRU3", spoofedDisplayId);
                    displaySettings.SetValue("MRU4", spoofedDisplayId);

                    string logBefore = "Display ID - Before: " + originalDisplayId;
                    string logAfter = "Display ID - After: " + displayId;
                    SaveLogsAsync("display", logBefore, logAfter);

                    AddToConsole("Display Function executed successfully.", Color.Green);
                }
                else
                {
                    AddToConsole("Display settings registry key not found.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                AddToConsole("An error occurred while changing the display ID: " + ex.Message, Color.Red);
            }
        }

        private void spaceButton10_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to spoof the MAC address?", "Confirm Spoofing", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    bool spoofSuccess = SpoofMAC();

                    if (!spoofSuccess)
                    {
                        AddToConsole("MAC address successfully spoofed.", Color.Green);
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole("An error occurred while spoofing the MAC address: " + ex.Message, Color.Red);
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                AddToConsole("MAC address spoofing cancelled.", Color.Orange);
            }
        }


        private void spaceButton9_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to change the GUIDs?", "Confirm Change", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    using (RegistryKey HardwareGUID = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\IDConfigDB\\Hardware Profiles\\0001", true))
                    {
                        if (HardwareGUID != null)
                        {
                            originalHardwareGUID = HardwareGUID.GetValue("HwProfileGuid")?.ToString();
                            string logBefore = "HwProfileGuid - Before: " + originalHardwareGUID;
                            HardwareGUID.DeleteValue("HwProfileGuid");
                            string newHardwareGUID = Guid.NewGuid().ToString();
                            HardwareGUID.SetValue("HwProfileGuid", newHardwareGUID);
                            string logAfter = "HwProfileGuid - After: " + newHardwareGUID;
                            SaveLogsAsync("guid", logBefore, logAfter);
                        }
                        else
                        {
                            AddToConsole("HardwareGUID key not found.", Color.Red);
                        }
                    }

                    using (RegistryKey MachineGUID = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", true))
                    {
                        if (MachineGUID != null)
                        {
                            originalMachineGUID = MachineGUID.GetValue("MachineGuid")?.ToString();
                            string logBefore = "MachineGuid - Before: " + originalMachineGUID;
                            MachineGUID.DeleteValue("MachineGuid");
                            string newMachineGUID = Guid.NewGuid().ToString();
                            MachineGUID.SetValue("MachineGuid", newMachineGUID);
                            string logAfter = "MachineGuid - After: " + newMachineGUID;
                            SaveLogsAsync("guid", logBefore, logAfter);
                        }
                        else
                        {
                            AddToConsole("MachineGUID key not found.", Color.Red);
                        }
                    }

                    using (RegistryKey MachineId = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\SQMClient", true))
                    {
                        if (MachineId != null)
                        {
                            originalMachineId = MachineId.GetValue("MachineId")?.ToString();
                            string logBefore = "MachineId - Before: " + originalMachineId;
                            MachineId.DeleteValue("MachineId");
                            string newMachineId = Guid.NewGuid().ToString();
                            MachineId.SetValue("MachineId", newMachineId);
                            string logAfter = "MachineId - After: " + newMachineId;
                            SaveLogsAsync("guid", logBefore, logAfter);
                        }
                        else
                        {
                            AddToConsole("MachineId key not found.", Color.Red);
                        }
                    }

                    using (RegistryKey SystemInfo = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\SystemInformation", true))
                    {
                        if (SystemInfo != null)
                        {
                            Random rnd = new Random();
                            int day = rnd.Next(1, 31);
                            string dayStr = (day < 10) ? $"0{day}" : day.ToString();

                            int month = rnd.Next(1, 13);
                            string monthStr = (month < 10) ? $"0{month}" : month.ToString();

                            int year = rnd.Next(1990, 2023);
                            string yearStr = year.ToString();

                            string randomDate = $"{monthStr}/{dayStr}/{yearStr}";

                            originalBIOSReleaseDate = SystemInfo.GetValue("BIOSReleaseDate")?.ToString();
                            string logBefore = "BIOSReleaseDate - Before: " + originalBIOSReleaseDate;
                            SystemInfo.SetValue("BIOSReleaseDate", randomDate);
                            string logAfter = "BIOSReleaseDate - After: " + randomDate;
                            SaveLogsAsync("guid", logBefore, logAfter);
                        }
                        else
                        {
                            AddToConsole("SystemInformation key not found.", Color.Red);
                        }
                    }

                    AddToConsole("GUIDs successfully generated.", Color.Green);
                }
                catch (Exception ex)
                {
                    AddToConsole("An error occurred: " + ex.Message, Color.Red);
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                AddToConsole("GUID change cancelled.", Color.Orange);
            }
        }

        private void spaceButton8_Click(object sender, EventArgs e)
        {
            try
            {
                string originalName;
                string newName = RandomId(8);
                using (RegistryKey computerNameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName", true))
                {
                    if (computerNameKey != null)
                    {
                        originalName = computerNameKey.GetValue("ComputerName").ToString();
                        originalComputerName = originalName;

                        computerNameKey.SetValue("ComputerName", newName);
                        computerNameKey.SetValue("ActiveComputerName", newName);
                        computerNameKey.SetValue("ComputerNamePhysicalDnsDomain", "");
                    }
                    else
                    {
                        AddToConsole("ComputerName key not found.", Color.Red);
                        return;
                    }
                }
                using (RegistryKey activeComputerNameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName", true))
                {
                    if (activeComputerNameKey != null)
                    {
                        activeComputerNameKey.SetValue("ComputerName", newName);
                        activeComputerNameKey.SetValue("ActiveComputerName", newName);
                        activeComputerNameKey.SetValue("ComputerNamePhysicalDnsDomain", "");
                    }
                    else
                    {
                        AddToConsole("ActiveComputerName key not found.", Color.Red);
                        return;
                    }
                }
                using (RegistryKey hostnameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", true))
                {
                    if (hostnameKey != null)
                    {
                        hostnameKey.SetValue("Hostname", newName);
                        hostnameKey.SetValue("NV Hostname", newName);
                    }
                    else
                    {
                        AddToConsole("Hostname key not found.", Color.Red);
                        return;
                    }
                }
                using (RegistryKey interfacesKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", true))
                {
                    if (interfacesKey != null)
                    {
                        foreach (string interfaceName in interfacesKey.GetSubKeyNames())
                        {
                            using (RegistryKey interfaceKey = interfacesKey.OpenSubKey(interfaceName, true))
                            {
                                if (interfaceKey != null)
                                {
                                    interfaceKey.SetValue("Hostname", newName);
                                    interfaceKey.SetValue("NV Hostname", newName);
                                }
                            }
                        }
                    }
                }
                string logBefore = "ComputerName - Before: " + originalName;
                string logAfter = "ComputerName - After: " + newName;
                SaveLogsAsync("pcname", logBefore, logAfter);

                AddToConsole("PC name spoofed successfully.", Color.Green);
            }
            catch (Exception ex)
            {
                AddToConsole("An error occurred while spoofing the PC name: " + ex.Message, Color.Red);
            }
        }

        private void spaceButton2_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(originalMachineGuid))
                {
                    AddToConsole("No original Machine GUID to revert to.", Color.Red);
                }
                else
                {
                    using (RegistryKey machineGuidKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", true))
                    {
                        if (machineGuidKey != null)
                        {
                            machineGuidKey.SetValue("MachineGuid", originalMachineGuid);
                            AddToConsole($"Successfully reverted Machine GUID to {originalMachineGuid}.", Color.Green);
                        }
                        else
                        {
                            AddToConsole("Machine GUID registry key not found.", Color.Red);
                        }
                    }
                }

                if (string.IsNullOrEmpty(originalComputerName))
                {
                    AddToConsole("No original Computer Name to revert to.", Color.Red);
                }
                else
                {
                    try
                    {
                        using (RegistryKey computerNameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName", true))
                        {
                            if (computerNameKey != null)
                            {
                                computerNameKey.SetValue("ComputerName", originalComputerName);
                                computerNameKey.SetValue("ActiveComputerName", originalComputerName);
                            }
                        }
                        using (RegistryKey activeComputerNameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName", true))
                        {
                            if (activeComputerNameKey != null)
                            {
                                activeComputerNameKey.SetValue("ComputerName", originalComputerName);
                                activeComputerNameKey.SetValue("ActiveComputerName", originalComputerName);
                            }
                        }
                        using (RegistryKey hostnameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", true))
                        {
                            if (hostnameKey != null)
                            {
                                hostnameKey.SetValue("Hostname", originalComputerName);
                                hostnameKey.SetValue("NV Hostname", originalComputerName);
                            }
                        }
                        using (RegistryKey interfacesKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", true))
                        {
                            if (interfacesKey != null)
                            {
                                foreach (string interfaceName in interfacesKey.GetSubKeyNames())
                                {
                                    using (RegistryKey interfaceKey = interfacesKey.OpenSubKey(interfaceName, true))
                                    {
                                        if (interfaceKey != null)
                                        {
                                            interfaceKey.SetValue("Hostname", originalComputerName);
                                            interfaceKey.SetValue("NV Hostname", originalComputerName);
                                        }
                                    }
                                }
                            }
                        }
                        AddToConsole($"Successfully reverted Computer Name to {originalComputerName}.", Color.Green);
                    }
                    catch (Exception ex)
                    {
                        AddToConsole("An error occurred while reverting the Computer Name: " + ex.Message, Color.Red);
                    }

                    if (string.IsNullOrEmpty(originalHardwareGUID))
                    {
                        AddToConsole("No original Hardware GUID to revert to.", Color.Red);
                    }
                    else
                    {
                        using (RegistryKey HardwareGUID = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\IDConfigDB\\Hardware Profiles\\0001", true))
                        {
                            if (HardwareGUID != null)
                            {
                                HardwareGUID.SetValue("HwProfileGuid", originalHardwareGUID);
                                AddToConsole($"Successfully reverted Hardware GUID to {originalHardwareGUID}.", Color.Green);
                            }
                            else
                            {
                                AddToConsole("Hardware GUID registry key not found.", Color.Red);
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(originalBIOSReleaseDate))
                    {
                        AddToConsole("No original BIOSReleaseDate to revert to.", Color.Red);
                    }
                    else
                    {
                        using (RegistryKey SystemInfo = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\SystemInformation", true))
                        {
                            if (SystemInfo != null)
                            {
                                SystemInfo.SetValue("BIOSReleaseDate", originalBIOSReleaseDate);
                                AddToConsole($"Successfully reverted BIOSReleaseDate to {originalBIOSReleaseDate}.", Color.Green);
                            }
                            else
                            {
                                AddToConsole("SystemInformation registry key not found.", Color.Red);
                            }
                        }
                    }


                    if (string.IsNullOrEmpty(originalMachineId))
                    {
                        AddToConsole("No original MachineId to revert to.", Color.Red);
                    }
                    else
                    {
                        using (RegistryKey MachineId = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\SQMClient", true))
                        {
                            if (MachineId != null)
                            {
                                MachineId.SetValue("MachineId", originalMachineId);
                                AddToConsole($"Successfully reverted MachineId to {originalMachineId}.", Color.Green);
                            }
                            else
                            {
                                AddToConsole("MachineId registry key not found.", Color.Red);
                            }
                        }
                    }

                    //MAC 
                    try
                    {
                        using (RegistryKey NetworkAdapters = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}", true))
                        {
                            foreach (var entry in originalMACAddresses)
                            {
                                string adapterId = entry.Key;
                                string originalMAC = entry.Value;

                                using (RegistryKey NetworkAdapter = NetworkAdapters.OpenSubKey(adapterId, true))
                                {
                                    if (NetworkAdapter != null)
                                    {
                                        if (string.IsNullOrEmpty(originalMAC))
                                        {
                                            NetworkAdapter.DeleteValue("NetworkAddress", false);
                                        }
                                        else
                                        {
                                            NetworkAdapter.SetValue("NetworkAddress", originalMAC);
                                        }
                                        Enable_LocalAreaConection(adapterId, false);
                                        Enable_LocalAreaConection(adapterId, true);
                                        AddToConsole($"Successfully reverted MAC address for adapter {adapterId} to {originalMAC}.", Color.Green);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddToConsole("An error occurred while reverting the MAC addresses: " + ex.Message, Color.Red);
                    }


                    if (string.IsNullOrEmpty(originalDisplayId))
                    {
                        AddToConsole("No original Display ID to revert to.", Color.Red);
                    }
                    else
                    {
                        using (RegistryKey displaySettings = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RunMRU", true))
                        {
                            if (displaySettings != null)
                            {
                                displaySettings.SetValue("MRU0", originalDisplayId);
                                displaySettings.SetValue("MRU1", originalDisplayId);
                                displaySettings.SetValue("MRU2", originalDisplayId);
                                displaySettings.SetValue("MRU3", originalDisplayId);
                                displaySettings.SetValue("MRU4", originalDisplayId);
                                AddToConsole($"Successfully reverted Display ID to {originalDisplayId}.", Color.Green);
                            }
                            else
                            {
                                AddToConsole("Display settings registry key not found.", Color.Red);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsole("An error occurred while reverting: " + ex.Message, Color.Red);
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void spaceButton13_Click(object sender, EventArgs e)
        {
            tabPage1.SelectedTab = tabPage4;
            saveCookieTxt.Focus();
            spaceButton3.BackColor = Color.LightBlue;
        }

        private void spaceButton14_Click(object sender, EventArgs e)
        {
            CheckForUpdates();

        }
        private Bitmap DownloadAsset(string assetId, string assetType)
        {
            try
            {
                string html, imagelocation = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://assetdelivery.roblox.com/v1/asset/?id={assetId}");
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        html = reader.ReadToEnd();
                    }
                }

                string AssetId = GetBetween(html, "<url>http://www.roblox.com/asset/?id=", "</url>");
                HttpWebRequest imagelocationreq = (HttpWebRequest)WebRequest.Create($"https://assetdelivery.roblox.com/v1/assetid/{AssetId}");
                imagelocationreq.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse iresponse = (HttpWebResponse)imagelocationreq.GetResponse())
                using (Stream istream = iresponse.GetResponseStream())
                using (StreamReader reader = new StreamReader(istream))
                {
                    imagelocation = reader.ReadToEnd();
                }

                string imageloc = GetBetween(imagelocation, "{\"location\":\"", "\"");
                HttpWebRequest imagereq = (HttpWebRequest)WebRequest.Create(imageloc);
                imagereq.AutomaticDecompression = DecompressionMethods.GZip;

                Bitmap img = new Bitmap(1, 1);
                using (HttpWebResponse iresponse = (HttpWebResponse)imagereq.GetResponse())
                using (Stream istream = iresponse.GetResponseStream())
                {
                    img = (Bitmap)Image.FromStream(istream);
                }

                if (assetType == "tshirt" || assetType == "pants")
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if (assetType == "tshirt")
                            g.DrawImage(Properties.Resources.shirt_template, new RectangleF(0, 0, img.Width, img.Height));
                        else if (assetType == "pants")
                            g.DrawImage(Properties.Resources.pants_template, new RectangleF(0, 0, img.Width, img.Height));
                    }
                }

                string pathString = Path.Combine(Environment.CurrentDirectory, "Assets");
                if (!Directory.Exists(pathString))
                {
                    Directory.CreateDirectory(pathString);
                }
                img.Save($"{pathString}\\{assetId}.png", System.Drawing.Imaging.ImageFormat.Png);

                AddToConsole($"> Asset {assetType} (ID: {assetId}) downloaded.", Color.Gray);

                return img;
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred while downloading asset {assetId}: {ex.Message}", Color.Red);
                return null;
            }
        }

        private List<long> groupAssetIds = new List<long>();

        private async Task<List<long>> FetchAndDownloadGroupAssets(long groupId) // Change the method signature to accept long.
        {
            try
            {
                AddToConsole($"> Fetching group assets for Group ID: {groupId}...", Color.Gray);

                // Make a request to the Roblox API to fetch group assets
                string apiUrl = $"https://catalog.roblox.com/v1/search/items/details?Category=3&CreatorType=2&IncludeNotForSale=true&Limit=30&CreatorTargetId={groupId}";
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();

                    // Parse the JSON data to retrieve asset details
                    JObject data = JObject.Parse(jsonData);
                    JArray items = (JArray)data["data"];

                    AddToConsole($"> {items.Count} group assets found for Group ID: {groupId}.", Color.Gray);

                    foreach (JObject item in items)
                    {
                        long assetId = item["id"].ToObject<long>(); // Use long instead of int.
                        string assetName = item["name"].ToString();
                        string assetType = item["assetType"].ToString().ToLower();

                        AddToConsole($"> Downloading asset: {assetName} (ID: {assetId})...", Color.Gray);

                        DownloadAsset(assetId.ToString(), assetType);

                        groupAssetIds.Add(assetId); // Use long instead of int.

                        AddToConsole($"> Asset {assetName} (ID: {assetId}) downloaded successfully.", Color.Green);
                    }

                    AddToConsole($"> {items.Count} group assets fetched and downloaded successfully.", Color.Green);
                }
                else
                {
                    AddToConsole($"> Failed to fetch group assets for Group ID: {groupId}. Status code: {response.StatusCode}", Color.Red);
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }

            return groupAssetIds; // Return the list of asset IDs (long).
        }


        private async Task<List<string>> ScrapeMarketplacePageAsync(string assetType, int pageNumber)
        {
            List<string> assetIds = new List<string>();

            using (HttpClient httpClient = new HttpClient())
            {
                string url = "https://catalog.roblox.com/v1/search/items";
                var parameters = new Dictionary<string, string>
        {
            { "category", "Clothing" },
            { "limit", "120" },
            { "minPrice", "5" },
            { "subcategory", assetType },
        };

                HttpResponseMessage response = await httpClient.GetAsync(url + "?" + string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(content);
                    JArray data = (JArray)json["data"];

                    foreach (var item in data)
                    {
                        string assetId = item["id"].ToString();
                        assetIds.Add(assetId);
                    }
                }
            }

            return assetIds;
        }

        private void StopScraping()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }



        private async void spaceButton15_Click(object sender, EventArgs e)
        {
            if (cts != null)
            {
                StopScraping();
            }

            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                string assetType = "";
                if (checkBox1.Checked)
                    assetType = "tshirt";
                else if (checkBox2.Checked)
                    assetType = "classic pants";

                int numPages = int.Parse(dreamTextBox1.Text);

                List<string> assetIds = new List<string>();

                for (int page = 1; page <= numPages; page++)
                {
                    if (token.IsCancellationRequested)
                    {
                        AddToConsole("> Scraping cancelled.", Color.Orange);
                        return;
                    }

                    List<string> newAssetIds = await ScrapeMarketplacePageAsync(assetType, page);
                    assetIds.AddRange(newAssetIds);
                }

                aloneProgressBar1.Minimum = 0;
                aloneProgressBar1.Maximum = assetIds.Count;
                aloneProgressBar1.Value = 0;

                foreach (string assetId in assetIds)
                {
                    if (token.IsCancellationRequested)
                    {
                        AddToConsole("> Downloading cancelled.", Color.Orange);
                        return;
                    }

                    await Task.Run(() =>
                    {
                        Bitmap img = DownloadAsset(assetId, assetType);

                        this.Invoke((MethodInvoker)delegate
                        {
                            assetPreview.Image = img;
                            assetPreview.SizeMode = PictureBoxSizeMode.StretchImage;
                            aloneProgressBar1.Value++;
                        });
                    });
                }

                AddToConsole("> All assets fetched and downloaded successfully.", Color.Green);
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }
            finally
            {
                StopScraping();
            }
        }

        private void spaceButton16_Click(object sender, EventArgs e)
        {
            StopScraping();
        }

        private void spaceButton17_Click(object sender, EventArgs e)
        {
            Changelog form2 = new Changelog();
            form2.ShowDialog();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void spaceButton1_Click_1(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("You can export the output as either a CSV, JSON, or TXT file. Do you want to continue?", "Export Option", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|Text files (*.txt)|*.txt";
                saveFileDialog.Title = "Export Console Output";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    string fileExtension = Path.GetExtension(filePath);

                    string[] lines = Outputbox.Lines;

                    if (fileExtension == ".csv")
                    {
                        string csvContent = string.Join("\n", lines);
                        File.WriteAllText(filePath, csvContent);
                        AddToConsole("> EXPORTED SUCCESSFULLY", Color.Green);
                    }
                    else if (fileExtension == ".json")
                    {
                        var jsonList = new List<object>();
                        foreach (string line in lines)
                        {
                            Color color = GetColorForLine(Array.IndexOf(lines, line));
                            jsonList.Add(new { Text = line, Color = color.Name });
                        }
                        string jsonContent = JsonConvert.SerializeObject(jsonList);
                        File.WriteAllText(filePath, jsonContent);
                        AddToConsole("> EXPORTED SUCCESSFULLY", Color.Green);
                    }
                    else if (fileExtension == ".txt")
                    {
                        string txtContent = string.Join("\n", lines);
                        File.WriteAllText(filePath, txtContent);
                        AddToConsole("> EXPORTED SUCCESSFULLY", Color.Green);
                    }
                }
            }
        }

        public class UserAgreement
        {
            public string UserName { get; set; }
            public string AgreementText { get; set; }
            public DateTime AgreementDate { get; set; }
            public string ProtectionFor { get; set; }
        }

        private void tabPage6_Click(object sender, EventArgs e)
        {

        }

        private void signButton_Click(object sender, EventArgs e)
        {
            UserAgreement userAgreement = new UserAgreement
            {
                UserName = nameTextbox.Text,
                AgreementText = richTextBox1.Text,
                AgreementDate = DateTime.Now,
                ProtectionFor = "Nickystv.com/am"
            };

            string json = JsonConvert.SerializeObject(userAgreement, Formatting.Indented);

            string legalDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LEGAL");
            if (!Directory.Exists(legalDir))
            {
                Directory.CreateDirectory(legalDir);
            }

            string jsonFilePath = Path.Combine(legalDir, "UserAgreement.json");
            File.WriteAllText(jsonFilePath, json);

            this.DialogResult = DialogResult.OK;

            spaceButton5.Enabled = true;
            spaceButton6.Enabled = true;
            spaceButton7.Enabled = true;
            spaceButton8.Enabled = true;
            spaceButton9.Enabled = true;
            spaceButton10.Enabled = true;
            nameTextbox.Enabled = false;
            signButton.Enabled = false;
            MessageBox.Show("You now have access to the SPOOFER tools.");
        }

        private void nameTextbox_TextChanged(object sender, EventArgs e)
        {

        }

        private void spaceButton18_Click(object sender, EventArgs e)
        {
            Proxy proxyForm = new Proxy();
            proxyForm.Show();
        }

        public class GroupInfo
        {
            public string GroupId { get; set; }
            public string GroupName { get; set; }
            public string OwnerStatus { get; set; }
            public bool IsLocked { get; set; }

            public bool IsGroupAsset { get; set; }
        }


        private static void CheckForMaxedOutProxyUsage(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.Forbidden) return;
            Console.WriteLine("Proxy usage limit reached. Fetch more proxies? (yes/no)");
            if (Console.ReadLine()?.ToLower() != "yes") return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://proxyscrape.com/home?ref=ndmwndm",
                UseShellExecute = true
            });
        }

        private static readonly Random RRandom = new Random();


        private async Task<GroupInfo> ScrapeRobloxGroup(string groupId, string proxyLine, int retryCount = 0)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            if (useProxy)
            {
                var proxyParts = proxyLine.Split(':');
                httpClientHandler.Proxy = new WebProxy($"{proxyParts[0]}:{proxyParts[1]}", false)
                {
                    Credentials = new NetworkCredential(proxyParts[2], proxyParts[3])
                };
            }

            using (HttpClient httpClient = new HttpClient(httpClientHandler))
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                string[] userAgents = {   "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/115.0",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/114.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:109.0) Gecko/20100101 Firefox/115.0",
    "Mozilla/5.0 (Windows NT 10.0; rv:109.0) Gecko/20100101 Firefox/115.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.67",
    "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/114.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.82",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/115.0",
    "Mozilla/5.0 (X11; Linux x86_64; rv:102.0) Gecko/20100101 Firefox/102.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.183",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:109.0) Gecko/20100101 Firefox/114.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:102.0) Gecko/20100101 Firefox/102.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36 OPR/99.0.0.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 OPR/100.0.0.0",
    "Mozilla/5.0 (Windows NT 10.0; rv:114.0) Gecko/20100101 Firefox/114.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.79",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.75 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/114.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; rv:102.0) Gecko/20100101 Firefox/102.0",
    "Mozilla/5.0 (Windows NT 10.0; WOW64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.5666.197 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.2 Safari/605.1.15",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.58",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36 Edg/114.0.1823.86",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36", };
                string randomUserAgent = userAgents[RRandom.Next(0, userAgents.Length)];
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(randomUserAgent);

                var response = await httpClient.GetAsync($"https://groups.roblox.com/v1/groups/{groupId}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var groupInfo = JObject.Parse(data);
                    bool isLocked = groupInfo["isLocked"]?.ToObject<bool>() ?? false;

                    return groupInfo["errors"] != null || groupInfo["id"] == null
                        ? new GroupInfo { GroupId = groupId, GroupName = "N/A", OwnerStatus = "Group does not exist", IsLocked = isLocked }
                        : new GroupInfo { GroupId = groupId, GroupName = groupInfo["name"].ToString(), OwnerStatus = groupInfo["owner"] == null ? "No owner" : "Has an owner", IsLocked = isLocked };
                }

                if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == (HttpStatusCode)429)
                {
                    useProxy = true;
                    await Task.Delay((int)Math.Pow(2, retryCount) * 1000 + random.Next(0, 1000));
                    return await ScrapeRobloxGroup(groupId, proxyLine, retryCount + 1);
                }

                CheckForMaxedOutProxyUsage(response);
                return new GroupInfo { GroupId = groupId, GroupName = "N/A", OwnerStatus = $"Failed to get info. Status code: {response.StatusCode}" };
            }
        }

        private bool IsGroupIdInRange(string groupId)
        {
            if (int.TryParse(groupId, out int id))
            {
                return id >= minGroupId && id <= maxGroupId;
            }
            return false;
        }
        private void AddToTreeView(GroupInfo groupInfo)
        {
          
            if (treeView1.InvokeRequired)
            {
                treeView1.Invoke(new Action(() => AddToTreeView(groupInfo)));
            }
            else
            {
                TreeNode node = new TreeNode($"Group ID: {groupInfo.GroupId} - {groupInfo.GroupName} ({groupInfo.OwnerStatus})");
                node.Tag = groupInfo.GroupId;

                // Set the image index based on the owner status
                if (groupInfo.OwnerStatus == "Has an owner")
                {
                    node.ImageIndex = 0;  // Green
                    node.SelectedImageIndex = 0;  // Green when selected
                }
                else
                {
                    node.ImageIndex = 1;  // Red
                    node.SelectedImageIndex = 1;  // Red when selected
                }

                treeView1.Nodes.Add(node);
            }
        }




        private List<string> ReadProxiesFromFile()
        {
            List<string> proxies = File.ReadAllLines("proxies.txt").ToList();
            AddToConsole("> Fetched Proxies.txt", Color.Green);
            return proxies;
        }

        private void spaceButton20_Click(object sender, EventArgs e)
        {
            isScraping = false;
            cancellationTokenSource.Cancel();
            AddToConsole("Scraping stopped by user.", Color.Red);
        }



        private async void spaceButton19_Click(object sender, EventArgs e)
        {
            spaceButton19.Enabled = false;
            treeView1.Nodes.Clear();
            scrapedGroupIds.Clear();
            cancellationTokenSource = new CancellationTokenSource();
            List<string> proxies = ReadProxiesFromFile();
            bool useProxy = false;  // Initialize the flag
            List<int> noOwnerGroups = new List<int>();  

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                _ = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        int groupId;
                        lock (lockObj)
                        {
                            do
                            {
                                groupId = random.Next(minGroupId, maxGroupId + 1);
                            } while (scrapedGroupIds.Contains(groupId));
                        }
                        foreach (string proxy in proxies)
                        {
                            if (cancellationTokenSource.Token.IsCancellationRequested) break;
                            GroupInfo groupInfo = await ScrapeRobloxGroup(groupId.ToString(), proxy, 0);
                            if (groupInfo.GroupName != "N/A")
                            {
                                lock (lockObj)
                                {
                                    if (!scrapedGroupIds.Contains(groupId))
                                    {
                                        scrapedGroupIds.Add(groupId);
                                        AddToTreeView(groupInfo);
                                        if (groupInfo.OwnerStatus == "No owner")
                                        {
                                            noOwnerGroups.Add(groupId);  // Add to the no-owner list
                                            Console.WriteLine($"Group with no owner found: {groupId}");
                                        }
                                    }
                                }
                            }
                            if (groupInfo.OwnerStatus.Contains("429") || groupInfo.OwnerStatus.Contains("403"))
                            {
                                useProxy = true;  // Switch to using proxy
                            }
                            await Task.Delay(2000);
                        }
                    }
                }, cancellationTokenSource.Token);

                await Task.Delay(1000);
            }

            spaceButton19.Enabled = true;
        }


        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Parent == null)
            {
                e.Node.Nodes.Clear();

                if (e.Node.Text.StartsWith("Group:"))
                {
                    string groupId = e.Node.Text.Split(' ')[1];
                    string groupLink = $"https://www.roblox.com/groups/{groupId}";
                    TreeNode linkNode = new TreeNode($"Group Link: {groupLink}");
                    e.Node.Nodes.Add(linkNode);

                    // Add the group link to the console
                    AddToConsole($"Group Link: {groupLink}", Color.Green);  // Replace Green with the color you want
                }
                else if (e.Node.Text.StartsWith("Asset"))
                {
                    string assetName = e.Node.Text;
                    if (e.Node.Nodes.Count > 0)
                    {
                        string assetLink = e.Node.Nodes[0].Text.Split(' ')[1];
                        TreeNode linkNode = new TreeNode($"Asset Link: {assetLink}");
                        e.Node.Nodes.Add(linkNode);

                        // Add the asset link to the console
                        AddToConsole($"Asset Link: {assetLink}", Color.Blue);  // Replace Blue with the color you want
                    }
                }
            }

            e.Node.Expand();
        }



        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            TreeNode node = this.treeView1.SelectedNode;
            if (node != null)
            {
                if (node.Parent == null)
                {
                    node.Nodes.Clear();
                    string groupId = node.Text.Split(' ')[2];
                    string groupLink = $"https://www.roblox.com/groups/{groupId}";
                    TreeNode linkNode = new TreeNode($"Group Link: {groupLink}");
                    node.Nodes.Add(linkNode);
                }
                else if (node.Parent != null && node.Nodes.Count > 0)
                {
                    string groupLink = node.Nodes[0].Text.Substring(12);
                    System.Diagnostics.Process.Start(groupLink);
                }
                node.Expand();
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            string discordLink = "https://discord.gg/faJRE4pAAE";
            Process.Start(new ProcessStartInfo
            {
                FileName = discordLink,
                UseShellExecute = true
            });
        }

        private void parrotGroupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox5_CheckedChanged(object sender)
        {

        }

        private void dreamTextBox4_TextChanged(object sender, EventArgs e)
        {
            string currentText = dreamTextBox4.Text;
            List<string> savedUsernames = ReadUsernamesFromCSV();

            foreach (string username in savedUsernames)
            {
                if (username.StartsWith(currentText, StringComparison.OrdinalIgnoreCase))
                {
                    dreamTextBox4.Text = username;
                    dreamTextBox4.SelectionStart = currentText.Length;
                    dreamTextBox4.SelectionLength = username.Length - currentText.Length;
                    break;
                }
            }
        }

        private List<string> ReadUsernamesFromCSV()
        {
            List<string> usernames = new List<string>();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            if (exeDirectory == null)
            {
                AddToConsole("> Failed to get the directory path.", Color.Red);
                return usernames;
            }
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            if (File.Exists(csvFilePath))
            {
                using (StreamReader sr = new StreamReader(csvFilePath))
                {
                    string line;
                    bool skipHeader = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (skipHeader)
                        {
                            skipHeader = false;
                            continue;
                        }
                        string[] values = line.Split(',');
                        string username = values[0];
                        usernames.Add(username);
                    }
                }
            }
            else
            {
                AddToConsole("> RobloxAccounts.csv File Does Not Exist.", Color.Red);
            }
            return usernames;
        }

        private async void spaceButton24_Click(object sender, EventArgs e)
        {
            string typedUsername = dreamTextBox4.Text;

            Dictionary<string, string> accounts = ReadAccountsFromCSV();

            if (accounts.ContainsKey(typedUsername))
            {
                string selectedCookie = accounts[typedUsername];

                var (isValid, returnedUsername) = await ValidateCookie(selectedCookie);

                if (isValid)
                {
                    AddToConsole($"> Account {typedUsername} is valid and selected for operations.", Color.Green);
                    foreverLabel33.Text = typedUsername; 
                }
                else
                {
                    AddToConsole($"> Account {typedUsername} is not valid.", Color.Red);
                }
            }
            else
            {
                AddToConsole("> Typed username does not exist in saved accounts.", Color.Red);
            }
        }


        private Dictionary<string, string> ReadAccountsFromCSV()
        {
            Dictionary<string, string> accounts = new Dictionary<string, string>();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            if (exeDirectory == null)
            {
                AddToConsole("> Failed to get the directory path.", Color.Red);
                return accounts;
            }
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            if (File.Exists(csvFilePath))
            {
                using (StreamReader sr = new StreamReader(csvFilePath))
                {
                    string line;
                    bool skipHeader = true;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (skipHeader)
                        {
                            skipHeader = false;
                            continue;
                        }
                        string[] values = line.Split(',');
                        string username = values[0];
                        string cookie = values[1];
                        accounts[username] = cookie;
                    }
                }
            }
            else
            {
                AddToConsole("> RobloxAccounts.csv File Does Not Exist.", Color.Red);
            }
            return accounts;
        }

        private async void spaceButton23_Click(object sender, EventArgs e)
        {
            try
            {
                cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;

                string selectedUsername = foreverLabel33.Text;
                if (string.IsNullOrEmpty(selectedUsername))
                {
                    AddToConsole("> No account selected for operations.", Color.Red);
                    return;
                }

                Dictionary<string, string> accounts = ReadAccountsFromCSV();
                if (!accounts.TryGetValue(selectedUsername, out string selectedCookie))
                {
                    AddToConsole("> Cookie not found for the selected account.", Color.Red);
                    return;
                }

                if (!csrfTokens.TryGetValue(selectedCookie, out string csrfToken))
                {
                    AddToConsole("> CSRF token not found for the selected account.", Color.Red);
                    return;
                }

                List<string> proxies = ReadProxiesFromFile();
                int proxyIndex = 0;

                HashSet<string> seenItems = new HashSet<string>();

                string proxyLine = proxies[proxyIndex];
                Dictionary<string, Tuple<long, string, string>> items = await FetchItems(httpClient, token, proxyLine);
                if (items.Count == 0)
                {
                    AddToConsole("> No items found.", Color.Yellow);
                }

                foreach (var item in items)
                {
                    if (token.IsCancellationRequested)
                    {
                        AddToConsole("> Scraping operation cancelled.", Color.Yellow);
                        return;
                    }

                    if (seenItems.Contains(item.Key))
                    {
                        continue;
                    }

                    seenItems.Add(item.Key);
                    AddToConsole("PURCHASE", Color.Pink);
                    await Purchase(httpClient, item.Key, item.Value.Item1, csrfToken, selectedCookie, proxyLine, selectedUsername);
                    AddToConsole($"Item Link: {item.Value.Item2}", Color.Blue);

                    proxyIndex = (proxyIndex + 1) % proxies.Count;
                    proxyLine = proxies[proxyIndex];
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }
        }



        private void spaceButton22_Click(object sender, EventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
                AddToConsole("> Scraping operation cancelled.", Color.Yellow);
            }
        }

        private async void dreamTextBox2_Enter(object sender, EventArgs e)
        {
            string userInput = dreamTextBox2.Text;
            bool isUsername = !IsNumeric(userInput);
            await FetchAndDisplayFriends(userInput, isUsername);
        }
        private async void spaceButton25_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The form may freeze for a moment while the assets are downloading.");

            if (cts != null)
            {
                StopScraping();
            }

            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                if (long.TryParse(dreamTextBox5.Text, out long groupId)) // Changed int to long here.
                {
                    AddToConsole($"> Scraping assets from Group ID: {groupId}...", Color.Gray);

                    List<long> assetIds = await FetchAndDownloadGroupAssets(groupId);

                    aloneProgressBar1.Minimum = 0;
                    aloneProgressBar1.Maximum = assetIds.Count;
                    aloneProgressBar1.Value = 0;

                    foreach (long assetId in assetIds) // Changed int to long here.
                    {
                        if (token.IsCancellationRequested)
                        {
                            AddToConsole("> Downloading cancelled.", Color.Orange);
                            return;
                        }

                        await Task.Run(() =>
                        {
                            Bitmap img = DownloadAsset(assetId.ToString(), "group");

                            this.Invoke((MethodInvoker)delegate
                            {
                                assetPreview.Image = img;
                                assetPreview.SizeMode = PictureBoxSizeMode.StretchImage;
                                aloneProgressBar1.Value++;
                            });
                        });
                    }

                    AddToConsole($"> All assets from Group ID {groupId} fetched and downloaded successfully.", Color.Green);
                }
                else
                {
                    AddToConsole("Please enter a valid Group ID in dreamTextBox5.", Color.Red);
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
            }
            finally
            {
                StopScraping();
            }
        }

        private void spaceButton26_Click(object sender, EventArgs e)
        {
            StopScraping();
        }
    }
}