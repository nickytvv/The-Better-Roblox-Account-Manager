using ReaLTaiizor.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Account_Manager
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



    }
}
