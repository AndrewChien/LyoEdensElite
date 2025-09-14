using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Launcher;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Client
{
    internal static class Program
    {
        public static CMain Form;
        public static AMain PForm;
        public static bool CanClose = true;
        public static bool Restart;

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg.ToLower() == "-tc") 
                        Settings.UseTestConfig = true;
                }
            }

            #if DEBUG
                Settings.UseTestConfig = true;
            #endif

            try
            {
                //若存在更新则退出本主程序
                if (UpdatePatcher()) 
                    return;

                if (RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully == true) { }

                //切换Packet库为客户端模式
                Packet.IsServer = false;

                //加载配置
                Settings.Load();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //更新开关控制进入更新程序还是直接进入游戏程序
                if (Settings.P_Patcher) 
                    Application.Run(PForm = new Launcher.AMain());//更新程序
                else 
                    Application.Run(Form = new CMain());//游戏程序

                //保存配置
                Settings.Save();

                //保存游戏热键
                CMain.InputKeys.Save();

                //控制本程序重启
                if (Restart)
                {
                    Application.Restart();
                }
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());
            }
        }

        /// <summary>
        /// 启动一个新的更新程序
        /// </summary>
        /// <returns></returns>
        private static bool UpdatePatcher()
        {
            try
            {
                const string fromName = @".\AutoPatcher.gz", toName = @".\AutoPatcher.exe";
                //无AutoPatcher.gz则不更新
                if (!File.Exists(fromName)) 
                    return false;

                Process[] processes = Process.GetProcessesByName("AutoPatcher");
                if (processes.Length > 0)
                {
                    //若更新程序已启动则杀更新进程，3秒内没有杀成功则报错
                    string patcherPath = Application.StartupPath + @"\AutoPatcher.exe";
                    for (int i = 0; i < processes.Length; i++)
                        if (processes[i].MainModule.FileName == patcherPath)
                            processes[i].Kill();

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    bool wait = true;
                    processes = Process.GetProcessesByName("AutoPatcher");

                    while (wait)
                    {
                        wait = false;
                        for (int i = 0; i < processes.Length; i++)
                            if (processes[i].MainModule.FileName == patcherPath)
                            {
                                wait = true;
                            }

                        if (stopwatch.ElapsedMilliseconds <= 3000) 
                            continue;
                        MessageBox.Show("Failed to close AutoPatcher during update.");
                        return true;
                    }
                }

                //删除AutoPatcher.exe并复制一个新的，启动更新程序
                if (File.Exists(toName)) 
                    File.Delete(toName);
                File.Move(fromName, toName);
                Process.Start(toName, "Auto");

                return true;
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());
                throw;
            }
        }

        public static class RuntimePolicyHelper
        {
            public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

            static RuntimePolicyHelper()
            {
                ICLRRuntimeInfo clrRuntimeInfo =
                    (ICLRRuntimeInfo)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                        Guid.Empty,
                        typeof(ICLRRuntimeInfo).GUID);
                try
                {
                    clrRuntimeInfo.BindAsLegacyV2Runtime();
                    LegacyV2RuntimeEnabledSuccessfully = true;
                }
                catch (COMException)
                {
                    // This occurs with an HRESULT meaning 
                    // "A different runtime was already bound to the legacy CLR version 2 activation policy."
                    LegacyV2RuntimeEnabledSuccessfully = false;
                }
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
            private interface ICLRRuntimeInfo
            {
                void xGetVersionString();
                void xGetRuntimeDirectory();
                void xIsLoaded();
                void xIsLoadable();
                void xLoadErrorString();
                void xLoadLibrary();
                void xGetProcAddress();
                void xGetInterface();
                void xSetDefaultStartupFlags();
                void xGetDefaultStartupFlags();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindAsLegacyV2Runtime();
            }
        }

    }
}
