using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirScenes;
using Client.MirSounds;
using Microsoft.DirectX.Direct3D;
using Font = System.Drawing.Font;

namespace Client
{
    public partial class CMain : Form
    {
        public static MirControl DebugBaseLabel, HintBaseLabel;
        public static MirLabel DebugTextLabel, HintTextLabel, ScreenshotTextLabel;
        public static Graphics Graphics;

        /// <summary>
        /// Mouse Point
        /// </summary>
        public static Point MPoint;

        public readonly static Stopwatch Timer = Stopwatch.StartNew();
        public readonly static DateTime StartTime = DateTime.Now;
        public static long Time, OldTime;
        public static DateTime Now { get { return StartTime.AddMilliseconds(Time); } }
        public static readonly Random Random = new Random();

        public static bool DebugOverride;

        private static long _fpsTime;
        private static int _fps;
        public static int FPS;

        public static bool Shift, Alt, Ctrl, Tilde;
        public static KeyBindSettings InputKeys = new KeyBindSettings();

        public CMain()
        {
            InitializeComponent();

            Application.Idle += Application_Idle;

            MouseClick += CMain_MouseClick;
            MouseDown += CMain_MouseDown;
            MouseUp += CMain_MouseUp;
            MouseMove += CMain_MouseMove;
            MouseDoubleClick += CMain_MouseDoubleClick;
            KeyPress += CMain_KeyPress;
            KeyDown += CMain_KeyDown;
            KeyUp += CMain_KeyUp;
            Deactivate += CMain_Deactivate;
            MouseWheel += CMain_MouseWheel;


            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            FormBorderStyle = Settings.FullScreen ? FormBorderStyle.None : FormBorderStyle.Fixed3D;
            Graphics = CreateGraphics();
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Graphics.CompositingQuality = CompositingQuality.HighQuality;
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Graphics.TextContrast = 0;
        }

        private void CMain_Load(object sender, EventArgs e)
        {
            try
            {
                ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
                //画面创建
                DXManager.Create();
                //声音创建
                SoundManager.Create();
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        private void CMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CMain.Time < GameScene.LogTime)
            {
                GameScene.Scene.ChatDialog.ReceiveChat("Cannot leave game for " + (GameScene.LogTime - CMain.Time) / 1000 + " seconds.", ChatType.System);
                e.Cancel = true;
            }
            if (GameScene.Scene != null)
            {
                e.Cancel = Program.CanClose;
                GameScene.Scene.CloseGame();
            }
        }

        #region 空闲状态下事件处理

