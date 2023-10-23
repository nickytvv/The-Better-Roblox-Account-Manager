using System;
using System.Drawing;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Windows.Forms;
using mshtml;

namespace AccountManager
{
    public partial class Changelog : Form
    {
        public bool ShouldUpdate { get; private set; } = false;

        public Changelog()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            UpdateVersionLabel();
            webBrowser1.Navigate("https://nickystv.com/version/changelog.txt");
        }

        private void aloneButton1_Click(object sender, EventArgs e)
        {
            ShouldUpdate = true;
            this.Close();
        }

        private void aloneButton2_Click(object sender, EventArgs e)
        {
            ShouldUpdate = false;
            this.Close();
        }

        private void webBrowser1_DocumentCompleted_1(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            IHTMLDocument2 doc = webBrowser1.Document.DomDocument as IHTMLDocument2;
            IHTMLWindow2 parentWindow = doc.parentWindow;
            parentWindow.execScript("document.body.style.zoom='85%'", "JavaScript");
            webBrowser1.Refresh(WebBrowserRefreshOption.Completely);  // Add this line to clear cache
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
                    this.Invoke((MethodInvoker)delegate
                    {
                        label2.Text = latestVersion.Trim();  // Added Trim() to remove any extra spaces
                    });
                }
                catch (Exception ex)
                {
                    label2.Text = "Error fetching version";
                }
            }
        }

        private void airForm1_Click(object sender, EventArgs e)
        {

        }
    }
}
