﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.Model;
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
            Instance = this;
            ThemeManager.Current.ApplicationTheme = ConfigHelper.GetConfig("Theme", DefaultConfigPath, "Light") == "Light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            try
            {
                ThemeManager.Current.AccentColor = (Color)ColorConverter.ConvertFromString(ConfigHelper.GetConfig("AccentColor", DefaultConfigPath, ""));
            }
            catch
            {
                ThemeManager.Current.AccentColor = null;
            }
        }

        public static MainWindow Instance { get; set; }

        public static string DefaultConfigPath { get; set; } = @"conf/UIConfig.json";

        private Dictionary<string, object> PageCache { get; set; } = new();

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
            ConfigHelper.SetConfig("Theme", ThemeManager.Current.ActualApplicationTheme.ToString(), DefaultConfigPath);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
            List<string> enablePlugins = ConfigHelper.GetConfig("EnablePlugins", DefaultConfigPath, new List<string>());
            foreach (var item in PluginManagerProxy.PluginProcess)
            {
                if (!PluginManagerProxy.Instance.WaitAppInfo(item.Key))
                {
                    return;
                }
                string appId = item.Value.AppId;
                if (enablePlugins.Any(x => x == appId))
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
            Server.OnShowErrorDialogCalled += ErrorDialogHelper.ShowErrorDialog;
            Another_Mirai_Native.Entry.CreateInitFolders();
            Another_Mirai_Native.Entry.InitExceptionCapture();
            AppConfig.LoadConfig();
            AppConfig.IsCore = true;
            new Another_Mirai_Native.WebSocket.Server().Start();
        }
    }
}