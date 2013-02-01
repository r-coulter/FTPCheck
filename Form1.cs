using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.IO.Compression;
using Ionic.Zip;
using System.Configuration;


namespace FTPCheck
{
    public partial class Form1 : Form
    {
        string lastTime = "";
        string host = System.Configuration.ConfigurationManager.AppSettings["host"];//ConfigurationSettings.AppSettings["host"];
        string user = System.Configuration.ConfigurationManager.AppSettings["user"];
        string pass = System.Configuration.ConfigurationManager.AppSettings["password"];
        string thePath = System.Configuration.ConfigurationManager.AppSettings["filePath"];
        public Form1()
        {
            InitializeComponent();
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDir.Text = folderBrowserDialog1.SelectedPath;
               // System.Configuration.Configuration config =
             // ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
               // config.AppSettings.Settings.Remove("filePath");
               // config.AppSettings.Settings.Add("filePath", folderBrowserDialog1.SelectedPath);
               // config.Save();



            }
        }

        private void checkFile()
        {
            string zipFilePath = txtDir.Text + "\\Temp\\";
            FileInfo theZipFile = new FileInfo(zipFilePath + "GWLXML.zip");
            if (!theZipFile.Exists) { downloadFile(); return; }

            DirectoryInfo extractDir = new DirectoryInfo(zipFilePath + "extract");
            if (extractDir.Exists)
            {
                extractDir.Delete(true);
            }

            var options = new ReadOptions { StatusMessageWriter = System.Console.Out };
            using (ZipFile zip = ZipFile.Read(zipFilePath + "GWLXML.zip", options))
            {
                zip.ExtractAll(zipFilePath + "extract");
            }

            DirectoryInfo di = new DirectoryInfo(zipFilePath + "extract");
            if (di.Exists)
            {
                FileInfo xmlFile = new FileInfo(zipFilePath + "extract\\ENTITY.XML");
                if (xmlFile.Exists)
                {
                    lastTime = xmlFile.LastWriteTime.Year.ToString() + "_" + xmlFile.LastWriteTime.Month.ToString() + "_" + xmlFile.LastWriteTime.Day.ToString() + "_" + xmlFile.LastWriteTime.Hour.ToString() + "." + xmlFile.LastWriteTime.Minute.ToString();
                    lblUpdate.Text = xmlFile.LastWriteTime.ToString();
                }
                di.Delete(true);
            }
            else
            {
                downloadFile();
            }
        }


        private void downloadFile()
        {
            
            string inputFilePath = txtDir.Text + "\\Temp\\";

            string ftpFullPath = host;
            WebClient request = new WebClient();
            request.Credentials = new NetworkCredential(user, pass);
            byte[] fileData;

            try
            {
                fileData = request.DownloadData(ftpFullPath);
            }
            catch(Exception e)
            {
                MessageBox.Show(DateTime.Now + " : " + e.Message,"Error connecting to FTP server");
                return;
            }

            Directory.CreateDirectory(inputFilePath);
            FileStream file = File.Create(inputFilePath + "GWLXML.zip");
            file.Write(fileData, 0, fileData.Length);
            file.Close();

            DirectoryInfo extractDir = new DirectoryInfo(inputFilePath + "extract");
            if (extractDir.Exists)
            {
                extractDir.Delete(true);
            }
            var options = new ReadOptions { StatusMessageWriter = System.Console.Out };
            using (ZipFile zip = ZipFile.Read(inputFilePath + "GWLXML.zip", options))
            {
                // This call to ExtractAll() assumes:
                //   - none of the entries are password-protected.
                //   - want to extract all entries to current working directory
                //   - none of the files in the zip already exist in the directory;
                //     if they do, the method will throw.
                zip.ExtractAll(inputFilePath + "extract");
            }
            DirectoryInfo di = new DirectoryInfo(inputFilePath + "extract");
            if (di.Exists)
            {
                FileInfo xmlFile = new FileInfo(inputFilePath + "extract\\ENTITY.XML");
                if (xmlFile.Exists)
                {
                    if (xmlFile.LastWriteTime.ToString() != lastTime || lastTime == "")
                    {
                        lastTime = xmlFile.LastWriteTime.Year.ToString() + "_" + xmlFile.LastWriteTime.Month.ToString() + "_" + xmlFile.LastWriteTime.Day.ToString() + "_" + xmlFile.LastWriteTime.Hour.ToString() + "." + xmlFile.LastWriteTime.Minute.ToString();
                        lblUpdate.Text = xmlFile.LastWriteTime.ToString();
                        string newDir = txtDir.Text +"\\"+ (lastTime.Replace("/", "").Replace(':', '.'));
                        Directory.CreateDirectory(newDir);
                        File.Copy(inputFilePath + "GWLXML.zip", newDir + "\\GWLXML.zip",true);
                    }
                }
                di.Delete(true);
            }
            
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Start();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            Form1.ActiveForm.Text = "FTPCheck: Running";
            notifyIcon1.Text = "FTPCheck: Running";
            txtDir.Enabled = false;
            checkFile();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            Form1.ActiveForm.Text = "FTPCheck: Stopped";
            notifyIcon1.Text = "FTPCheck: Stopped";
            txtDir.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            downloadFile();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "FTPCheck";
            notifyIcon1.BalloonTipText = "FTPCheck is still running. Double-click the tray icon to restore.";

            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

    }
}
