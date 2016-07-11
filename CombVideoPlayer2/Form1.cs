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

namespace CombVideoPlayer2
{
    public partial class Form1 : Form
    {
        private Dictionary<string, string> VideosHashToPath = new Dictionary<string, string>();
        private List<string> PlayHistory = new List<string>();
        private string[] BenNames = new string[] { "par-", "ico-", "nam-", "exn-" }, VideosPlayList, 
            PlayModeText = { "\uf01e", "\uf01e" }, PlayStyleText = { "\uf079", "\uf074" };
        private string NowPlayingHash = "";
        private long MaxTime = (long)0, NowTimeLong=0;
        private bool SliderStop = false, SliderMove = false, PlayEnd = false, FullScreenMode, HideControl = false;
        int VideosPlayListPos = -1, PlayMode = 0, PlayStyle = 0, Theme = 0, PrevPanel3Height = 0;
        private Point mousePoint;
        Random randObj = new Random();
        Point FullScreenPrevPoint;
        Size FullScreenPrevSize;

        Form2 f2 = new Form2();
        public bool ExitFlag = false, ScanSubDir;
        public string[] ScanDirs;
        public string ScanEx;

        private delegate void AddVideosListDelegate(Panel[] parents);
        private delegate void IconImageShowDelegate(string VideoHash);
        private delegate void MaxTimeChangeDelegate(string TimeText);
        private delegate void TimeChangeDelefate(string TimeText, long TimeLong);
        private delegate void VideoPlayEndReachedDelegate();
        private delegate void DirScanEndDelegate();
        private delegate void TitleUpdateDelegate(string NewTitle);
        


        public Form1()
        {
            InitializeComponent();
        }

