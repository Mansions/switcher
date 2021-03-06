﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using ass.Properties;

namespace MansionServerSwitcher
{
    public partial class MainForm : Form
    {
        public bool Mansion = true;
        public string MansionIP = "62.210.27.20";  // memesys
        public string mirrorIP = "198.251.90.183";  // zxq
        public bool testConnection = false;

        public int currentVersion = 140;     // Increment this and update changelog before compiling a new update
        public int latestChangelog = 0;

        public string settingsPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mansion Server Switcher";
        public string hostsPath = Environment.GetEnvironmentVariable("windir") + "\\system32\\drivers\\etc\\hosts";

        public MainForm()
        {
            InitializeComponent();

            // Make sure hosts file exists
            if (!File.Exists(hostsPath))
                File.AppendAllText(hostsPath, "# Hosts file");

            // Create tooltips
            ToolTip OnOffTooltip = new ToolTip();
            OnOffTooltip.SetToolTip(this.switchButton, "Switch between osu! and Mansion Servers");
            ToolTip LocalRipwotTooltip = new ToolTip();
            LocalRipwotTooltip.SetToolTip(this.updateIPButton, "Get the right server IP address directly from the server.");
            ToolTip InstallCertificateTooltip = new ToolTip();
            InstallCertificateTooltip.SetToolTip(this.installCertificateButton, "Install/Remove HTTPS certificate.\nYou must have install the certificate in order to connect\nto Mansion with stable/beta/cutting edge.\nYou don't need the certificate with fallback.");

            // Create settings directory (if it doesn't exists)
            Directory.CreateDirectory(settingsPath);

            // Check if Mansion.txt exists and if not create a default one
            if (!File.Exists(settingsPath + "\\Mansion.txt"))
            {
                File.AppendAllText(settingsPath + "\\Mansion.txt", MansionIP + Environment.NewLine);
                File.AppendAllText(settingsPath + "\\Mansion.txt", mirrorIP + Environment.NewLine);
                File.AppendAllText(settingsPath + "\\Mansion.txt", "true");
                File.AppendAllText(settingsPath + "\\Mansion.txt", Convert.ToString(currentVersion-1)+Environment.NewLine);
            }

            // Read Mansion.txt
            string[] MansionTxt = File.ReadAllLines(settingsPath + "\\Mansion.txt");

            // If there are 4 lines, it's not corrupter or memes
            if (MansionTxt.Length == 4)
            {
                // Read IP
                MansionIP = MansionTxt[0];
                mirrorIP = MansionTxt[1];

                // Check if testConnection is bool, if yes read it, otherwise use default settings
                bool isBool;
                Boolean.TryParse(MansionTxt[2], out isBool);
                if (isBool)
                    testConnection = Convert.ToBoolean(MansionTxt[2]);

                // Read latest changelog
                latestChangelog = Convert.ToInt32(MansionTxt[3]);
            }
            else
            {
                // Something went wrong, use default settings
            }

            // Check if we have to show a changelog
            if (latestChangelog < currentVersion)
            {
                // Show new changelog
                ChangelogForm cf = new ChangelogForm();
                cf.ShowDialog();

                // Update latest changelog
                latestChangelog = currentVersion;
            }

            // Update settings
            updateSettings();

            // Get current hosts configuration
            findServer();

            // Get certificate status and update button text
            updateCertificateButton();


            // Check if we are using old server IP
            checkOldServerIP();
        }


        private void MainForm_Shown(object sender, EventArgs e)
        {
            updateStatusLabel();
        }

        public void saveSettings()
        {
            // Save settings to Mansion.txt
            File.WriteAllText(settingsPath + "\\Mansion.txt", MansionIP + Environment.NewLine);
            File.AppendAllText(settingsPath + "\\Mansion.txt", mirrorIP + Environment.NewLine);
            File.AppendAllText(settingsPath + "\\Mansion.txt", Convert.ToString(testConnection) + Environment.NewLine);
            File.AppendAllText(settingsPath + "\\Mansion.txt", Convert.ToString(latestChangelog));
        }

