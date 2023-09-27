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