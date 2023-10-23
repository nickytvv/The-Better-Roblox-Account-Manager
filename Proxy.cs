using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AccountManager
{
    public partial class Proxy : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;

        public Proxy()
        {
            InitializeComponent();
            this.MouseDown += new MouseEventHandler(Form_MouseDown);
            this.MouseUp += new MouseEventHandler(Form_MouseUp);
            this.MouseMove += new MouseEventHandler(Form_MouseMove);

            listView1.Columns.Add("IP", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("Port", 50, HorizontalAlignment.Left);
            listView1.Columns.Add("Username", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("Password", 100, HorizontalAlignment.Left);
            listView1.Columns.Add("Status", 100, HorizontalAlignment.Left);

            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.MultiSelect = true;
            listView1.Sorting = SortOrder.None;
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView1.SmallImageList = imageList1;

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Test Proxy", null, TestProxy_Click);
            listView1.ContextMenuStrip = contextMenu;
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            lastCursor = Cursor.Position;
            lastForm = this.Location;
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point cursor = Cursor.Position;
                int dx = cursor.X - lastCursor.X;
                int dy = cursor.Y - lastCursor.Y;
                int x = lastForm.X + dx;
                int y = lastForm.Y + dy;
                this.Location = new Point(x, y);
            }
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private async Task<bool> CheckProxy(string ip, string port, string username, string password)
        {
            var proxy = new WebProxy($"{ip}:{port}", false)
            {
                Credentials = new NetworkCredential(username, password)
            };
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            using (HttpClient httpClient = new HttpClient(httpClientHandler))
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync("http://ip-api.com/json");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        public async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(7);
                List<Task> tasks = new List<Task>();

                foreach (ListViewItem item in listView1.Items)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string ip = item.SubItems[0].Text;
                            string port = item.SubItems[1].Text;
                            string username = item.SubItems[2].Text;
                            string password = item.SubItems[3].Text;

                            bool isWorking = await CheckProxy(ip, port, username, password);

                            this.Invoke((MethodInvoker)delegate
                            {
                                if (isWorking)
                                {
                                    item.SubItems[4].Text = "Working";
                                    item.ImageIndex = 0;
                                }
                                else
                                {
                                    item.SubItems[4].Text = "Not Working";
                                    item.ImageIndex = 1;
                                }
                            });
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool hasWorkingProxies = false;
            StringBuilder sb = new StringBuilder();

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[4].Text == "Working")
                {
                    sb.AppendLine($"{item.SubItems[0].Text}:{item.SubItems[1].Text}:{item.SubItems[2].Text}:{item.SubItems[3].Text}");
                    hasWorkingProxies = true;
                }
            }

            if (hasWorkingProxies)
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "proxies.txt")))
                {
                    sw.Write(sb.ToString());
                }
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private async void TestProxy_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                ListViewItem item = listView1.SelectedItems[0];
                string ip = item.SubItems[0].Text;
                string port = item.SubItems[1].Text;
                string username = item.SubItems[2].Text;
                string password = item.SubItems[3].Text;

                bool isWorking = await CheckProxy(ip, port, username, password);

                if (isWorking)
                {
                    item.SubItems[4].Text = "Working";
                    item.ImageIndex = 0;
                }
                else
                {
                    item.SubItems[4].Text = "Not Working";
                    item.ImageIndex = 1;
                }
            }
        }

        private void foreverGroupBox1_DragDrop_1(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".txt")
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (string line in lines)
                    {
                        string[] parts = ParseProxyLine(line);
                        if (parts != null)
                        {
                            ListViewItem item = new ListViewItem(parts[0]);
                            item.SubItems.Add(parts.Length > 1 ? parts[1] : "");
                            item.SubItems.Add(parts.Length > 2 ? parts[2] : "");
                            item.SubItems.Add(parts.Length > 3 ? parts[3] : "");
                            item.SubItems.Add("Not Checked");
                            listView1.Items.Add(item);
                        }
                    }
                }
            }
        }

        private string[] ParseProxyLine(string line)
        {
            if (line.StartsWith("http://"))
            {
                string trimmedLine = line.Substring(7);
                string[] mainParts = trimmedLine.Split('@');
                string[] userPass = mainParts[0].Split(':');
                string[] hostPort = mainParts[1].Split(':');
                return new string[] { hostPort[0], hostPort.Length > 1 ? hostPort[1] : "", userPass[0], userPass.Length > 1 ? userPass[1] : "" };
            }
            else
            {
                string[] parts = line.Split(':');
                if (parts.Length >= 1)
                {
                    return parts;
                }
            }
            return null;
        }

        private void foreverGroupBox1_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void crownMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void dreamForm1_Enter(object sender, EventArgs e)
        {
        }
    }
}
