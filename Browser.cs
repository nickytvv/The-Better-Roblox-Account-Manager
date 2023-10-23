using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace AccountManager
{
    public partial class Browser : Form
    {
        public ChromiumWebBrowser chromeBrowser;

        public Browser()
        {
            InitializeComponent();
            InitializeChromium();
        }

        public void InitializeChromium()
        {
            chromeBrowser = new ChromiumWebBrowser("https://www.google.com");
            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            chromeBrowser.LoadingStateChanged += OnLoadingStateChanged;
            chromeBrowser.LoadError += OnLoadError;
        }

        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs args)
        {
            if (!args.IsLoading)
            {
                MessageBox.Show("Page load is complete.");
            }
        }

        private void OnLoadError(object sender, LoadErrorEventArgs args)
        {
            MessageBox.Show("Load Error:" + args.ErrorCode + ", " + args.ErrorText);
        }
    }
}
