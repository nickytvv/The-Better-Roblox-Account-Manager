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
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Account_Manager
{
    public partial class Form1 : Form
    {
        private int scrollPosition = 0;
        private List<ListViewItem> originalItems = new List<ListViewItem>();

        private string originalMachineGuid;
        private string originalDisplayId;
        private Dictionary<string, string> originalMACAddresses = new Dictionary<string, string>();
        private string originalHardwareGUID;
        private string originalMachineGUID;
        private string originalMachineId;
        private string originalBIOSReleaseDate;
        private string originalComputerName;

        private static readonly object syncLock = new object();
        private static readonly Random random = new Random();
        private const string ScsiPortsKey = "HARDWARE\\DEVICEMAP\\Scsi";

        public Form1()
        {
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
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

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
            catch (Exception ex)
            {
                MessageBox.Show("Restart the program..");
            }
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

            ToolStripMenuItem launchInBrowserMenuItem = new ToolStripMenuItem("Launch in Browser");
            launchInBrowserMenuItem.Click += async (sender, e) =>
            {
                if (targetListView.SelectedItems.Count > 0)
                {
                    ListViewItem selectedItem = targetListView.SelectedItems[0];
                    string selectedUsername = selectedItem.Text;
                    string cookie = selectedItem.SubItems[1].Text; // Assuming the cookie is stored in the second column

                    var (isValid, username) = await ValidateCookie(cookie);
                    if (isValid)
                    {
                        // TODO: Code to launch the browser and log in using the cookie
                    }
                    else
                    {
                        MessageBox.Show($"Failed to validate cookie for {selectedUsername}");
                    }
                }
            };

            contextMenuStrip.Items.Add(deleteMenuItem);
            contextMenuStrip.Items.Add(refreshMenuItem);
            contextMenuStrip.Items.Add(launchInBrowserMenuItem);
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
                        string cookie = values[1]; // Assuming the cookie is the second value in the CSV
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
            string currentVersion = "1.1.8";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true, MustRevalidate = true };
                string latestVersion = await httpClient.GetStringAsync($"{latestVersionUrl}?nocache={Guid.NewGuid()}");
                string changelog = await httpClient.GetStringAsync($"{changelogUrl}?nocache={Guid.NewGuid()}");
                string latestChangelog = GetLatestChangelogEntry(changelog, latestVersion);

                if (latestVersion != currentVersion)
                {
                    DialogResult dialogResult = MessageBox.Show($"New version available!\n\nLatest Changelog:\n{latestChangelog}\n\nDo you want to update now?", "Update Available", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        string batchContent = @"
@echo off
echo Updating please be patient...
timeout /t 2 /nobreak

setlocal
set ""TempDir=%TEMP%""
set ""BatchFileDir=%~dp0""

echo Deleting old version...
del /f /q ""%BatchFileDir%AccountManager.exe""
del /f /q ""%BatchFileDir%AccountManager.dll""

echo Downloading new version...
powershell -Command ""Invoke-WebRequest -Uri 'https://nickystv.com/version/AccountManager.exe' -OutFile '%TempDir%\AccountManager_new.exe'""
powershell -Command ""Invoke-WebRequest -Uri 'https://nickystv.com/version/AccountManager.dll' -OutFile '%TempDir%\AccountManager_new.dll'""
powershell -Command ""Invoke-WebRequest -Uri 'https://nickystv.com/version/changelog.txt' -OutFile '%BatchFileDir%\changelog.txt'""

echo Moving new version...
move /y ""%TempDir%\AccountManager_new.exe"" ""%BatchFileDir%AccountManager.exe""
move /y ""%TempDir%\AccountManager_new.dll"" ""%BatchFileDir%AccountManager.dll""

echo Setting permissions...
icacls ""%BatchFileDir%AccountManager.exe"" /grant Everyone:F
icacls ""%BatchFileDir%AccountManager.dll"" /grant Everyone:F

echo Starting new version...
start """" ""%BatchFileDir%AccountManager.exe""

echo Update complete.
exit
";

                        string batchFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");
                        File.WriteAllText(batchFilePath, batchContent);

                        Process.Start(new ProcessStartInfo(batchFilePath) { CreateNoWindow = true, UseShellExecute = true, Verb = "runas" });
                        Environment.Exit(0);
                    }
                }
                else
                {
                    AddToConsole("> No Update Found...", Color.HotPink);
                }
            }
        }


        private string GetLatestChangelogEntry(string changelog, string targetVersion)
        {
            string[] entries = changelog.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            string latestEntry = "No changelog available for version " + targetVersion;

            for (int i = 0; i < entries.Length; i += 2)
            {
                if (i + 1 < entries.Length)
                {
                    string version = entries[i].Trim();
                    if (version == targetVersion)
                    {
                        latestEntry = entries[i + 1].Trim();
                        break;
                    }
                }
            }

            return latestEntry;
        }



        private void nightLabel20_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nightLabel20.Text);
            AddToConsole("> Copied ID to clipboard", Color.Green);

        }

        private async void button9_Click(object sender, EventArgs e)
        {
            string userInput = textBox1.Text;
            bool isUsername = !IsNumeric(userInput);
            await FetchAndDisplayFriends(userInput, isUsername);
        }

        private bool IsNumeric(string input)
        {
            return long.TryParse(input, out _);
        }


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

        private async void button10_Click(object sender, EventArgs e)
        {
            string userId = nightLabel20.Text;
            bool isUsername = !IsNumeric(userId);
            await FetchAndDisplayFriends(userId, isUsername);
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


        private void nightLabel19_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(nightLabel19.Text);
            AddToConsole("> Copied Username to clipboard", Color.Green);
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

        private async Task SaveLogsAsync(string id, string logBefore, string logAfter)
        {
            try
            {
                string logsFolderPath = Path.Combine(Application.StartupPath, "Logs");
                if (!Directory.Exists(logsFolderPath))
                    Directory.CreateDirectory(logsFolderPath);

                string logFileName = Path.Combine(logsFolderPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                string logEntryBefore = $"{DateTime.Now:HH:mm:ss}: ID {id} -  {logBefore} (Before)";
                string logEntryAfter = $"{DateTime.Now:HH:mm:ss}: ID {id} -  {logAfter} (After)";

                await File.AppendAllTextAsync(logFileName, logEntryBefore + Environment.NewLine);
                await File.AppendAllTextAsync(logFileName, logEntryAfter + Environment.NewLine);

                AddToConsole(logEntryBefore + logEntryAfter, Color.Green);
            }
            catch (Exception ex)
            {
                AddToConsole($"SaveLogs error: {ex.Message}", Color.Red);
            }
        }

        private readonly List<string> diskNames = new List<string>()
{
    "Samsung SSD 870 QVO 1TB",
    "NVMe KINGSTON SA2000M2105",
    "Crucial MX500 1TB",
    "WD Blue 2TB",
    "Seagate Barracuda 4TB",
    "Intel 660p 1TB",
    "SanDisk Ultra 3D 2TB",
    "Toshiba X300 6TB",
    "Adata XPG SX8200 Pro 1TB",
    "HP EX920 512GB",
    "Kingston A2000 500GB",
    "Corsair MP600 2TB",
    "Western Digital Black 6TB",
    "Crucial P1 1TB",
    "Seagate FireCuda 2TB",
    "Samsung 970 EVO Plus 1TB",
    "ADATA Swordfish 500GB",
    "Toshiba N300 8TB",
    "WD Red Pro 10TB",
    "Kingston KC600 256GB",
};
        public static string RandomId(int length)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder result = new StringBuilder(length);

            lock (syncLock)
            {
                for (int i = 0; i < length; i++)
                {
                    result.Append(chars[random.Next(chars.Length)]);
                }
            }

            return result.ToString();
        }

        private string GetRandomDiskName()
        {
            string diskName = "";

            lock (syncLock)
            {
                int index = random.Next(diskNames.Count);
                diskName = diskNames[index];
            }

            return diskName;
        }


        private async void airButton6_Click_1(object sender, EventArgs e)
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

        private async Task ProcessScsiPortsAsync()
        {
            try
            {
                AddToConsole("Entering ProcessScsiPorts...", Color.Blue);
                using (RegistryKey ScsiPorts = Registry.LocalMachine.OpenSubKey(ScsiPortsKey))
                {
                    if (ScsiPorts == null)
                    {
                        AddToConsole("ScsiPorts key not found.", Color.Red);
                        return;
                    }

                    foreach (string port in ScsiPorts.GetSubKeyNames())
                    {
                        await ProcessScsiBusesAsync(port);
                    }
                }
                AddToConsole("Exiting ProcessScsiPorts...", Color.Blue);
            }
            catch (Exception ex)
            {
                AddToConsole($"ScsiPorts error: {ex.Message}", Color.Red);
            }
        }

        private async Task ProcessScsiBusesAsync(string port)
        {
            try
            {
                using (RegistryKey ScsiBuses = Registry.LocalMachine.OpenSubKey($"{ScsiPortsKey}\\{port}"))
                {
                    if (ScsiBuses == null)
                    {
                        AddToConsole("ScsiBuses key not found.", Color.Red);
                        return;
                    }

                    foreach (string bus in ScsiBuses.GetSubKeyNames())
                    {
                        await ProcessScsiBusAsync(port, bus);
                    }
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"ScsiBuses error: {ex.Message}", Color.Red);
            }
        }

        private async Task ProcessScsiBusAsync(string port, string bus)
        {
            try
            {
                string keyPath = $"{ScsiPortsKey}\\{port}\\{bus}\\Target Id 0\\Logical Unit Id 0";
                using (RegistryKey ScsuiBus = Registry.LocalMachine.OpenSubKey(keyPath, true))
                {
                    if (ScsuiBus == null) return;

                    object deviceTypeValue = ScsuiBus.GetValue("DeviceType");
                    if (deviceTypeValue == null || deviceTypeValue.ToString() != "DiskPeripheral") return;

                    await UpdateDiskPeripheralAsync(ScsuiBus, bus);
                }
            }
            catch (Exception ex)
            {
                AddToConsole($"ScsiBus error: {ex.Message}", Color.Red);
            }
        }

        private async Task UpdateDiskPeripheralAsync(RegistryKey ScsuiBus, string bus)
        {
            try
            {
                object identifierBeforeObj = ScsuiBus.GetValue("Identifier");
                object serialNumberBeforeObj = ScsuiBus.GetValue("SerialNumber");

                if (identifierBeforeObj == null || serialNumberBeforeObj == null) return;

                string identifierBefore = identifierBeforeObj.ToString();
                string serialNumberBefore = serialNumberBeforeObj.ToString();

                string identifierAfter = GetRandomDiskName();
                string serialNumberAfter = RandomId(14);


                string logBefore = $"DiskPeripheral {bus}\\Target Id 0\\Logical Unit Id 0 - Identifier: {identifierBefore}, SerialNumber: {serialNumberBefore}";
                string logAfter = $"DiskPeripheral {bus}\\Target Id 0\\Logical Unit Id 0 - Identifier: {identifierAfter}, SerialNumber: {serialNumberAfter}";

                await SaveLogsAsync("disk", logBefore, logAfter);

                ScsuiBus.SetValue("DeviceIdentifierPage", Encoding.UTF8.GetBytes(serialNumberAfter));
                ScsuiBus.SetValue("Identifier", identifierAfter);
                ScsuiBus.SetValue("InquiryData", Encoding.UTF8.GetBytes(identifierAfter));
                ScsuiBus.SetValue("SerialNumber", serialNumberAfter);

                AddToConsole($"Successfully changed DiskPeripheral {bus}.", Color.Green);
                AddToConsole($"Old Identifier: {identifierBefore}, New Identifier: {identifierAfter}", Color.Green);
                AddToConsole($"Old SerialNumber: {serialNumberBefore}, New SerialNumber: {serialNumberAfter}", Color.Green);
            }
            catch (Exception ex)
            {
                AddToConsole($"UpdateDiskPeripheral error: {ex.Message}", Color.Red);
            }
        }
        private string RandomIdprid2(int length)
        {
            const string digits = "0123456789";
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var id = new char[32];
            int letterIndex = 0;

            for (int i = 0; i < 32; i++)
            {
                if (i == 8 || i == 13 || i == 18 || i == 23)
                {
                    id[i] = '-';
                }
                else if (i % 5 == 4)
                {
                    id[i] = letters[random.Next(letters.Length)];
                    letterIndex++;
                }
                else
                {
                    id[i] = digits[random.Next(digits.Length)];
                }
            }

            return new string(id);
        }

        private void airButton7_Click(object sender, EventArgs e)
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

        private void airButton15_Click(object sender, EventArgs e)
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

        private int RandomDisplayId()
        {
            Random rnd = new Random();
            return rnd.Next(1, 9);
        }

        private void airButton8_Click(object sender, EventArgs e)
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

        public static string RandomMac()
        {
            string chars = "ABCDEF0123456789";
            string windows = "26AE";
            string result = "";
            Random random = new Random();

            result += chars[random.Next(chars.Length)];
            result += windows[random.Next(windows.Length)];

            for (int i = 0; i < 5; i++)
            {
                result += "-";
                result += chars[random.Next(chars.Length)];
                result += chars[random.Next(chars.Length)];

            }

            return result;
        }
        public static void Enable_LocalAreaConection(string adapterId, bool enable = true)
        {
            string interfaceName = "Ethernet";
            foreach (NetworkInterface i in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (i.Id == adapterId)
                {
                    interfaceName = i.Name;
                    break;
                }
            }

            string control;
            if (enable)
                control = "enable";
            else
                control = "disable";

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("netsh", $"interface set interface \"{interfaceName}\" {control}");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();
            p.WaitForExit();
        }

        private bool SpoofMAC()
        {
            bool err = false;
            using (RegistryKey NetworkAdapters = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}"))
            {
                foreach (string adapter in NetworkAdapters.GetSubKeyNames())
                {
                    if (adapter != "Properties")
                    {
                        try
                        {
                            using (RegistryKey NetworkAdapter = Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{4d36e972-e325-11ce-bfc1-08002be10318}}\\{adapter}", true))
                            {
                                if (NetworkAdapter.GetValue("BusType") != null)
                                {
                                    string adapterId = NetworkAdapter.GetValue("NetCfgInstanceId").ToString();
                                    string macBefore = NetworkAdapter.GetValue("NetworkAddress")?.ToString();

                                    // Store the original MAC address
                                    if (!string.IsNullOrEmpty(macBefore))
                                    {
                                        originalMACAddresses[adapterId] = macBefore ?? string.Empty;
                                    }

                                    string macAfter = RandomMac();
                                    string logBefore = $"MAC Address {adapterId} - Before: {macBefore}";
                                    string logAfter = $"MAC Address {adapterId} - After: {macAfter}";
                                    SaveLogsAsync("mac", logBefore, logAfter);

                                    NetworkAdapter.SetValue("NetworkAddress", macAfter);
                                    Enable_LocalAreaConection(adapterId, false);
                                    Enable_LocalAreaConection(adapterId, true);
                                }
                            }
                        }
                        catch (System.Security.SecurityException)
                        {
                            err = true;
                            break;
                        }
                    }
                }
            }
            return err;
        }


        private void airButton11_Click(object sender, EventArgs e)
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

        private void airButton10_Click(object sender, EventArgs e)
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

        private void airButton9_Click(object sender, EventArgs e)
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
                        originalComputerName = originalName;  // Store the original name

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

        private void airButton12_Click(object sender, EventArgs e)
        {

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

        private void fetchAsset_Click(object sender, EventArgs e)
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

                if (airCheckBox2.Checked || airCheckBox3.Checked)
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        if (airCheckBox2.Checked)
                            g.DrawImage(Properties.Resources.shirt_template, new RectangleF(0, 0, img.Width, img.Height));
                        else if (airCheckBox3.Checked)
                            g.DrawImage(Properties.Resources.pants_template, new RectangleF(0, 0, img.Width, img.Height));
                    }
                }

                assetPreview.Image = img;

                string pathString = Path.Combine(Environment.CurrentDirectory, "Assets");
                if (!Directory.Exists(pathString))
                {
                    Directory.CreateDirectory(pathString);
                }
                img.Save($"{pathString}\\{assetTextbox.Text}.png", System.Drawing.Imaging.ImageFormat.Png);

                Console.WriteLine("> Asset fetched and downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"> An error occurred: {ex.Message}");
            }
        }

        private async Task DownloadAssetAsync(string downloadUrl, string fileName)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "YourAppName");

                    byte[] data = await httpClient.GetByteArrayAsync(downloadUrl);

                    string pathString = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
                    if (!Directory.Exists(pathString))
                    {
                        Directory.CreateDirectory(pathString);
                    }

                    string filePath = Path.Combine(pathString, fileName);

                    await File.WriteAllBytesAsync(filePath, data);

                    AddToConsole($"> {fileName} downloaded successfully.", Color.Black);
                }
                catch (HttpRequestException ex)
                {
                    AddToConsole($"> An error occurred: {ex.Message}", Color.Red);
                }
            }
        }

        private void assetDownload_Click(object sender, EventArgs e)
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
                Console.WriteLine($"> An error occurred: {ex.Message}");
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            string discordUrl = "https://discord.gg/faJRE4pAAE";

            Process.Start(new ProcessStartInfo
            {
                FileName = discordUrl,
                UseShellExecute = true
            });
        }
    }
}