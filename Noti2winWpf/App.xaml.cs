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
        protected override void OnStartup(StartupEventArgs e)
        {
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
