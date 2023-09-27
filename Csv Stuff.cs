using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Account_Manager
{
    public partial class Form1 : Form
    {
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
                    string cookie = selectedItem.SubItems[1].Text; 

                    var (isValid, username) = await ValidateCookie(cookie);
                    if (isValid)
                    {
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
            string? exeDirectory = Path.GetDirectoryName(exePath);
            string csvFilePath = Path.Combine(exeDirectory, "RobloxAccounts.csv");
            List<string> lines = File.ReadAllLines(csvFilePath).ToList();
            lines.RemoveAll(line => line.StartsWith(usernameToRemove + ","));
            File.WriteAllLines(csvFilePath, lines);
            AddToConsole("> Selected account deleted", Color.Green);
        }
    }
}
