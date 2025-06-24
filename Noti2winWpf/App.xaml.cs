using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Foundation.Collections;

namespace Noti2winWpf
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static string WeChatPathStr { get; set; } = string.Empty;
        public static string QQPathStr { get; set; } = string.Empty;
        public static string DingTalkPathStr { get; set; } = string.Empty;
        protected override void OnStartup(StartupEventArgs e)
        {

            string procName = Process.GetCurrentProcess().ProcessName;
            var running = Process.GetProcessesByName(procName);
            Console.WriteLine("running length:" + running.Length );
            if (running.Length > 1)
            {
                // 已有实例在运行
                Utils.OrdinaryNoti(null, "程序已在运行中，请勿重复打开！");
                Application.Current.Shutdown();
                return;
            }
           
            string iconTempDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconTemp");
            try
            {
                if (System.IO.Directory.Exists(iconTempDir))
                {
                    System.IO.Directory.Delete(iconTempDir, true);
                }
                System.IO.Directory.CreateDirectory(iconTempDir); 
            }
            catch (Exception ex)
            {
                Utils.WriteLog("初始化 IconTemp 文件夹失败: " + ex.Message, Utils.LogErr);
            }
            base.OnStartup(e);
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {

                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Need to dispatch to UI thread if performing UI operations
                Application.Current.Dispatcher.Invoke(delegate
                {
                    // TODO: Show the corresponding content
                    
                        switch (args["conversation"])
                        {
                            case "wechat":
                                ScheduleOpenExe(@WeChatPathStr, 100);
                                break;
                            case "qq":
                                ScheduleOpenExe(@QQPathStr, 100);
                                break;
                            case "dingtalk":
                            ScheduleOpenExe(@DingTalkPathStr, 100);
                            break;
                        default:
                                break;
                        }
                 
                     
                });
            };
        }

        private void ScheduleOpenExe(string exePath, int delayMilliseconds)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(delayMilliseconds);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                OpenExe(exePath);
            };
            timer.Start();
        }


        private void OpenExe(string exePath)
        {
            try
            {
                Debug.WriteLine($"Attempting to start process: {exePath}");
                Process.Start(exePath);
                Debug.WriteLine($"Process started: {exePath}");
            }
            catch (Exception ex)
            {
                Utils.WriteLog($"Failed to open {exePath}: {ex.Message}",Utils.LogErr);
                Console.WriteLine($"Failed to open {exePath}: {ex.Message}");
            }
        }
    }

}