        private void AddVideosList(Panel[] parents)
        {
            //UI描画
            panel2.Controls.AddRange(parents);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            f2.F2StartUp(this);

            if (!Directory.Exists(@"icons\")) Directory.CreateDirectory(@"icons\");
            if(!File.Exists("readme.txt")) File.WriteAllText(@"readme.txt", Resources.readme);

            WindowState = FormWindowState.Maximized;
            pictureBox5.Width = 0;
            label14.Location = new Point(panel2.Left + (panel2.Width - label14.Width) / 2, panel2.Top + 20);
            Form1_Resize(sender, e);


            List<string> CScanDirPath = new List<string>();
            string CScanFileFlag = "";
            bool CScanSubDir = false;
            int ThemeNum = 0;
            if (File.Exists(@"comb_video_player_2.conf"))
            {
                StreamReader sr = new StreamReader(@"comb_video_player_2.conf", Encoding.Unicode);
                string ConfigStr = sr.ReadToEnd();
                sr.Close();

                string[] ConfigLines = ConfigStr.Split('\n');
                foreach (string ConfigLine in ConfigLines)
                {
                    string[] ConfigObj = ConfigLine.Split(':');
                    if (ConfigObj.Length >= 2)
                    {
                        for (int i = 2; i < ConfigObj.Length; ++i) ConfigObj[1] += ":"+ConfigObj[i];
                        if (ConfigObj[0] == "dir") CScanDirPath.Add(ConfigObj[1]);
                        else if (ConfigObj[0] == "ex") CScanFileFlag += ConfigObj[1];
                        else if (ConfigObj[0] == "flag") CScanSubDir = Convert.ToBoolean(ConfigObj[1]);
                        else if (ConfigObj[0] == "color") ThemeNum = int.Parse(ConfigObj[1]);
                    }
                }
            }

            string DirTextLines = "";
            foreach(string Line in CScanDirPath)
            {
                if (DirTextLines != "") DirTextLines += "\r\n";
                DirTextLines += Line;
            }
            f2.myTextBox1.Text = DirTextLines;
            f2.richTextBox2.Text = CScanFileFlag;
            f2.ScanSubDir = CScanSubDir;
            f2.label8.Text = CScanSubDir ? "ON" : "OFF";
            ScanSubDir = CScanSubDir;

            VideoListRefresh(CScanDirPath.ToArray(), CScanFileFlag.Replace(" ", "").Replace(",", "|"), CScanSubDir);

            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(@"libs\fontawesome-webfont.ttf");

            Font f = new Font(pfc.Families[0], 12);

            panel5.Font = f;
            label13.Font = f;
            label16.Font = f;

            pfc.Dispose();
            PlayerUiSetting();

            ApplicationTheme(ThemeNum);
            f2.ConfigTheme(ThemeNum);
        }

        public void VideoListRefresh(string[] DirPath, string ExScan, bool SubDir)
        {
            label14.Show();
            label14.Update();

            ScanDirs = DirPath;
            ScanEx = ExScan;
            ScanSubDir = SubDir;

            vlcControl1.Stop();
            for (int i = panel2.Controls.Count - 1; 0 <= i; i--) panel2.Controls[i].Dispose();
            panel2.Refresh();
            panel2.Update();
            panel2.Invalidate();

            PlayHistory = new List<string>();
            VideosHashToPath = new Dictionary<string, string>();
            VideosPlayList = new string[0];
            NowPlayingHash = "";
            MaxTime = 0;
            SliderStop = false;
            SliderMove = false;
            PlayEnd = false;
            VideosPlayListPos = -1;


            (new Thread(new ThreadStart(PlayWorker))).Start();
        }

        public void ApplicationTheme(int ThemeNum)
        {
            Theme = ThemeNum;
            if(ThemeNum == 0)
            {
                BackColor = rgb(207, 216, 220);
                ForeColor = rgb(33, 33, 33);
                panel2.BackColor = rgb(187, 196, 200);
            }
            else if(ThemeNum == 1)
            {
                BackColor = rgb(30,30,30);
                ForeColor = Color.White;
                panel2.BackColor = rgb(30, 30, 30);
            }

            Control[] Buttons = new Control[] {label7, label8, label9, label10, label11, label12, label13, label16};
            foreach(Control ButtonOne in Buttons) ButtonLeave(ButtonOne, null);

            for (int i = panel2.Controls.Count - 1; 0 <= i; i--) VideoListLeave(panel2.Controls[i], null);
        }

        private void PlayerUiSetting()
        {
            label7.Text = Regex.Unescape("\uf049");
            label8.Text = Regex.Unescape("\uf04c");
            label9.Text = Regex.Unescape("\uf050");
            label10.Text = Regex.Unescape(PlayModeText[0]);
            label11.Text = Regex.Unescape(PlayStyleText[0]);
            label12.Text = Regex.Unescape("\uf0b2");
            label13.Text = Regex.Unescape("\uf013");
            label16.Text = Regex.Unescape("\uf063");
        }

        private void ButtonHover(object sender, EventArgs e)
        {
            if(Theme==0) ((Label)sender).BackColor = Color.White;
            else if (Theme == 1)((Label)sender).BackColor = rgb(84,84,84);
        }
        private void ButtonLeave(object sender, EventArgs e)
        {
            if (Theme == 0) ((Label)sender).BackColor = rgb(187, 196, 200);
            else if (Theme == 1) ((Label)sender).BackColor = rgb(50, 50, 50);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            WindowRangeDraw();
            label13.Location = new Point(Width - 33, Height - 33);
        }

        private void IconImageShow(string Hash)
        {
            Control[] cs = panel2.Controls.Find(string.Concat("ico-", Hash), true);
            if (cs.Length != 0)
            {
                ((PictureBox)cs[0]).ImageLocation = string.Concat("icons\\", Hash.Split('-')[0]);
            }
        }


        private void MaxTimeChange(string TimeText)
        {
            label2.Text = TimeText;
        }

        private void OnVlcControlNeedLibDirectory(object sender, VlcLibDirectoryNeededEventArgs e)
        {
            e.VlcLibDirectory = new DirectoryInfo(Path.GetFullPath("libs\\"));
        }

        private void pictureBox6_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                mousePoint = new Point(e.X, e.Y);
                label6.Show();
            }
        }

