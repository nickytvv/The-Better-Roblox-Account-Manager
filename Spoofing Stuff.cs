using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Account_Manager
{
    public partial class Form1 : Form
    {
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


        private int RandomDisplayId()
        {
            Random rnd = new Random();
            return rnd.Next(1, 9);
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


    }
}
