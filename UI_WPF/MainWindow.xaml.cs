﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.Native;
using Another_Mirai_Native.UI.Controls;
using Another_Mirai_Native.WebSocket;
using ModernWpf;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Another_Mirai_Native.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UIConfig.InitConfigs();
            Instance = this;
            switch (UIConfig.Theme)
            {
                case "Light":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    break;

                case "Dark":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;

                default:
                    break;
            }
            try
            {
                ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString(UIConfig.AccentColor);
            }
            catch
            {
                ThemeManager.Current.AccentColor = null;
            }
            Width = Math.Max(MinWidth, UIConfig.Width);
            Height = Math.Max(MinHeight, UIConfig.Height);
        }

        public static MainWindow Instance { get; set; }

        private Dictionary<string, object> PageCache { get; set; } = new();

        private DispatcherTimer ResizeTimer { get; set; }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                string selectedItemTag = (string)selectedItem.Tag;
                if (PageCache.ContainsKey(selectedItemTag))
                {
                    MainFrame.Navigate(PageCache[selectedItemTag]);
                }
                else
                {
                    Type pageType = typeof(MainWindow).Assembly.GetType("Another_Mirai_Native.UI.Pages." + selectedItemTag);
                    var obj = Activator.CreateInstance(pageType);
                    PageCache.Add(selectedItemTag, obj);
                    MainFrame.Navigate(obj);
                }
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme =
                         ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark
                         ? ApplicationTheme.Light
                         : ApplicationTheme.Dark;
            UIConfig.Theme = ThemeManager.Current.ActualApplicationTheme.ToString();
            ConfigHelper.SetConfig("Theme", UIConfig.Theme, UIConfig.DefaultConfigPath);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            ResizeTimer.Tick += ResizeTimer_Tick;
            InitCore();
            _ = new ProtocolManager();
            ProtocolSelectorDialog dialog = new();
            await dialog.ShowAsync();
            if (dialog.DialogResult == ContentDialogResult.Secondary)
            {
                Environment.Exit(0);
            }
            LoadPlugins();
        }

        private void ResizeTimer_Tick(object? sender, EventArgs e)
        {
            ResizeTimer.Stop();
            ConfigHelper.SetConfig("Window_Width", Width, UIConfig.DefaultConfigPath);
            ConfigHelper.SetConfig("Window_Height", Height, UIConfig.DefaultConfigPath);
        }

        private void LoadPlugins()
        {
            Task.Run(() =>
            {
                var manager = new PluginManagerProxy();
                manager.LoadPlugins();
                Thread.Sleep(500);
                EnablePluginByConfig();
            });
        }

        public void EnablePluginByConfig()
        {
            foreach (var item in PluginManagerProxy.PluginProcess)
            {
                if (!PluginManagerProxy.Instance.WaitAppInfo(item.Key))
                {
                    return;
                }
                string appId = item.Value.AppId;
                if (UIConfig.AutoEnablePlugins.Any(x => x == appId))
                {
                    var proxy = PluginManagerProxy.Proxies.FirstOrDefault(x => x.AppInfo.AppId == appId);
                    if (proxy == null)
                    {
                        return;
                    }
                    PluginManagerProxy.Instance.SetPluginEnabled(proxy, true);
                }
            }
        }

        private void InitCore()
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            Server.OnShowErrorDialogCalled += DialogHelper.ShowErrorDialog;
            Another_Mirai_Native.Entry.CreateInitFolders();
            Another_Mirai_Native.Entry.InitExceptionCapture();
            AppConfig.LoadConfig();
            AppConfig.IsCore = true;
            new Another_Mirai_Native.WebSocket.Server().Start();
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            DialogHelper.ShowErrorDialog($"UI异常: {e.Exception.Message}", e.Exception.StackTrace);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ResizeTimer == null)
            {
                return;
            }
            ResizeTimer.Stop();
            ResizeTimer.Start();
        }

        private async void DisconnectProtocol_Click(object sender, RoutedEventArgs e)
        {
            if (await DialogHelper.ShowConfirmDialog("协议切换", "确定要切换协议吗，操作会导致与机器人断开连接，无法处理消息"))
            {
                AppConfig.AutoConnect = false;
                ConfigHelper.SetConfig("AutoConnect", false);
                bool success = ProtocolManager.Instance.CurrentProtocol.Disconnect();
                if (success)
                {
                    ProtocolSelectorDialog dialog = new();
                    await dialog.ShowAsync();
                    if (dialog.DialogResult == ContentDialogResult.Secondary)
                    {
                        Environment.Exit(0);
                    }
                }
                else
                {
                    DialogHelper.ShowSimpleDialog("切换失败力", "协议断开连接失败，建议在设置中更改自己需要的协议并重启框架");
                }
            }
        }
    }
}