        private void pictureBox6_MouseEnter(object sender, EventArgs e)
        {
            PictureBox top = pictureBox6;
            top.Top = top.Top - 10;
            PictureBox height = pictureBox6;
            height.Height = height.Height + 20;
            SliderStop = true;
        }

        private void pictureBox6_MouseLeave(object sender, EventArgs e)
        {
            PictureBox top = pictureBox6;
            top.Top = top.Top + 10;
            PictureBox height = pictureBox6;
            height.Height = height.Height - 20;
            SliderStop = false;
        }

        private void pictureBox6_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                PictureBox left = pictureBox6;
                left.Left = left.Left + (e.X - mousePoint.X);
                SliderMove = true;
                long SliderTimeLong = (long)((double)(pictureBox6.Left + pictureBox6.Width / 2) / (double)pictureBox4.Width * (double)MaxTime) / (long)1000;
                label6.Text = string.Concat(string.Format("{0:00}", SliderTimeLong / (long)60), ":", string.Format("{0:00}", SliderTimeLong % (long)60));
                label6.Left = pictureBox6.Left - (label6.Width - pictureBox6.Width) / 2;
            }
        }

        private void pictureBox6_MouseUp(object sender, MouseEventArgs e)
        {
            if (SliderMove)
            {
                vlcControl1.Time = (long)((double)(pictureBox6.Left + pictureBox6.Width / 2) / (double)pictureBox4.Width * (double)MaxTime);
                SliderMove = false;
            }
            label6.Hide();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            //ゾンビプロセスと化す場合があるからプロセスを殺す
            Process hProcess = Process.GetCurrentProcess();

            hProcess.Kill();

            hProcess.Close();
            hProcess.Dispose();
        }

        private void PlayWorker()
        {
            try
            {
                //ファイルリスト作成
                List<string> VideosPath = new List<string>();
                string[] ScanExs = ScanEx.Split('|');

                foreach (string DirPath in ScanDirs)
                {
                    foreach (string ExStr in ScanExs)
                    {
                        try
                        {
                            VideosPath.AddRange(Directory.GetFiles(DirPath, "*." + ExStr, ScanSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                        }
                        catch { }
                    }
                }


                //UI用配列作成
                Dictionary<string, string> VideoHash = new Dictionary<string, string>();
                Panel[] ParentPanels = new Panel[(int)VideosPath.Count];
                PictureBox[] IconsBox = new PictureBox[(int)VideosPath.Count];
                Label[] NameLabel = new Label[(int)VideosPath.Count];
                Label[] ExNameLabel = new Label[(int)VideosPath.Count];
                VideosPlayList = new string[(int)VideosPath.Count];
                for (int i = 0; i < (int)VideosPath.Count; i++)
                {
                    string VideoPath = VideosPath[i];
                    VideosPlayList[i] = VideoPath;
                    byte[] byteValue = Encoding.UTF8.GetBytes(VideoPath);
                    byte[] hashValue = (new SHA256CryptoServiceProvider()).ComputeHash(byteValue);
                    StringBuilder hashedText = new StringBuilder();
                    for (int ii = 0; ii < (int)hashValue.Length; ii++)
                    {
                        hashedText.AppendFormat("{0:X2}", hashValue[ii]);
                    }
                    VideoHash.Add(VideoPath, hashedText.ToString());
                    VideosHashToPath.Add(hashedText.ToString(), VideoPath);
                    int prevPanelBottom = 0;
                    if (i > 0) prevPanelBottom = ParentPanels[i - 1].Bottom;
                    ParentPanels[i] = new Panel();
                    ParentPanels[i].Size = new Size(panel2.Width - 20, 50);
                    ParentPanels[i].Location = new Point(0, prevPanelBottom);
                    ParentPanels[i].Click += new EventHandler(VideoListClick);
                    ParentPanels[i].MouseEnter += new EventHandler(VideoListHover);
                    ParentPanels[i].MouseLeave += new EventHandler(VideoListLeave);
                    ParentPanels[i].Name = string.Concat("par-", hashedText.ToString() + "-" + i);
                    IconsBox[i] = new PictureBox();
                    IconsBox[i].Size = new Size(50, 50);
                    IconsBox[i].Location = new Point(10, 0);
                    IconsBox[i].SizeMode = PictureBoxSizeMode.Zoom;
                    IconsBox[i].Name = string.Concat("ico-", hashedText.ToString() + "-" + i);
                    IconsBox[i].ErrorImage = Resources.loadError;
                    IconsBox[i].Click += new EventHandler(VideoListClick);
                    IconsBox[i].MouseEnter += new EventHandler(VideoListHover);
                    IconsBox[i].MouseLeave += new EventHandler(VideoListLeave);
                    NameLabel[i] = new Label();
                    NameLabel[i].Text = Path.GetFileNameWithoutExtension(VideoPath);
                    NameLabel[i].Font = new Font("Meiryo UI", 12f);
                    NameLabel[i].Size = new Size(150, 32);
                    NameLabel[i].Location = new Point(60, 0);
                    NameLabel[i].TextAlign = ContentAlignment.MiddleCenter;
                    NameLabel[i].Click += new EventHandler(VideoListClick);
                    NameLabel[i].MouseEnter += new EventHandler(VideoListHover);
                    NameLabel[i].MouseLeave += new EventHandler(VideoListLeave);
                    NameLabel[i].Name = string.Concat("nam-", hashedText.ToString() + "-" + i);
                    ExNameLabel[i] = new Label();
                    ExNameLabel[i].Text = Path.GetExtension(VideoPath).Replace(".", "");
                    ExNameLabel[i].Font = new Font("Meiryo UI", 9f);
                    ExNameLabel[i].Size = new Size(150, 18);
                    ExNameLabel[i].Location = new Point(60, 32);
                    ExNameLabel[i].TextAlign = ContentAlignment.MiddleRight;
                    ExNameLabel[i].Click += new EventHandler(VideoListClick);
                    ExNameLabel[i].MouseEnter += new EventHandler(VideoListHover);
                    ExNameLabel[i].MouseLeave += new EventHandler(VideoListLeave);
                    ExNameLabel[i].Name = string.Concat("exn-", hashedText.ToString() + "-" + i);
                    ParentPanels[i].Controls.AddRange(new Control[] { IconsBox[i], NameLabel[i], ExNameLabel[i] });

                }
                Invoke(new AddVideosListDelegate(AddVideosList), new object[] { ParentPanels });
                for (int k = 0; k < (int)VideosPath.Count; k++)
                {
                    object[] argsObj = new object[] { VideosPath[k], VideoHash[VideosPath[k]], k.ToString() };
                    new Thread(new ParameterizedThreadStart(CreateIcon)).Start(argsObj);
                }

            }
            catch { }

            //UI描画の終了を通知
            Invoke(new DirScanEndDelegate(DirScanEnd));
        }

        private void CreateIcon(object args)
        {
            string VideoPath = (string)((object[])args)[0];
            string VideoHashOne = (string)((object[])args)[1];
            string k = (string)((object[])args)[2];
            //アイコンの作成
            try
            {
                string output = string.Concat("icons\\", VideoHashOne);
                if (!File.Exists(output))
                {
                    string arguments = string.Format("-ss {1} -i \"{0}\" -vframes 1 -f image2 \"{2}\"", VideoPath, 0, output);
                    Process.Start(new ProcessStartInfo("ffmpeg.exe", arguments)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }).WaitForExit();
                }
                //アイコンの描画
                Invoke(new IconImageShowDelegate(IconImageShow), new object[] { VideoHashOne + "-" + k });
            }
            catch { }
        }

        private void DirScanEnd()
        {
            label14.Hide();
        }

        public Color rgb(int r, int g, int b)
        {
            return Color.FromArgb(r, g, b);
        }

        private void TimeChange(string TimeText, long TimeLong)
        {
            NowTimeLong = TimeLong;
            label3.Text = TimeText;
            pictureBox5.Width = (int)((double)TimeLong / (double)MaxTime * (double)pictureBox4.Width);
            if (!SliderStop)
            {
                pictureBox6.Left = pictureBox5.Width - pictureBox6.Width / 2;
            }
        }

        private void VideoListClick(object sender, EventArgs e)
        {
            string[] benNames = BenNames;
            for (int i = 0; i < (int)benNames.Length; i++)
            {
                string BenName = benNames[i];
                Control[] cs = panel2.Controls.Find(string.Concat(BenName, NowPlayingHash), true);
                if (cs.Length != 0)
                {
                    cs[0].BackColor = panel2.BackColor;
                }
            }
            NowPlayingHash = ((Control)sender).Name.Split(new char[] { '-' })[1]+ "-" + ((Control)sender).Name.Split(new char[] { '-' })[2];

            vlcControl1.Play(new Uri(VideosHashToPath[((Control)sender).Name.Split(new char[] { '-' })[1]]));
            PlayHistory.Add(NowPlayingHash);

            string[] strArrays = BenNames;
            VideosPlayListPos = int.Parse(((Control)sender).Name.Split(new char[] { '-' })[2]);
            for (int j = 0; j < (int)strArrays.Length; j++)
            {
                string BenName = strArrays[j];
                Control[] cs = panel2.Controls.Find(string.Concat(BenName, NowPlayingHash), true);
                if (cs.Length != 0)
                {
                    cs[0].BackColor = rgb(76, 175, 80);
                }
            }
        }

        private void VideoListHover(object sender, EventArgs e)
        {
            string AllHash = ((Control)sender).Name.Split(new char[] { '-' })[1] + "-" + ((Control)sender).Name.Split(new char[] { '-' })[2];
            string[] benNames = BenNames;
            for (int i = 0; i < (int)benNames.Length; i++)
            {
                string BenName = benNames[i];
                Control[] cs = panel2.Controls.Find(string.Concat(BenName, AllHash), true);
                if (cs.Length != 0)
                {
                    cs[0].BackColor = rgb(0, 150, 136);
                }
            }
        }

        private void VideoListLeave(object sender, EventArgs e)
        {
            string AllHash = ((Control)sender).Name.Split(new char[] { '-' })[1] + "-" + ((Control)sender).Name.Split(new char[] { '-' })[2];
            string[] benNames = BenNames;
            for (int i = 0; i < (int)benNames.Length; i++)
            {
                string BenName = benNames[i];
                if (AllHash == NowPlayingHash)
                {
                    Control[] cs = panel2.Controls.Find(string.Concat(BenName, AllHash), true);
                    if (cs.Length != 0)
                    {
                        cs[0].BackColor = rgb(76, 175, 80);
                    }
                }
                else
                {
                    Control[] cs = panel2.Controls.Find(string.Concat(BenName, AllHash), true);
                    if (cs.Length != 0)
                    {
                        cs[0].BackColor = panel2.BackColor;
                    }
                }
            }
        }

        private void vlcControl1_LengthChanged(object sender, VlcMediaPlayerLengthChangedEventArgs e)
        {
            TimeSpan timeSpan = new TimeSpan((long)e.NewLength);
            MaxTime = (long)timeSpan.TotalMilliseconds;
            long MaxTimeTmp = MaxTime / (long)1000;
            Invoke(new MaxTimeChangeDelegate(MaxTimeChange), new object[] { string.Concat(string.Format("{0:00}", MaxTimeTmp / (long)60), ":", string.Format("{0:00}", MaxTimeTmp % (long)60)) });
        }

        private void vlcControl1_MediaChanged(object sender, VlcMediaPlayerMediaChangedEventArgs e)
        {
        }

        private void vlcControl1_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            int TimeInt = (int)(e.NewTime / (long)1000);
            Invoke(new TimeChangeDelefate(TimeChange), new object[] { string.Concat(string.Format("{0:00}", TimeInt / 60), ":", string.Format("{0:00}", TimeInt % 60)), e.NewTime });
        }


        private void WindowRangeDraw()
        {
            Bitmap canvas = new Bitmap(Width, Height);
            Graphics g = Graphics.FromImage(canvas);
            Pen p = new Pen(rgb(0, 150, 136), 2f);
            g.DrawRectangle(p, 2, 2, Width - 4, Height - 4);
            p.Dispose();
            g.Dispose();
            BackgroundImage = canvas;

            panel5.Left = (panel1.Width - panel5.Width) / 2;
        }

        private void vlcControl1_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            PlayEnd = true;
        }

        private void label8_Click(object sender, EventArgs e)
        {
            if (vlcControl1.IsPlaying)
            {
                vlcControl1.Pause();
                label8.Text = Regex.Unescape("\uf04b");
            }
            else
            {
                vlcControl1.Play();
                label8.Text = Regex.Unescape("\uf04c");
            }
        }

        private void label11_Click(object sender, EventArgs e)
        {
            ++PlayStyle;
            if (PlayStyle >= PlayStyleText.Length) PlayStyle = 0;
            label11.Text = Regex.Unescape(PlayStyleText[PlayStyle]);
        }

        private void label13_Click(object sender, EventArgs e)
        {
            f2.Visible = !f2.Visible;
        }

        private void vlcControl1_TitleChanged(object sender, VlcMediaPlayerTitleChangedEventArgs e)
        {
            Invoke(new TitleUpdateDelegate(TitleUpdate), new object[] { e.NewTitle });
        }

        private void TitleUpdate(string NewTitle)
        {
            label15.Text = NewTitle;
        }

        private void vlcControl1_MouseClick(object sender, MouseEventArgs e)
        {
            label8_Click(null, null);
        }

        private void vlcControl1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            label12_Click(null, null);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && FullScreenMode) label12_Click(null, null);
            else if (e.KeyCode == Keys.F) label12_Click(null, null);
            else if (e.KeyCode == Keys.N || e.KeyCode == Keys.Left) label9_Click(null, null);
            else if (e.KeyCode == Keys.P || e.KeyCode == Keys.Right) label7_Click(null, null);
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.S) label8_Click(null, null);
            else if (e.KeyCode == Keys.R) label10_Click(null, null);
            else if (e.KeyCode == Keys.T) label11_Click(null, null);
            else if (e.KeyCode == Keys.C) label13_Click(null, null);
            else if (e.KeyCode == Keys.M) label16_Click(null, null);
        }

        private void label16_Click(object sender, EventArgs e)
        {
            HideControl = !HideControl;

            if (HideControl)
            {
                panel3.Height = panel1.Height;
                label16.Location = new Point(panel1.Width - label16.Width - 5, panel1.Height - label16.Height - 5);
                label16.Text = Regex.Unescape("\uf062");
                pictureBox6.Hide();
                label6.Hide();
            }
            else
            {
                label16.Location = new Point(label2.Left - label16.Width - 5, panel5.Top);
                panel3.Height = panel1.Height-62;
                label16.Text = Regex.Unescape("\uf063");
                pictureBox6.Show();
                label6.Show();
            }
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            pictureBox5.Width = (int)((double)NowTimeLong / (double)MaxTime * (double)pictureBox4.Width);
            if (!SliderStop)
            {
                pictureBox6.Left = pictureBox5.Width - pictureBox6.Width / 2;
            }
        }

        private void vlcControl1_Opening(object sender, VlcMediaPlayerOpeningEventArgs e)
        {
            Invoke(new TitleUpdateDelegate(TitleUpdate), new object[] { Path.GetFileNameWithoutExtension(VideosPlayList[VideosPlayListPos]) });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ExitFlag = true;
            e.Cancel = true;
            pictureBox7_Click(null, null);
        }

        private void label12_Click(object sender, EventArgs e)
        {
            FullScreenMode = !FullScreenMode;

            if (FullScreenMode)
            {
                FullScreenPrevPoint = panel1.Location;
                FullScreenPrevSize = panel1.Size;

                panel1.Location = new Point(0, 0);
                panel1.Size = Size;
                label12.Text = Regex.Unescape("\uf066");
            }
            else
            {
                panel1.Location = FullScreenPrevPoint;
                panel1.Size = FullScreenPrevSize;
                label12.Text = Regex.Unescape("\uf0b2");
            }

            WindowRangeDraw();
        }

        private void vlcControl1_EncounteredError(object sender, VlcMediaPlayerEncounteredErrorEventArgs e)
        {
        }

        private void vlcControl1_Stopped(object sender, VlcMediaPlayerStoppedEventArgs e)
        {
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (PlayEnd)
            {
                if (PlayMode == 0) NextPlay();
                else vlcControl1.Play(new Uri(VideosHashToPath[NowPlayingHash.Split('-')[0]]));
                PlayEnd = false;
            }
        }

        private void label10_Click(object sender, EventArgs e)
        {
            ++PlayMode;
            if (PlayMode >= PlayModeText.Length) PlayMode = 0;
            label10.Text = Regex.Unescape(PlayModeText[PlayMode]);
            if(PlayMode == 1)
            {
                Bitmap canvas = new Bitmap(label10.Width, label10.Height);
                Graphics g = Graphics.FromImage(canvas);

                Font fnt = new Font("Meiryo UI", 5);
                g.DrawString("1", fnt, new SolidBrush(label10.ForeColor), 17, 2);

                fnt.Dispose();
                g.Dispose();

                label10.Image = canvas;
            }
            else label10.Image = null;
        }

        private void label9_Click(object sender, EventArgs e)
        {

            NextPlay();
        }
        private void NextPlay()
        {
            int NextPlayPos = VideosPlayListPos + 1;
            if (NextPlayPos > VideosPlayList.Length-1 || VideosPlayListPos < 0) NextPlayPos = VideosPlayListPos = 0;
            if (PlayStyle == 1) NextPlayPos = randObj.Next(0, VideosPlayList.Length - 1);

            byte[] byteValue = Encoding.UTF8.GetBytes(VideosPlayList[NextPlayPos]);
            byte[] hashValue = (new SHA256CryptoServiceProvider()).ComputeHash(byteValue);
            StringBuilder hashedText = new StringBuilder();
            for (int ii = 0; ii < (int)hashValue.Length; ii++)
            {
                hashedText.AppendFormat("{0:X2}", hashValue[ii]);
            }


            Control[] cs = panel2.Controls.Find("par-" + hashedText.ToString() + "-" + NextPlayPos, true);
            if (cs.Length != 0) VideoListClick(cs[0], null);

            VideosPlayListPos = NextPlayPos;
            NowPlayingHash = hashedText.ToString() + "-" + NextPlayPos;

        }
        private void label7_Click(object sender, EventArgs e)
        {

            int NextPlayPos = VideosPlayListPos - 1;
            if (NextPlayPos < 0) NextPlayPos = VideosPlayList.Length-1;
            if(PlayStyle == 1)
            {
                PlayHistory.Remove(PlayHistory.Last());
                if (PlayHistory.Count < 1)
                {
                    NextPlay();
                    return;
                }
                else {
                    NextPlayPos = int.Parse(PlayHistory.Last().Split('-')[1]);
                    PlayHistory.Remove(PlayHistory.Last());
                }
            }


            byte[] byteValue = Encoding.UTF8.GetBytes(VideosPlayList[NextPlayPos]);
            byte[] hashValue = (new SHA256CryptoServiceProvider()).ComputeHash(byteValue);
            StringBuilder hashedText = new StringBuilder();
            for (int ii = 0; ii < (int)hashValue.Length; ii++)
            {
                hashedText.AppendFormat("{0:X2}", hashValue[ii]);
            }


            Control[] cs = panel2.Controls.Find("par-" + hashedText.ToString() + "-" + NextPlayPos, true);
            if (cs.Length != 0) VideoListClick(cs[0], null);

            VideosPlayListPos = NextPlayPos;
            NowPlayingHash = hashedText.ToString() + "-" + NextPlayPos;
        }
    }
}