using CombVideoPlayer2.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Vlc.DotNet.Core;
using Vlc.DotNet.Forms;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Permissions;

namespace CombVideoPlayer2
{
    public partial class Form2 : Form
    {
        Form1 f1;
        public bool ScanSubDir = false, ListConfigChange=false, FirstSettingSubDir = false, IconsDelete = false;
        public int Theme = 0, ThemeTmp = 0, FirstSettingTheme = 0;
        public string FirstSettingDirs = "", FirstSettingFlag = "";


        public Form2()
        {
            InitializeComponent();
        }
        public void F2StartUp(Form1 Own)
        {
            f1 = Own;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            Size = new Size(650, 360);
            Location = new Point((f1.Width - Width) / 2, (f1.Height - Height) / 2);
            label3.Location = new Point(436, 317);
            label2.Location = new Point(540, 317);
            panel1.Size = new Size(626, 270);

            Bitmap canvas = new Bitmap(Width, Height);
            Graphics g = Graphics.FromImage(canvas);
            Pen p = new Pen(rgb(0, 150, 136), 2f);
            g.DrawRectangle(p, 2, 2, Width - 4, Height - 4);
            p.Dispose();
            g.Dispose();
            BackgroundImage = canvas;
        }

        private Color rgb(int r, int g, int b)
        {
            return Color.FromArgb(r, g, b);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Hide();

            myTextBox1.Text = FirstSettingDirs;
            richTextBox2.Text = FirstSettingFlag;
            ScanSubDir = FirstSettingSubDir;
            Theme = FirstSettingTheme;

            if (Theme == 0) label16_Click(null, null);
            else if (Theme == 1) label17_Click(null, null);
            label8.Text = FirstSettingSubDir ? "ON" : "OFF";

            label18.Text = "";
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!f1.ExitFlag) e.Cancel = true;
            }
            catch { }
            Hide();
        }

        private void label8_Click(object sender, EventArgs e)
        {
            ScanSubDir = !ScanSubDir;
            if (ScanSubDir) label8.Text = "ON";
            else label8.Text = "OFF";
        }

        private void label2_Click(object sender, EventArgs e)
        {
            string[] ScanDir = myTextBox1.Text.Replace("\r", "").Split('\n');
            string ScanEx = richTextBox2.Text.Replace(" ", "").Replace(",", "|");

            Theme = ThemeTmp;
            string WriteText = "";
            foreach (string DirPath in ScanDir)
            {
                if (WriteText != "") WriteText += "\n";
                WriteText += "dir:" + DirPath;
            }
            if (WriteText != "") WriteText += "\n";
            WriteText += "ex:" + richTextBox2.Text;

            WriteText += "\nflag:" + ScanSubDir.ToString();
            WriteText += "\ncolor:" + Theme;


            StreamWriter sw = new StreamWriter(@"comb_video_player_2.conf", false, Encoding.Unicode);
            sw.Write(WriteText);
            sw.Close();

            Hide();

            if (ListConfigChange) f1.VideoListRefresh(ScanDir, ScanEx, ScanSubDir);
            f1.ApplicationTheme(Theme);
            ConfigTheme(Theme);


            FirstSettingDirs = myTextBox1.Text;
            FirstSettingFlag = richTextBox2.Text;
            FirstSettingSubDir = ScanSubDir;
            FirstSettingTheme = Theme;
            label18.Text = "";
        }

        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) label3_Click(null, null);
        }


        private void label14_Click(object sender, EventArgs e)
        {
            label18.Text = "アイコンの削除中";
            label18.Update();
            Update();

            DirectoryInfo target = new DirectoryInfo(@"icons\");
            foreach (FileInfo file in target.GetFiles()) file.Delete();
            foreach (DirectoryInfo dir in target.GetDirectories()) dir.Delete(true);

            IconsDelete = true;
            label18.Text = "アイコンの削除が完了しました";
            label18.Update();
            Update();
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            panel1.Select();
        }

        private void label16_Click(object sender, EventArgs e)
        {
            ThemeTmp = 0;

            label17.BackColor = Theme == 1 ? rgb(50, 50, 50) : rgb(187, 196, 200);
            label17.ForeColor = ForeColor;
            label16.BackColor = rgb(0, 150, 136);
            label16.ForeColor = rgb(255, 255, 255);
        }

        private void label17_Click(object sender, EventArgs e)
        {
            ThemeTmp = 1;

            label16.BackColor = Theme == 1 ? rgb(50, 50, 50) : rgb(187, 196, 200);
            label16.ForeColor = ForeColor;
            label17.BackColor = rgb(0, 150, 136);
            label17.ForeColor = rgb(255, 255, 255);
        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            FirstSettingDirs = myTextBox1.Text;
            FirstSettingFlag = richTextBox2.Text;
            FirstSettingSubDir = ScanSubDir;
            FirstSettingTheme = Theme;

            ThemeTmp=Theme;
        }

        public void ConfigTheme(int ThemeNum)
        {
            Theme = ThemeNum;
            if (Theme == 0) label16_Click(null, null);
            else if (Theme == 1) label17_Click(null, null);
            if (ThemeNum == 0)
            {
                BackColor = rgb(207, 216, 220);
                ForeColor = rgb(33, 33, 33);
                panel1.BackColor = BackColor;
            }
            else if (ThemeNum == 1)
            {
                BackColor = rgb(30, 30, 30);
                ForeColor = Color.White;
                panel1.BackColor = BackColor;
            }

            Control[] Buttons = new Control[] { label16, label17, label14, label2, label3, label8, label16, myTextBox1, richTextBox2 };
            foreach (Control ButtonOne in Buttons)
            {
                ButtonOne.BackColor = Theme == 1 ? rgb(50, 50, 50) : rgb(187, 196, 200);
                ButtonOne.ForeColor = ForeColor;
            }

            if (Theme == 0) label16_Click(null, null);
            else if (Theme == 1) label17_Click(null, null);
        }
        private void ButtonHover(object sender, EventArgs e)
        {
            if (Theme == 0)
            {
                ((Control)sender).BackColor = Color.White;
                ((Control)sender).ForeColor = ForeColor;
            }
            else if (Theme == 1) ((Control)sender).BackColor = rgb(84, 84, 84);
        }
        private void ButtonLeave(object sender, EventArgs e)
        {
            if (Theme == 0)
            {
                ((Control)sender).BackColor = rgb(187, 196, 200);
                if (ThemeTmp == 0 && ((Control)sender).Name == "label16"
                || ThemeTmp == 1 && ((Control)sender).Name == "label17") ((Control)sender).ForeColor = Color.White;
            }
            else if (Theme == 1) ((Control)sender).BackColor = rgb(50, 50, 50);

            if (ThemeTmp == 0 && ((Control)sender).Name == "label16" 
                || ThemeTmp == 1 && ((Control)sender).Name == "label17") ((Control)sender).BackColor = rgb(0, 150, 136);
        }

    }

}