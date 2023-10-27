﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.Native;
using Another_Mirai_Native.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Another_Mirai_Native.UI.Pages
{
    /// <summary>
    /// PluginPage.xaml 的交互逻辑
    /// </summary>
    public partial class PluginPage : Page
    {
        public PluginPage()
        {
            InitializeComponent();
        }

        public ObservableCollection<CQPluginProxyWrapper> CQPlugins { get; set; } = new();

        private Dictionary<int, string> AuthChineseName { get; set; } = new() {
            {20, "[敏感]取Cookies"},
            {30, "接收语音"},
            {101, "发送群消息"},
            {103, "发送讨论组消息"},
            {106, "发送私聊消息"},
            {110, "[敏感]发送赞"},
            {120, "置群员移除"},
            {121, "置群员禁言"},
            {122, "置群管理员"},
            {123, "置全群禁言"},
            {124, "置匿名群员禁言"},
            {125, "置群匿名设置"},
            {126, "置群成员名片"},
            {127, "[敏感]置群退出"},
            {128, "置群成员专属头衔"},
            {130, "取群成员信息"},
            {131, "取陌生人信息"},
            {132, "取群信息"},
            {140, "置讨论组退出"},
            {150, "置好友添加请求"},
            {151, "置群添加请求"},
            {160, "取群成员列表"},
            {161, "取群列表"},
            {162, "取好友列表"},
            {180, "撤回消息"},
        };

        private CQPluginProxy SelectedPlugin { get; set; }

        private bool ReloadAllRunningStatus { set => Dispatcher.Invoke(() => ReloadAllStatus.IsActive = value); }

        private bool ReloadRunningStatus { set => Dispatcher.Invoke(() => ReloadStatus.IsActive = value); }

        private bool ToggleEnableRunningStatus { set => Dispatcher.Invoke(() => EnableStatus.IsActive = value); }

        private bool FormLoaded { get; set; }

        private void PluginListContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPlugin = (PluginListContainer.SelectedItem as CQPluginProxyWrapper)?.TargetPlugin;
            UpdateAuthList();
        }

        private void UpdateAuthList()
        {
            if (PluginListContainer.SelectedItem == null)
            {
                return;
            }
            AuthDisplay.Items.Clear();
            foreach (var item in ((CQPluginProxyWrapper)PluginListContainer.SelectedItem).TargetPlugin.AppInfo.auth.OrderBy(x => x))
            {
                if (AuthChineseName.ContainsKey(item))
                {
                    AuthDisplay.Items.Add(AuthChineseName[item]);
                }
            }
        }

        private async void ReloadAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await DialogHelper.ShowConfirmDialog("重载插件", "确定要重载所有插件吗？这可能会需要一些时间"))
            {
                return;
            }
            ReloadAllRunningStatus = true;
            await Task.Run(() =>
            {
                PluginManagerProxy.Instance.ReloadAllPlugins();
                MainWindow.Instance.EnablePluginByConfig();
                ReloadAllRunningStatus = false;
            });
        }

        private void OpenPluginPathBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(Environment.CurrentDirectory, "data", "plugins"));
        }

        private void OpenAllDataBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(Environment.CurrentDirectory, "data", "app"));
        }

        private void ToggleEnableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlugin == null)
            {
                return;
            }
            if (SelectedPlugin.HasConnection is false)
            {
                DialogHelper.ShowSimpleDialog("嗯哼", "插件进程不存在或失去连接，尝试点击重载");
                return;
            }
            ToggleEnableRunningStatus = true;
            Task.Run(() =>
            {
                PluginManagerProxy.Instance.SetPluginEnabled(SelectedPlugin, !SelectedPlugin.Enabled);
                ToggleEnableRunningStatus = false;
                if (SelectedPlugin.Enabled)
                {
                    UIConfig.AutoEnablePlugins.Add(SelectedPlugin.PluginId);
                }
                else
                {
                    UIConfig.AutoEnablePlugins.Remove(SelectedPlugin.PluginId);
                }
                ConfigHelper.SetConfig("AutoEnablePlugins", UIConfig.AutoEnablePlugins, UIConfig.DefaultConfigPath);
            });
        }

        private void OpenMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: ContextMenu
        }

        private void OpenDataBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlugin == null)
            {
                return;
            }
            string path = Path.Combine(Environment.CurrentDirectory, "data", "app", SelectedPlugin.PluginId);
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
            else
            {
                DialogHelper.ShowSimpleDialog("嗯哼", "插件数据目录不存在");
            }
        }

        private async void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlugin == null)
            {
                return;
            }
            if (!await DialogHelper.ShowConfirmDialog("重载插件", $"确定要重载 {SelectedPlugin.PluginName} 吗？"))
            {
                return;
            }
            ReloadRunningStatus = true;
            await Task.Run(() =>
            {
                bool enable = SelectedPlugin.Enabled;
                string id = SelectedPlugin.PluginId;
                PluginManagerProxy.Instance.ReloadPlugin(SelectedPlugin);
                if (enable)
                {
                    PluginManagerProxy.Instance.SetPluginEnabled(PluginManagerProxy.Proxies.FirstOrDefault(x => x.PluginId == id), true);
                }
                ReloadRunningStatus = false;
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (FormLoaded)
            {
                return;
            }
            ReloadAllRunningStatus = false;
            ReloadRunningStatus = false;
            ToggleEnableRunningStatus = false;
            PluginManagerProxy.OnPluginEnableChanged -= PluginManagerProxy_OnPluginEnableChanged;
            PluginManagerProxy.OnPluginProxyAdded -= PluginManagerProxy_OnPluginProxyAdded;
            PluginManagerProxy.OnPluginProxyConnectStatusChanged -= PluginManagerProxy_OnPluginProxyConnectStatusChanged;
            PluginManagerProxy.OnPluginEnableChanged += PluginManagerProxy_OnPluginEnableChanged;
            PluginManagerProxy.OnPluginProxyAdded += PluginManagerProxy_OnPluginProxyAdded;
            PluginManagerProxy.OnPluginProxyConnectStatusChanged += PluginManagerProxy_OnPluginProxyConnectStatusChanged;

            DataContext = this;
            CQPlugins.Clear();
            foreach (var item in PluginManagerProxy.Proxies)
            {
                CQPlugins.Add(new CQPluginProxyWrapper(item));
            }
            FormLoaded = true;
        }

        private void PluginManagerProxy_OnPluginProxyConnectStatusChanged(CQPluginProxy plugin)
        {
            var target = CQPlugins.FirstOrDefault(x => x.TargetPlugin == plugin);
            target?.InvokePropertyChanged(nameof(target.TargetPlugin.HasConnection));
        }

        private void PluginManagerProxy_OnPluginProxyAdded(CQPluginProxy plugin)
        {
            Dispatcher.Invoke(() => CQPlugins.Add(new CQPluginProxyWrapper(plugin)));
        }

        private void PluginManagerProxy_OnPluginEnableChanged(CQPluginProxy plugin)
        {
            var target = CQPlugins.FirstOrDefault(x => x.TargetPlugin == plugin);
            target?.InvokePropertyChanged(nameof(target.TargetPlugin.Enabled));
        }
    }
}