using ET;
using ConsoleApp.Properties;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;
using System.Windows.Documents;
using System.Reflection;

namespace UIConsole.App
{

    public struct ItemShopBuy
    {
        public int iProductID;
        public int iProductCnt;
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MainWindow Ptr;
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            Ptr = this;
            var ThirdParty = Path.GetFullPath("./ThirdParty/");
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + ThirdParty);

            //訪問TG時HTTPS出錯 添加以下代碼正常
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Directory.CreateDirectory("Temp");
            Game.AddSingleton<MainThreadSynchronizationContext>();
            Game.AddSingleton<Logger>().ILog = new NLogger("Console", 1, Path.GetFullPath("./Config/NLog/NLog.config"));


            Game.AddSingleton<CodeTypes, Assembly[]>(new[] { typeof(ET.Game).Assembly, Assembly.GetEntryAssembly() });
            Game.AddSingleton<TimeInfo>();
            Game.AddSingleton<IdGenerater>();
            Game.AddSingleton<MainConfig>();

            var hashSet = CodeTypes.Instance.GetTypes(typeof(SingletonAttribute));
            foreach (Type type in hashSet)
            {
                object obj = Activator.CreateInstance(type);
                //((ISingletonAwake)obj).Awake();
                Game.AddSingleton((ISingleton)obj);
            }

            ContentFrame.Navigate(new ConsoleApp.Pages.AccountSettingPage());
        }


        private async void Window_Closed(object sender, EventArgs e)
        {
            Game.Close();
            await Task.Delay(1000);
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            /*
            e.Cancel = true;
            if (await MainWindow.Ptr.ShowMessageAsync("提示", $"確認要退出程式嗎?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
            {
                Closing -= MetroWindow_Closing;
                Close();
            }
            */
        }

        private int frameCount;
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            try
            {
                Game.Update();
                Game.LateUpdate();
                Game.FrameFinishUpdate();
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            // 更新帧计数
            frameCount++;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized) //最小化的时候托盘操作
            {

            }
        }

        private void MetroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ContentFrame.CanGoBack)
                {
                    ContentFrame.GoBack();
                }
            }
        }

    }
}
