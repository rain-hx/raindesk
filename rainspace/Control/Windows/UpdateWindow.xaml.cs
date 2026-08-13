using rainspace.Constant;
using rainspace.Interface;
using rainspace.Util;
using rainspace.ViewModel;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using rainspace.Services;
using rainspace.Services.Interfaces;

namespace rainspace.Control.Windows
{
    /// <summary>
    /// UpdateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateWindow : Window, IWindowCommon
    {
        private readonly IAppDataProvider _dataProvider = AppServices.Get<IAppDataProvider>();
        private AppConfig appConfig => _dataProvider.Current.AppConfig;
        private static string githubUrl = "";
        private static string giteeUrl = "";
        private UpdateWindow(JObject jo)
        {
            try
            {
                this.Topmost = true;
                InitializeComponent();
                rainspace.Services.ThemeService.RegisterWindow(this);
                // WinUI 3: Center on screen via AppWindow APIs
                WindowDragHelper.CenterOnScreen(this);
                WindowDragHelper.DisableResize(this);
                WindowDragHelper.HideFromTaskbar(this);
                DataHandle(jo);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "初始化更新窗口失败");
            }

        }

        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragMove(object sender, PointerRoutedEventArgs e)
        {
            WindowDragHelper.DragMove(this);
        }


        private void DataHandle(JObject jo)
        {
            TitleText.Text = StringUtil.IsEmpty(jo["title"]) ? "" : jo["title"].ToString();
            SubTitle.Text = StringUtil.IsEmpty(jo["subTitle"]) ? "" : jo["subTitle"].ToString();
            MsgTitle.Text = StringUtil.IsEmpty(jo["msgTitle"]) ? "" : jo["msgTitle"].ToString();
            JArray ja = JArray.Parse(StringUtil.IsEmpty(jo["msg"]) ? "[]" : jo["msg"].ToString());
            githubUrl = StringUtil.IsEmpty(jo["githubUrl"]) ? ConfigurationManager.AppSettings["GitHubUrl"] : jo["githubUrl"].ToString();
            giteeUrl = StringUtil.IsEmpty(jo["giteeUrl"]) ? ConfigurationManager.AppSettings["GiteeUrl"] : jo["giteeUrl"].ToString();
            string msg = "";
            for (int i = 0; i < ja.Count; i++)
            {
                msg += "• " + ja[i].ToString() + "\n";
            }
            Msg.Text = msg;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string packageUrl;
            switch (appConfig.UpdateType)
            {
                case UpdateType.GitHub:
                    packageUrl = githubUrl;
                    break;
                default:
                    packageUrl = giteeUrl;
                    break;
            }
            Process.Start(new ProcessStartInfo(packageUrl)
            {
                UseShellExecute = true
            });
            this.Close();
        }

        private static Window window = null;
        public static void Show(JObject jo)
        {
            if (window != null)
            {
                window.Close();
            }
            window = new UpdateWindow(jo);
            window.Activate();
        }

        public void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Escape)
            {
                this.DataContext = null;
                this.Close();
            }
        }

        private void StarBtnClick(object sender, RoutedEventArgs e)
        {

            string githubUrl = ConfigurationManager.AppSettings["GitHubUrl"];
            string giteeUrl = ConfigurationManager.AppSettings["GiteeUrl"];

            if(!ReqGitUrl(githubUrl))
            {
                if (!ReqGitUrl(giteeUrl))
                {
                    OpenLinkUrl(githubUrl);
                } else
                {
                    OpenLinkUrl(giteeUrl);
                }
            } else
            {
                OpenLinkUrl(githubUrl);
            }
        }


        private bool ReqGitUrl(string url)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#pragma warning disable SYSLIB0014 // WebRequest is obsolete
                WebRequest myRequest = WebRequest.Create(url);
#pragma warning restore SYSLIB0014
                myRequest.ContentType = "text/plain; charset=utf-8";
                using var myResponse = (HttpWebResponse)myRequest.GetResponse();
                return myResponse.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        private void OpenLinkUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

    }
}