        /// <summary>
        /// 空闲状态时执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Application_Idle(object sender, EventArgs e)
        {
            try
            {
                while (AppStillIdle)
                {
                    //更新时间
                    UpdateTime();
                    //更新环境
                    UpdateEnviroment();
                    //渲染环境
                    RenderEnvironment();
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// （空闲状态下）更新时间
        /// </summary>
        private static void UpdateTime()
        {
            Time = Timer.ElapsedMilliseconds;
        }

        /// <summary>
        /// （空闲状态下）更新环境
        /// </summary>
        private static void UpdateEnviroment()
        {  

            if (Time >= _fpsTime)
            {
                _fpsTime = Time + 1000;
                FPS = _fps;
                _fps = 0;
                DXManager.Clean(); //每秒清理一次
            }
            else
                _fps++;

            //（空闲状态下）处理包事务
            Network.Process();

            //处理场景事务
            if (MirScene.ActiveScene != null)
                MirScene.ActiveScene.Process();

            //处理控件动画
            for (int i = 0; i < MirAnimatedControl.Animations.Count; i++)
                MirAnimatedControl.Animations[i].UpdateOffSet();
            //处理按钮动画
            for (int i = 0; i < MirAnimatedButton.Animations.Count; i++)
                MirAnimatedButton.Animations[i].UpdateOffSet();

            //处理画面文字移动
            CreateHintLabel();
            //处理调试文字（窗体头部）
            CreateDebugLabel();
 
        }

        /// <summary>
        /// （空闲状态下）渲染环境
        /// </summary>
        private static void RenderEnvironment()
        {
            try
            {
                if (DXManager.DeviceLost)
                {
                    DXManager.AttemptReset();
                    Thread.Sleep(1);
                    return;
                }
                else
                {
                    //1、清屏
                    DXManager.Device.Clear(ClearFlags.Target, Color.CornflowerBlue, 0, 0);
                    //2、开启 Scene，这里的 Scene 用于分组和管理每一帧的 Draw Call
                    DXManager.Device.BeginScene();
                    //3、开启透明度混合
                    DXManager.Sprite.Begin(SpriteFlags.AlphaBlend);
                    //4、设置 Render Target
                    DXManager.SetSurface(DXManager.MainSurface);

                    //开始画LoginScene界面（ActiveScene初始化为LoginScene）
                    if (MirScene.ActiveScene != null)
                    {
                        //5、提交当前 Scene 的 Draw Call
                        MirScene.ActiveScene.Draw();
                    }

                    DXManager.Sprite.End();
                    //6、通过 EndScene 提交 Command Buffer
                    DXManager.Device.EndScene();
                    //7、通过 Present 进行 swap 上屏
                    DXManager.Device.Present();
                }
            }
            catch (DeviceLostException)
            {
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());

                DXManager.AttemptRecovery();
            }
        }

        /// <summary>
        /// 处理调试文字（窗体头部）
        /// </summary>
        private static void CreateDebugLabel()
        {
            if (!Settings.DebugMode) return;

            if (DebugBaseLabel == null || DebugBaseLabel.IsDisposed)
            {
                DebugBaseLabel = new MirControl
                    {
                        BackColour = Color.FromArgb(50, 50, 50),
                        Border = true,
                        BorderColour = Color.Black,
                        DrawControlTexture = true,
                        Location = new Point(5, 5),
                        NotControl = true,
                        Opacity = 0.5F
                    };
            }
            
            if (DebugTextLabel == null || DebugTextLabel.IsDisposed)
            {
                DebugTextLabel = new MirLabel
                {
                    AutoSize = true,
                    BackColour = Color.Transparent,
                    ForeColour = Color.White,
                    Parent = DebugBaseLabel,
                };

                DebugTextLabel.SizeChanged += (o, e) => DebugBaseLabel.Size = DebugTextLabel.Size;
            }

            if (DebugOverride) return;
            
            string text;
            if (MirControl.MouseControl != null)
            {
                text = string.Format("FPS: {0}", FPS);

                if (MirControl.MouseControl is MapControl)
                {
                    text += string.Format(", Co Ords: {0}", MapControl.MapLocation);

                    //text += "\r\n";

                    //var cell = GameScene.Scene.MapControl.M2CellInfo[MapControl.MapLocation.X, MapControl.MapLocation.Y];

                    //if (cell != null)
                    //{
                    //    text += string.Format("BackImage : {0}. BackIndex : {1}. MiddleImage : {2}. MiddleIndex {3}. FrontImage : {4}. FrontIndex : {5}", cell.BackImage, cell.BackIndex, cell.MiddleImage, cell.MiddleIndex, cell.FrontImage, cell.FrontIndex);
                    //}
                }

                if (MirScene.ActiveScene is GameScene)
                {
                    //text += "\r\n";
                    text += string.Format(", Objects: {0}", MapControl.Objects.Count);
                }
                if (MirObjects.MapObject.MouseObject != null)
                {
                    text += string.Format(", Target: {0}", MirObjects.MapObject.MouseObject.Name);
                }
                else
                {
                    text += string.Format(", Target: none");
                }
            }
            else
            {
                text = string.Format("FPS: {0}", FPS);
            }
            

            DebugTextLabel.Text = text;
        }

        public static void SendDebugMessage(string text)
        {
            if (!Settings.DebugMode) return;

            if (DebugBaseLabel == null || DebugTextLabel == null)
            {
                CreateDebugLabel();//处理调试文字（窗体头部）
            }

            DebugOverride = true;

            DebugTextLabel.Text = text;
        }

        /// <summary>
        /// 处理画面文字移动
        /// </summary>
        private static void CreateHintLabel()
        {
            //  Could be this
            try

            {
                if (HintBaseLabel == null || HintBaseLabel.IsDisposed)
                {
                    HintBaseLabel = new MirControl
                    {
                        BackColour = Color.FromArgb(128, 128, 50),
                        Border = true,
                        DrawControlTexture = true,
                        BorderColour = Color.Yellow,
                        ForeColour = Color.Yellow,
                        Parent = MirScene.ActiveScene,
                        NotControl = true,
                        Opacity = 0.5F
                    };
                }


                if (HintTextLabel == null || HintTextLabel.IsDisposed)
                {
                    HintTextLabel = new MirLabel
                    {
                        AutoSize = true,
                        BackColour = Color.Transparent,
                        ForeColour = Color.White,
                        Parent = HintBaseLabel,
                    };

                    HintTextLabel.SizeChanged += (o, e) => HintBaseLabel.Size = HintTextLabel.Size;
                }

                if (MirControl.MouseControl == null || string.IsNullOrEmpty(MirControl.MouseControl.Hint))
                {
                    HintBaseLabel.Visible = false;
                    return;
                }

                HintBaseLabel.Visible = true;

                HintTextLabel.Text = MirControl.MouseControl.Hint;

                Point point = MPoint.Add(-HintTextLabel.Size.Width, 20);

                if (point.X + HintBaseLabel.Size.Width >= Settings.ScreenWidth)
                    point.X = Settings.ScreenWidth - HintBaseLabel.Size.Width - 1;
                if (point.Y + HintBaseLabel.Size.Height >= Settings.ScreenHeight)
                    point.Y = Settings.ScreenHeight - HintBaseLabel.Size.Height - 1;

                if (point.X < 0)
                    point.X = 0;
                if (point.Y < 0)
                    point.Y = 0;

                HintBaseLabel.Location = point;
            }
            catch (Exception ex)
            {
                SaveError( string.Format("ERROR {0}\r\nCMain.exe", ex.ToString()));
            }
        }

        #endregion

        #region 标准事件处理

        /// <summary>
        /// 游戏窗体处于非活动状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CMain_Deactivate(object sender, EventArgs e)
        {
            MapControl.MapButtons = MouseButtons.None;
            Shift = false;
            Alt = false;
            Ctrl = false;
            Tilde = false;
        }

        /// <summary>
        /// 画面键盘按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_KeyDown(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (e.KeyCode == Keys.Oem8)
                CMain.Tilde = true;

            try
            {
                if (e.Alt && e.KeyCode == Keys.Enter)
                {
                    ToggleFullScreen();
                    return;
                }

                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnKeyDown(e);

            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标左键移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.FullScreen)
                Cursor.Clip = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);

            MPoint = Program.Form.PointToClient(Cursor.Position);

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseMove(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面键盘抬起
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_KeyUp(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;
            bool flag = e.KeyCode == Keys.Oem8;
            if (flag)
            {
                Tilde = false;
            }
            bool flag2 = e.KeyCode == InputKeys.GetKey(KeybindOptions.Screenshot);
            if (flag2)
            {
                Program.Form.CreateScreenShot();
            }
            try
            {
                bool flag3 = MirScene.ActiveScene != null;
                if (flag3)
                {
                    MirScene.ActiveScene.OnKeyUp(e);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面键盘按键事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnKeyPress(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标左键双击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseClick(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标左键抬起
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseUp(object sender, MouseEventArgs e)
        {
            MapControl.MapButtons &= ~e.Button;
            if (!MapControl.MapButtons.HasFlag(MouseButtons.Right))
                GameScene.CanRun = false;

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseUp(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标左键按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (Program.Form.ActiveControl is TextBox)
            {
                MirTextBox textBox = Program.Form.ActiveControl.Tag as MirTextBox;

                if (textBox != null && textBox.CanLoseFocus)
                    Program.Form.ActiveControl = null;
            }

            if (e.Button == MouseButtons.Right && (GameScene.SelectedCell != null || GameScene.PickedUpGold))
            {
                GameScene.SelectedCell = null;
                GameScene.PickedUpGold = false;
                return;
            }

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseDown(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标左键单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseClick(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 画面鼠标滚轮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CMain_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseWheel(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        #endregion

        #region 其他方法

        /// <summary>
        /// 截屏处理
        /// </summary>
        public void CreateScreenShot()
        {
            Point location = PointToClient(Location);

            location = new Point(-location.X, -location.Y);

            string text = string.Format("[{0} Server {1}] {2} {3:hh\\:mm\\:ss}", 
                Settings.P_ServerName.Length > 0 ? Settings.P_ServerName : "Crystal", 
                MapControl.User != null ? MapControl.User.Name : "", 
                Now.ToShortDateString(), 
                Now.TimeOfDay);

            using (Bitmap image = GetImage(Handle, new Rectangle(location, ClientSize)))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                StringFormat sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 3, 10), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 9), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 5, 10), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 11), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.White, new Point((Settings.ScreenWidth / 2) + 4, 10), sf);//SandyBrown               

                string path = Path.Combine(Application.StartupPath, @"Screenshots\");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                int count = Directory.GetFiles(path, "*.png").Length;

                image.Save(Path.Combine(path, string.Format("Image {0}.Png", count)), ImageFormat.Png);
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="ex"></param>
        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", Now, ex, Environment.NewLine));
                }
            }
            catch
            {
            }
        }

        #endregion

        #region DX画面切换处理

        /// <summary>
        /// DX执行全屏切换
        /// </summary>
        private static void ToggleFullScreen()
        {
            Settings.FullScreen = !Settings.FullScreen;

            Program.Form.FormBorderStyle = Settings.FullScreen ? FormBorderStyle.None : FormBorderStyle.Fixed3D;

            DXManager.Parameters.Windowed = !Settings.FullScreen;
            DXManager.Device.Reset(DXManager.Parameters);
            Program.Form.ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
        }

        /// <summary>
        /// DX执行画面大小切换
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetResolution(int width, int height)
        {
            if (Settings.ScreenWidth == width && Settings.ScreenHeight == height) 
                return;

            DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0, 0);
            DXManager.Device.Present();

            DXManager.Device.Dispose();

            Settings.ScreenWidth = width;
            Settings.ScreenHeight = height;
            Program.Form.ClientSize = new Size(width, height);

            DXManager.Create();
        }

        #endregion

        #region ScreenCapture系统API

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr handle);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr handle);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr handle, int width, int height);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr handle, IntPtr handle2);
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr handle, int destX, int desty, int width, int height,
                                         IntPtr handle2, int sourX, int sourY, int flag);
        [DllImport("gdi32.dll")]
        public static extern int DeleteDC(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr handle, IntPtr handle2);
        [DllImport("gdi32.dll")]
        public static extern int DeleteObject(IntPtr handle);

