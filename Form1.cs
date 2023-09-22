using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using JsonException = Newtonsoft.Json.JsonException;
using System.Text.RegularExpressions;
using System.Net;
using System.Windows.Forms;
using System.Collections.Generic;


namespace Account_Manager
{
    public partial class Form1 : Form
    {
        private int scrollPosition = 0;
        private List<ListViewItem> originalItems = new List<ListViewItem>();

        public Form1()
        {
            // > Call Functions 
            InitializeComponent();
            checkCSV();
            LoadUsernamesFromCSV(dashboardListView);
            LoadUsernamesFromCSV(cookieListView);
            LoadUsernamesFromCSV(accountDetailList);
            UpdateDashboardMetrics();
            UpdateVersionLabel();
            SetAutoScaleModeBasedOnDPI();
            AddToConsole("> Executed Everything in Form() load...", Color.Green);
            CheckForUpdates();

            cookieListView.AllowDrop = true;
            cookieListView.DragEnter += new DragEventHandler(Form_DragEnter);
            cookieListView.DragDrop += new DragEventHandler(Form_DragDrop);


            foreach (ListViewItem item in dashboardListView.Items)
            {
                originalItems.Add((ListViewItem)item.Clone());
            }
        }

        public void checkCSV()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? exeDirectory = Path.GetDirectoryName(exePath);

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

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension == ".csv")
                {
                    ParseCSV(file);
                }
                else if (extension == ".json")
                {
                    ParseJSON(file);
                }
            }
        }

        private void ParseCSV(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        ValidateCookie(line);
                        AddToConsole("> Validated Cookie Successfully", Color.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"An error occurred while reading the CSV file: {ex.Message}", Color.Red);
            }
        }
        private void ParseJSON(string filePath)
        {

        }


        public void AddToConsole(string message, Color color)
        {
            if (materialRichTextBox1.InvokeRequired)
            {
                materialRichTextBox1.Invoke(new Action(() => AddToConsole(message, color)));
                return;
            }

            materialRichTextBox1.SelectionStart = materialRichTextBox1.TextLength;
            materialRichTextBox1.SelectionLength = 0;

            materialRichTextBox1.SelectionColor = color;
            materialRichTextBox1.AppendText(message + "\n");
            materialRichTextBox1.SelectionColor = materialRichTextBox1.ForeColor;
        }

        public async Task<(bool, string)> ValidateCookie(string cookie)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", ".ROBLOSECURITY=" + cookie);
                    HttpResponseMessage response = await httpClient.GetAsync("https://www.roblox.com/mobileapi/userinfo");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        JObject json = JObject.Parse(responseBody);

                        if (json.ContainsKey("UserName") && json.ContainsKey("UserID"))
                        {
                            string username = json["UserName"].ToString();
                            string userId = json["UserID"].ToString();
                            string notes = "";
                            SaveToCSV(username, cookie, userId, notes);

                            return (true, username);
                        }
                        else
                        {
                            return (false, "Invalid JSON response: Missing UserName or UserID");
                        }
                    }
                    else
                    {
                        return (false, $"HTTP Error: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException e)
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

        //Save2CSV
        public void SaveToCSV(string username, string cookie, string userId, string notes)
        {
            string csvFilePath = Path.Combine(Directory.GetCurrentDirectory(), "RobloxAccounts.csv");
            List<string> lines = File.ReadAllLines(csvFilePath).ToList();

            bool userExists = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts[0] == username)
                {
                    string existingNotes = parts.Length > 3 ? parts[3] : "";
                    string newNotes = string.IsNullOrEmpty(notes) ? existingNotes : notes;
                    lines[i] = $"{username},{cookie},{userId},{newNotes}";
                    userExists = true;
                    break;
                }
            }

            if (!userExists)
            {
                lines.Add($"{username},{cookie},{userId},{notes}");
            }

            File.WriteAllLines(csvFilePath, lines);
            AddToConsole("> Account details updated successfully.", Color.Green);
        }


        //LoadUsernames
        public void LoadUsernamesFromCSV(ListView targetListView)
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
                MessageBox.Show("Refresh clicked");
            };
            contextMenuStrip.Items.Add(deleteMenuItem);
            contextMenuStrip.Items.Add(refreshMenuItem);
            targetListView.ContextMenuStrip = contextMenuStrip;

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? exeDirectory = Path.GetDirectoryName(exePath);
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
                        string notes = values.Length > 3 ? values[3] : "";
                        ListViewItem item = new ListViewItem(username);
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
            string? exeDirectory = Path.GetDirectoryName(exePath);
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            List<string> lines = File.ReadAllLines(csvFilePath).ToList();
            lines.RemoveAll(line => line.StartsWith(usernameToRemove + ","));
            File.WriteAllLines(csvFilePath, lines);
            AddToConsole("> Selected account deleted", Color.Green);
        }

        private void airCheckBox1_CheckedChanged(object sender)
        {

#pragma warning disable CA1416
            if (airCheckBox1.Checked)
            {
                this.TopMost = true;
                AddToConsole("> TopMost = true;", Color.Black);
            }
            else
            {
                this.TopMost = false;
                AddToConsole("> TopMost = false;", Color.Black);
            }
#pragma warning restore CA1416
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string cookie = saveCookieTxt.Text;
            var (isValid, result) = await ValidateCookie(cookie);

            if (isValid)
            {
                AddToConsole("> Cookie is valid.", Color.Green);
                ListViewItem item = new ListViewItem(result);
                cookieListView.Items.Add(item);
            }
            else
            {
                AddToConsole($"> Cookie is invalid. Reason: {result}", Color.Red);
            }
        }

        private void airButton1_Click(object sender, EventArgs e)
        {
            airTabPage1.SelectedTab = tabPage3;
            saveCookieTxt.Focus();
            button2.BackColor = Color.LightBlue;
        }

        private async void accountDetailList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (accountDetailList.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = accountDetailList.SelectedItems[0];
                string selectedUsername = selectedItem.Text;

                string csvFilePath = "RobloxAccounts.csv";
                List<string> lines = new List<string>(File.ReadAllLines(csvFilePath));
                for (int i = 0; i < lines.Count; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts[0] == selectedUsername)
                    {
                        usernameLabel.Text = parts[0];
                        robloxIDlabel.Text = parts[2];
                        cookieLabel.Text = parts[1];

                        if (parts.Length > 3)
                        {
                            notesBox.Text = parts[3];
                        }
                        else
                        {
                            notesBox.Text = "";
                        }

                        var (isValid, _) = await ValidateCookie(parts[1]);
                        if (isValid)
                        {
                            statusLabel.Text = "Verified";
                            statusLabel.ForeColor = Color.Green;
                        }
                        else
                        {
                            statusLabel.Text = "Unverified";
                            statusLabel.ForeColor = Color.Red;

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

                                        cookieLabel.Text = newCookie;

                                        statusLabel.Text = "Verified";
                                        statusLabel.ForeColor = Color.Green;
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


        private void airButton5_Click(object sender, EventArgs e)
        {
            accountDetailList.Clear();
            LoadUsernamesFromCSV(accountDetailList);
            AddToConsole("> Refreshed Account Details List...", Color.HotPink);
        }
        private void airButton2_Click(object sender, EventArgs e)
        {
            dashboardListView.Clear();
            LoadUsernamesFromCSV(dashboardListView);
            AddToConsole("> Refreshed Dashboard List...", Color.HotPink);
            UpdateDashboardMetrics();
        }

        private async void UpdateDashboardMetrics()
        {
            int totalAccounts = 0;
            int activeAccounts = 0;

            string csvFilePath = "RobloxAccounts.csv";
            if (File.Exists(csvFilePath))
            {
                string[] lines = File.ReadAllLines(csvFilePath);
                totalAccounts = lines.Length - 1;

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(',');
                    string cookie = parts[1];

                    var (isValid, _) = await ValidateCookie(cookie);
                    if (isValid)
                    {
                        activeAccounts++;
                    }
                }
            }

            crownLabel2.Text = $"{totalAccounts}";
            crownLabel3.Text = $"{activeAccounts}";
            AddToConsole("> Fetched Dashboard Metrics.", Color.Black);
        }


        private void cookieLabel_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(cookieLabel.Text);
            AddToConsole("> Copied cookie to clipboard", Color.Green);
        }

        private void saveNotesBTN_Click(object sender, EventArgs e)
        {
            if (accountDetailList.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = accountDetailList.SelectedItems[0];
                string selectedUsername = selectedItem.Text;

                string csvFilePath = "RobloxAccounts.csv";
                List<string> lines = new List<string>(File.ReadAllLines(csvFilePath));

                for (int i = 0; i < lines.Count; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts[0] == selectedUsername)
                    {
                        string newNotes = notesBox.Text;
                        lines[i] = $"{parts[0]},{parts[1]},{parts[2]},{newNotes}";

                        File.WriteAllLines(csvFilePath, lines);

                        AddToConsole("> Notes saved successfully.", Color.Green);
                        break;
                    }
                }
            }
            else
            {
                AddToConsole("> No account selected. Cannot save notes.", Color.Red);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (accountDetailList.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = accountDetailList.SelectedItems[0];
                string selectedUsername = selectedItem.Text;

                string csvFilePath = "RobloxAccounts.csv";
                string[] lines = File.ReadAllLines(csvFilePath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(',');
                    if (parts[0] == selectedUsername)
                    {
                        DialogResult dialogResult = MessageBox.Show("Would you like to export as a CSV or JSON file? (YES = CSV) (NO = JSON)", "Export Options", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, 0, false);

                        if (dialogResult == DialogResult.Yes)
                        {
                            string exportData = $"{parts[0]},{parts[1]},{parts[2]},{parts[3]}";
                            File.WriteAllText($"Export_{selectedUsername}.csv", "Username,Cookie,RobloxID,Notes\n" + exportData);
                            AddToConsole($"> Exported details of {selectedUsername} to Export_{selectedUsername}.csv", Color.Green);
                        }
                        else if (dialogResult == DialogResult.No)
                        {
                            var exportData = new
                            {
                                Username = parts[0],
                                Cookie = parts[1],
                                RobloxID = parts[2],
                                Notes = parts[3]
                            };

                            string jsonExportData = Newtonsoft.Json.JsonConvert.SerializeObject(exportData);
                            File.WriteAllText($"Export_{selectedUsername}.json", jsonExportData);
                            AddToConsole($"> Exported details of {selectedUsername} to Export_{selectedUsername}.json", Color.Green);
                        }
                        else
                        {
                            AddToConsole("> Export cancelled.", Color.Red);
                        }

                        break;
                    }
                }
            }
            else
            {
                AddToConsole("> No account selected. Cannot export details.", Color.Red);
            }
        }

        private void airButton4_Click(object sender, EventArgs e)
        {
            notesBox.Text = "";
            if (accountDetailList.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = accountDetailList.SelectedItems[0];
                string selectedUsername = selectedItem.Text;

                string csvFilePath = "RobloxAccounts.csv";
                List<string> lines = new List<string>(File.ReadAllLines(csvFilePath));

                for (int i = 0; i < lines.Count; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts[0] == selectedUsername)
                    {
                        string newNotes = notesBox.Text;
                        lines[i] = $"{parts[0]},{parts[1]},{parts[2]},{newNotes}";

                        File.WriteAllLines(csvFilePath, lines);

                        AddToConsole("> Cleared Notes.", Color.Green);
                        break;
                    }
                }
            }
            else
            {
                AddToConsole("> No account selected. Cannot save notes.", Color.Red);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private async void UpdateVersionLabel()
        {
            string latestVersionUrl = "https://nickystv.com/version/version.txt";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true
                };

                try
                {
                    string latestVersion = await httpClient.GetStringAsync($"{latestVersionUrl}?nocache={Guid.NewGuid()}").ConfigureAwait(false);

                    nightLabel17.Text = latestVersion.Trim();
                    AddToConsole("> Fetched Latest Version..", Color.Black);
                }
                catch (Exception ex)
                {
                    nightLabel17.Text = "Error fetching version";
                }
            }
        }




        private async Task CheckForUpdates()  
        {
            string latestVersionUrl = "https://nickystv.com/version/version.txt";
            string changelogUrl = "https://nickystv.com/version/changelog.txt";
            string newVersionDownloadUrl = "https://nickystv.com/version/AccountManager.exe";
            string newDllUrl = "https://nickystv.com/version/AccountManager.dll";
            string currentVersion = "1.1.3";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true
                };

                try
                {
                    string latestVersion = await httpClient.GetStringAsync($"{latestVersionUrl}?nocache={Guid.NewGuid()}");
                    string changelog = await httpClient.GetStringAsync($"{changelogUrl}?nocache={Guid.NewGuid()}");

                    string changelogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "changelog.txt");
                    await File.WriteAllTextAsync(changelogFilePath, changelog);

                    string latestChangelog = GetLatestChangelogEntry(changelog);

                    if (string.IsNullOrEmpty(latestVersion))
                    {
                        AddToConsole("> No Update information is available at this time.", Color.Pink);
                        airTabPage1.SelectedTab = tabPage6;
                        return;
                    }

                    if (latestVersion != currentVersion)
                    {
                        DialogResult dialogResult = MessageBox.Show($"New version available!\n\nLatest Changelog:\n{latestChangelog}\n\nDo you want to update now?", "Update Available", MessageBoxButtons.YesNo);

                        if (dialogResult == DialogResult.Yes)
                        {
                            string tempExeFilePath = Path.GetTempFileName();
                            string tempDllFilePath = Path.GetTempFileName();

                            using (var fs = new FileStream(tempExeFilePath, FileMode.Create))
                            {
                                var stream = await httpClient.GetStreamAsync($"{newVersionDownloadUrl}?nocache={Guid.NewGuid()}");
                                await stream.CopyToAsync(fs);
                            }

                            using (var fs = new FileStream(tempDllFilePath, FileMode.Create))
                            {
                                var stream = await httpClient.GetStreamAsync($"{newDllUrl}?nocache={Guid.NewGuid()}");
                                await stream.CopyToAsync(fs);
                            }

                            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                            if (File.Exists($"{currentDirectory}\\AccountManager.exe"))
                            {
                                File.Delete($"{currentDirectory}\\AccountManager.exe");
                            }
                            if (File.Exists($"{currentDirectory}\\AccountManager.dll"))
                            {
                                File.Delete($"{currentDirectory}\\AccountManager.dll");
                            }

                            if (File.Exists(tempExeFilePath))
                            {
                                File.Move(tempExeFilePath, $"{currentDirectory}\\AccountManager.exe");
                            }
                            if (File.Exists(tempDllFilePath))
                            {
                                File.Move(tempDllFilePath, $"{currentDirectory}\\AccountManager.dll");
                            }

                            string updateScript =
        $@"@echo off
echo Updating please be patient...
timeout /t 2 /nobreak
echo Deleting old version...
del /f /q ""%~dp0\AccountManager.exe""
del /f /q ""%~dp0\AccountManager.dll""
echo Moving new version...
move /y ""%TEMP%\AccountManager_new.exe"" ""%~dp0\AccountManager.exe""
move /y ""%TEMP%\AccountManager_new.dll"" ""%~dp0\AccountManager.dll""
echo Setting permissions...
icacls ""%~dp0\AccountManager.exe"" /grant Everyone:F
icacls ""%~dp0\AccountManager.dll"" /grant Everyone:F
echo Starting new version...
start """" ""%~dp0\AccountManager.exe""
echo Update complete.
exit";

                            string batchFilePath = Path.Combine(currentDirectory, "update.bat");
                            File.WriteAllText(batchFilePath, updateScript);

                            Process.Start(new ProcessStartInfo(batchFilePath) { CreateNoWindow = true, UseShellExecute = true, Verb = "runas" });
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        AddToConsole("> No Update is available at this time.", Color.Pink);
                        airTabPage1.SelectedTab = tabPage6;
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex}", Color.Red);
                }
            }
        }

        private string GetLatestChangelogEntry(string changelog)
        {
            string[] entries = changelog.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            string latestEntry = "No changelog available";

            for (int i = 0; i < entries.Length; i += 2)
            {
                if (i + 1 < entries.Length)
                {
                    latestEntry = entries[i + 1].Trim();
                    break;
                }
            }

            return latestEntry;
        }

        private void nightLabel16_Click(object sender, EventArgs e)
        {

        }

        private void airForm1_Click(object sender, EventArgs e)
        {

        }

        private void airSeparator2_Click(object sender, EventArgs e)
        {

        }

        private void airButton6_Click(object sender, EventArgs e)
        {

        }

        private void airSeparator3_Click(object sender, EventArgs e)
        {

        }

        private void nightLabel20_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nightLabel20.Text);
            AddToConsole("> Copied ID to clipboard", Color.Green);

        }

        private async void button9_Click(object sender, EventArgs e)
        {
            string userInput = textBox1.Text;
            await FetchAndDisplayFriends(userInput);
        }


        private async Task FetchAndDisplayFriends(string identifier, bool isUsername = false)
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

        private async void button10_Click(object sender, EventArgs e)
        {
            string userId = nightLabel20.Text;
            await FetchAndDisplayFriends(userId);
        }


        private void friendsListView_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (friendsListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = friendsListView.SelectedItems[0];
                string selectedFriendName = selectedItem.SubItems[0].Text;
                string selectedFriendId = selectedItem.SubItems[1].Text;
                string selectedFriendAvatarUrl = selectedItem.SubItems[2].Text;

                AddToConsole("> Avatar URL: " + selectedFriendAvatarUrl, Color.Blue);

                nightLabel19.Text = selectedFriendName;
                nightLabel20.Text = selectedFriendId;

                try
                {
                    if (!string.IsNullOrEmpty(selectedFriendAvatarUrl) && selectedFriendAvatarUrl != "Unknown")
                    {
                        hopePictureBox1.Load(selectedFriendAvatarUrl);
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

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void nightLabel19_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nightLabel19.Text);
            AddToConsole("> Copied Username to clipboard", Color.Green);
        }

        private void assetType_Click(object sender, EventArgs e)
        {

        }

        private void foreverToggle1_CheckedChanged(object sender)
        {

        }

        private void createFolder_Click(object sender, EventArgs e)
        {
            string folderName = "Assets";

            string pathString = Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (!Directory.Exists(pathString))
            {
                Directory.CreateDirectory(pathString);
                AddToConsole("> Assets folder created successfully.", Color.Black);
            }
            else
            {
                AddToConsole("> Assets folder already exists.", Color.Black);
            }
        }

        private void fetchAsset_Click(object sender, EventArgs e)
        {
            string assetId = assetTextbox.Text;

            // Fetch asset preview
            string assetPreviewUrl = $"https://thumbnails.roblox.com/v1/assets?assetIds={assetId}&returnPolicy=PlaceHolder&size=30x30&format=Png&isCircular=false";

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    string jsonData = webClient.DownloadString(assetPreviewUrl);
                    JObject jsonObject = JObject.Parse(jsonData);
                    string imageUrl = jsonObject["data"][0]["imageUrl"].ToString();
                    imageUrl = imageUrl.Replace("30/30", "420/420");

                    byte[] imageData = webClient.DownloadData(imageUrl);
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        assetPreview.Image = Image.FromStream(ms);
                    }

                    string assetInfoUrl = $"https://api.roblox.com/marketplace/productinfo?assetId={assetId}";
                    string assetInfoData = webClient.DownloadString(assetInfoUrl);
                    JObject assetInfoObject = JObject.Parse(assetInfoData);
                    string creatorName = assetInfoObject["Creator"]["Name"].ToString();
                    string assetType = assetInfoObject["AssetTypeId"].ToString();

                    AddToConsole($"Creator: {creatorName}", Color.Black);
                    AddToConsole($"Asset Type: {assetType}", Color.Black);
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex.Message}", Color.Red);
                }
            }
        }

        private void assetDownload_Click(object sender, EventArgs e)
        {
            string assetId = assetTextbox.Text;
            string downloadUrl = $"https://assetdelivery.roblox.com/v1/asset?id={assetId}";

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    string pathString = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
                    string filePath = Path.Combine(pathString, $"{assetId}.rbxm"); 

                    webClient.DownloadFile(downloadUrl, filePath);
                    AddToConsole("> Asset downloaded successfully.", Color.Black);
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex.Message}", Color.Red);
                }
            }
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void assetTextbox_TextChanged(object sender, EventArgs e)
        {

        }

        private async void submitSuggestion_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(suggestionTitle.Text) || suggestionTitle.Text.Length < 5)
            {
                AddToConsole("> Suggestion Title must be at least 5 characters long.", Color.Red);
                return;
            }

            if (string.IsNullOrEmpty(suggestionInput.Text) || suggestionInput.Text.Length < 10)
            {
                AddToConsole("> Suggestion must be at least 10 characters long.", Color.Red);
                return;
            }

            var payload = new
            {
                content = "",
                embeds = new[]
                {
            new
            {
                title = $"Suggestion: {suggestionTitle.Text}",
                description = suggestionInput.Text
            }
        }
            };

            string payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            using (HttpClient httpClient = new HttpClient())
            {
                var content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync(
                        "https://discord.com/api/webhooks/1148608476088643725/cqs19Xmf5DAD3mszT5O6H7ISsdF2DxXIeGoT_h1H1GX2CgvZlYmIeup7H_huoHsDxjLP",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        AddToConsole("> Suggestion sent successfully!", Color.Green);
                        airTabPage1.SelectedTab = tabPage6;
                    }
                    else
                    {
                        AddToConsole($"> Failed to send suggestion. Error code: {response.StatusCode}", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    AddToConsole($"An error occurred: {ex.Message}", Color.Red);
                }
            }
        }

        public void SetAutoScaleModeBasedOnDPI()
        {
            float dpiX, dpiY;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }

            if (dpiX > 96 || dpiY > 96)
            {
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            }
            else
            {
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            }
        }

        private void aloneTextBox1_TextChanged(object sender, EventArgs e)
        {
            string query = aloneTextBox1.Text.ToLower();

            dashboardListView.Items.Clear();

            foreach (ListViewItem item in originalItems)
            {
                if (item.Text.ToLower().Contains(query))
                {
                    dashboardListView.Items.Add((ListViewItem)item.Clone());
                }
            }
        }

        private void dashboardListView_SelectedIndexChanged(object sender, EventArgs e)
        {


        }
    }
}