        public bool findServer()
        {
            // Read hosts
            string[] hostsContent = File.ReadAllLines(hostsPath);

            // Loop through all strings
            for (var i = 0; i < hostsContent.Length; i++)
            {
                // Check if current line is not empty (otherwise it throws an exception)
                if (hostsContent[i] != "")
                {
                    // Check if current line is not commented and redirects to osu.ppy.sh
                    if ((Regex.Matches(hostsContent[i], "#").Count == 0) && (Regex.Matches(hostsContent[i], "osu.ppy.sh").Count > 0))
                    {
                        // Our hosts points to Mansion
                        Mansion = true;
                        return Mansion;
                    }
                }
            }

            // Hosts doesn't contain any reference to osu.ppy.sh, we are not pointing to Mansion
            Mansion = false;
            return Mansion;
        }

        public bool updateServer()
        {
            // Check if IP is not empty and valid (I should rewrite this but idc for now)
            if (MansionIP != "" && mirrorIP != "")
            {
                IPAddress type;
                if ( (IPAddress.TryParse(MansionIP, out type) && type.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) && IPAddress.TryParse(mirrorIP, out type))
                {
                    // Check read only
                    if(IsFileReadOnly(hostsPath))
                        SetFileReadAccess(hostsPath, false);

                    // Read hosts
                    string[] hostsContent = File.ReadAllLines(hostsPath);

                    // Check for any osu.ppy.sh line, remove them
                    for (var i = 0; i < hostsContent.Length; i++)
                    {
                        if (Regex.Matches(hostsContent[i], "(?:osu|a|c|c1|bm6|i).ppy.sh").Count > 0)
                        {
                            // Line that points (or used to point) to osu.ppy.sh, remove it
                            hostsContent[i] = "";
                        }
                    }

                    // Empty hosts
                    try
                    {
                        File.WriteAllText(hostsPath, "");

                        // Rewrite hosts
                        for (var i = 0; i < hostsContent.Length; i++)
                        {
                            if (hostsContent[i] != "")
                            {
                                // Current line is not empty, write it
                                File.AppendAllText(hostsPath, hostsContent[i] + Environment.NewLine);
                            }
                        }

                        // Point to Mansion if required
                        if (Mansion)
                        {
                            File.AppendAllText(hostsPath, MansionIP + "   osu.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, MansionIP + "   c.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, MansionIP + "   c1.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, MansionIP + "   a.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, MansionIP + "   i.ppy.sh" + Environment.NewLine);
                            File.AppendAllText(hostsPath, mirrorIP + "   bm6.ppy.sh" + Environment.NewLine);
                        }

                        return true;
                    }
                    catch
                    {
                        MessageBox.Show("Error while writing hosts file."+Environment.NewLine+"Please make sure hosts is not set to read only mode.");
                        return false;
                    }
                }
                else
                {
                    statusLabel.Text = "Invalid Mansion/Mirror IP address";
                    return false;
                }
            }
            else
            {
                statusLabel.Text = "Invalid Mansion/Mirror IP address";
                return false;
            }
        }

        public void updateStatusLabel()
        {
            // Update statusLabel based on Mansion variable
            statusLabel.Text = Mansion ? "You are playing on Mansion." + Environment.NewLine+IPTextBox.Text+" - "+MirrorIPTextBox.Text : "You are playing on Osu! server.";
            // Ayy k maron sn pigor xd
            updateJennaWarning();
        }

        public void updateJennaWarning()
        {
            if (Application.OpenForms.Count >= 1)
                if (MansionIP == "127.0.0.1")
                    Application.OpenForms[0].Height = 330;
                else
                    Application.OpenForms[0].Height = 202;
        }

        public void updateSettings()
        {
            // Update textBoxes in settings group
            IPTextBox.Text = MansionIP;
            MirrorIPTextBox.Text = mirrorIP;
            //testCheckBox.Checked = testConnection;
        }

        private void IPTextBox_TextChanged(object sender, EventArgs e)
        {
            // Settings: Update IP address
            MansionIP = IPTextBox.Text;
        }

        private void MirrorIPTextBox_TextChanged(object sender, EventArgs e)
        {
            // Settings: Update Mirror IP address
            mirrorIP = MirrorIPTextBox.Text;
        }

        private void testCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Settings:  Update test connection
            //testConnection = testCheckBox.Checked;
        }

        private void switchButton_Click(object sender, EventArgs e)
        {
            // Get current hosts status, because it might have changed
            findServer();

            // Switch between Mansion/osu!, write hosts and update label
            Mansion = !Mansion;
            if (updateServer())
                updateStatusLabel();

            // Connection check if we are on Mansion
            //if (Mansion && testConnection)
            //    checkMansionConnection();
        }

