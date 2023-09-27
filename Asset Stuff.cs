using ReaLTaiizor.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Account_Manager
{
    public partial class Form1 : Form
    {
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

    }

}