        public static Bitmap GetImage(IntPtr handle, Rectangle r)
        {
            IntPtr sourceDc = GetWindowDC(handle);
            IntPtr destDc = CreateCompatibleDC(sourceDc);

            IntPtr hBmp = CreateCompatibleBitmap(sourceDc, r.Width, r.Height);
            if (hBmp != IntPtr.Zero)
            {
                IntPtr hOldBmp = SelectObject(destDc, hBmp);
                BitBlt(destDc, 0, 0, r.Width, r.Height, sourceDc, r.X, r.Y, 0xCC0020); //0, 0, 13369376);
                SelectObject(destDc, hOldBmp);
                DeleteDC(destDc);
                ReleaseDC(handle, sourceDc);

                Bitmap bmp = Image.FromHbitmap(hBmp);


                DeleteObject(hBmp);

                return bmp;
            }

            return null;
        }
        #endregion

        #region Idle Check 系统API
        private static bool AppStillIdle
        {
            get
            {
                return !PeekMessage(out PeekMsg msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin,
                                               uint messageFilterMax, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct PeekMsg
        {
            private readonly IntPtr hWnd;
            private readonly Message msg;
            private readonly IntPtr wParam;
            private readonly IntPtr lParam;
            private readonly uint time;
            private readonly Point p;
        }
        #endregion
    }
}