        public void checkMansionConnection()
        {
            // Checks if osu.ppy.sh actually points to Mansion
            try
            {
                //WebClient wc = new WebClient();
                //string s = wc.DownloadString("http://osu.ppy.sh/");

                string s;
                using (WebClient client = new WebClient())
                {
                    byte[] response =
                    client.UploadValues("http://osu.ppy.sh/", new NameValueCollection()
                    {
                        { "switcher", "true" },
                    });
                    s = Encoding.UTF8.GetString(response);
                }

                if (s == "ok")
                    updateStatusLabel();    // This changes statuslabel.text to "You are playing on Mansion"
                else
                    statusLabel.Text = "Error while connecting Mansion.";
            }
            catch
            {
                // 4xx / 5xx error
                statusLabel.Text = "Error while connecting Mansion.";
            }
        }

        private void genuineButton1_Click(object sender, EventArgs e)
        {
            // Save settings and close
            saveSettings();
            Application.Exit();
        }

        private void groupBox1_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.White, Color.FromArgb(100, 100, 100));
        }

        private void DrawGroupBox(GroupBox box, Graphics g, Color textColor, Color borderColor)
        {
            if (box != null)
            {
                Brush textBrush = new SolidBrush(textColor);
                Brush borderBrush = new SolidBrush(borderColor);
                Pen borderPen = new Pen(borderBrush);
                SizeF strSize = g.MeasureString(box.Text, box.Font);
                Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                               box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                               box.ClientRectangle.Width - 1,
                                               box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

                // Clear text and border
                g.Clear(this.BackColor);

                // Draw text
                g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                // Drawing Border
                //Left
                g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                //Right
                g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Bottom
                g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Top1
                g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                //Top2
                g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            }
        }

      
        void updateServerIP()
        {
            try
            {
                // Get server ip from ip.Mansion.moe
                WebClient client = new WebClient();
                string[] remoteIPs = client.DownloadString("https://old.themansions.nl/ips.txt").TrimEnd('\r', '\n').Split('\n');

                // Mansion IP
                if (MansionIP != remoteIPs[0])
                {
                    IPTextBox.Text = remoteIPs[0];
                    if (updateServer())
                        updateStatusLabel();
                }

                // Mirror IP
                if (mirrorIP != remoteIPs[1])
                {
                    MirrorIPTextBox.Text = remoteIPs[1];
                    if (updateServer())
                        updateStatusLabel();
                }
            }
            catch
            {
                // Error
            }
        }

        void updateServerIPrelax()
        {
            try
            {
                // Get server ip from ip.Mansion.moe
                WebClient client = new WebClient();
                string[] remoteIPs = client.DownloadString("https://old.relax.themansions.nl/ips.txt").TrimEnd('\r', '\n').Split('\n');

                // Mansion IP
                if (MansionIP != remoteIPs[0])
                {
                    IPTextBox.Text = remoteIPs[0];
                    if (updateServer())
                        updateStatusLabel();
                }

                // Mirror IP
                if (mirrorIP != remoteIPs[1])
                {
                    MirrorIPTextBox.Text = remoteIPs[1];
                    if (updateServer())
                        updateStatusLabel();
                }
            }
            catch
            {
                // Error
            }
        }

        void updateServerIPAuto()
        {
            try
            {
                // Get server ip from ip.Mansion.moe
                WebClient client = new WebClient();
                string[] remoteIPs = client.DownloadString("https://old.ap.themansions.nl/ips.txt").TrimEnd('\r', '\n').Split('\n');

                // Mansion IP
                if (MansionIP != remoteIPs[0])
                {
                    IPTextBox.Text = remoteIPs[0];
                    if (updateServer())
                        updateStatusLabel();
                }

                // Mirror IP
                if (mirrorIP != remoteIPs[1])
                {
                    MirrorIPTextBox.Text = remoteIPs[1];
                    if (updateServer())
                        updateStatusLabel();
                }
            }
            catch
            {
                // Error
            }
        }

        void updateServerIPArcade()
        {
            try
            {
                // Get server ip from ip.Mansion.moe
                WebClient client = new WebClient();
                string[] remoteIPs = client.DownloadString("https://old.arcade.themansions.nl/ips.txt").TrimEnd('\r', '\n').Split('\n');

                // Mansion IP
                if (MansionIP != remoteIPs[0])
                {
                    IPTextBox.Text = remoteIPs[0];
                    if (updateServer())
                        updateStatusLabel();
                }

                // Mirror IP
                if (mirrorIP != remoteIPs[1])
                {
                    MirrorIPTextBox.Text = remoteIPs[1];
                    if (updateServer())
                        updateStatusLabel();
                }
            }
            catch
            {
                // Error
            }
        }

        void checkOldServerIP()
        {
            try
            {
                // Get old ip from ip.Mansion.moe
                WebClient client = new WebClient();
                var oldIPs = client.DownloadString("https://old.relax.themansions.nl/ips.txt").Split('\n');
                int l = oldIPs.Length;
                for (int i=0; i< l; i++)
                {
                    if (IPTextBox.Text == oldIPs[i] || MirrorIPTextBox.Text == oldIPs[i])
                    {
                        MessageBox.Show("You're ips might be out of date just check by pressing the button of the server you want to play on :)");
                        break;
                    }
                }
            }
            catch
            {
                // Error
            }
        }

        private void updateIPButton_Click(object sender, EventArgs e)
        {
            updateServerIP();
        }


        // Returns wether a file is read-only.
        public static bool IsFileReadOnly(string FileName)
        {
            // Create a new FileInfo object.
            FileInfo fInfo = new FileInfo(FileName);

            // Return the IsReadOnly property value.
            return fInfo.IsReadOnly;
        }

        // Sets the read-only value of a file.
        public static void SetFileReadAccess(string FileName, bool SetReadOnly)
        {
            // Create a new FileInfo object.
            FileInfo fInfo = new FileInfo(FileName);

            // Set the IsReadOnly property.
            fInfo.IsReadOnly = SetReadOnly;
        }

        private void updateCertificateButton(bool __installed = true, bool check = true)
        {
            bool installed = __installed;
            if (check)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, "Mansion", true);
                installed = certs.Count > 0 ? true : false;
            }
            if (installed)
            {
                installCertificateButton.Text = "Remove certificate";
                installCertificateButton.Font = new Font(installCertificateButton.Font.Name, installCertificateButton.Font.Size, FontStyle.Regular);
            }
            else
            {
                installCertificateButton.Text = "Install certificate";
                installCertificateButton.Font = new Font(installCertificateButton.Font.Name, installCertificateButton.Font.Size, FontStyle.Bold);
            }
        }

        private void installCertificateButton_Click(object sender, EventArgs e)
        {
            // Check and install certificate
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, "Mansion", true);

            if (certs.Count > 0)
            {
                // Certificate already installed, remove it
                DialogResult yn = MessageBox.Show("Are you sure you want to remove Mansion's HTTPS certificate?\nThere's no need to remove it, you'll be able to browse both Mansion and osu!\nwithout any problem even if the certificate is installed and the switcher is off.", "Mansion certificate installer", MessageBoxButtons.YesNo);
                if (yn == DialogResult.No)
                {
                    store.Close();
                    return;
                }
                foreach (var cert in certs)
                {
                    try
                    {
                        store.Remove(cert);
                        updateCertificateButton(false, false);
                        MessageBox.Show("Certificate removed!", "Mansion certificate installer");
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                if (store != null)
                {
                    store.Close();
                }
            }
            else
            {
                // Install certificate
                try
                {
                    // Save the certificate in settingsPath temporary
                    string certFilePath = settingsPath + "\\cmyui.crt";
                    File.WriteAllBytes(certFilePath, Resources.certificate);

                    // Get all certficates
                    X509Certificate2Collection collection = new X509Certificate2Collection();
                    collection.Import(certFilePath);

                    // Install all certificates
                    foreach (X509Certificate2 cert in collection)
                        store.Add(cert);

                    updateCertificateButton(true, false);
                    MessageBox.Show("Certificate installed! Try connecting to Mansion with beta/stable/cutting edge", "Mansion certificate installer");

                    // Delete temp certificate file
                    File.Delete(certFilePath);
                }
                catch
                {
                    MessageBox.Show("Error while installing certificate.", "Mansion certificate installer");
                }
            }

            store.Close();
        }

        private void localButton_Click(object sender, EventArgs e)
        {
            updateServerIPrelax();
        }

        private void statusLabel_Click(object sender, EventArgs e)
        {

        }

        private void genuineTheme1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void genuineButton2_Click(object sender, EventArgs e)
        {
            updateServerIPAuto();
        }

        private void genuineButton3_Click(object sender, EventArgs e)
        {
            updateServerIPArcade();
        }
    }
}
