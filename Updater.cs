using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http.Headers;


namespace AccountManager
{
    public partial class Form1 : Form
    {

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

                    this.Invoke((MethodInvoker)delegate
                    {
                        latestVersionBox.Text = latestVersion.Trim();
                        AddToConsole("> Fetched Latest Version..", Color.White);
                    });
                }
                catch (Exception ex)
                {
                        latestVersionBox.Text = "Error fetching version";
                }
            }
        }


        private async Task CheckForUpdates()
        {
            string latestVersionUrl = "https://nickystv.com/version/version.txt";
            string changelogUrl = "https://nickystv.com/version/changelog.txt";
            string currentVersion = "1.3.2";

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true, MustRevalidate = true };
                string latestVersion = await httpClient.GetStringAsync($"{latestVersionUrl}?nocache={Guid.NewGuid()}");
                string changelog = await httpClient.GetStringAsync($"{changelogUrl}?nocache={Guid.NewGuid()}");

                if (latestVersion.Trim() != currentVersion.Trim())
                {
                    using (Changelog form2 = new Changelog())
                    {
                        form2.ShowDialog();

                        if (form2.ShouldUpdate)
                        {
                            string twoFoldersUp = Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName).FullName;
                            string installerPath = Path.Combine(twoFoldersUp, "Installer.exe");

                            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true, Verb = "runas" });
                            Environment.Exit(0);
                        }
                    }
                }
                else
                {
                    AddToConsole("> No Update Found...", Color.HotPink);
                }
            }
        }
    }
}
