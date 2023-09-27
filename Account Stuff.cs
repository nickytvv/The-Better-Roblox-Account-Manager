using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Account_Manager
{
    public partial class Form1 : Form
    {
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


    }
}
