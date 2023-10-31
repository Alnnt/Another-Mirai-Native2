﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.DB;
using Another_Mirai_Native.Model;
using Another_Mirai_Native.Model.Enums;
using System.Diagnostics;

namespace Another_Mirai_Native.Native
{
    public class PluginManagerProxy
    {
        public PluginManagerProxy()
        {
            Instance = this;
            StartPluginMonitor();
        }

        public static PluginManagerProxy Instance { get; private set; }

        public static Dictionary<int, AppInfo> PluginProcess { get; private set; } = new();

        public static List<CQPluginProxy> Proxies { get; private set; } = new();

        public static event Action<CQPluginProxy> OnPluginProxyAdded;

        public static event Action<CQPluginProxy> OnPluginProxyConnectStatusChanged;

        public static event Action<CQPluginProxy> OnPluginEnableChanged;

        private static int PID => Process.GetCurrentProcess().Id;

        public static void SetProxyConnected(Guid id)
        {
            if (Proxies.Any(x => x.ConnectionID == id))
            {
                var proxy = Proxies.First(x => x.ConnectionID == id);
                proxy.HasConnection = true;
                OnPluginProxyConnectStatusChanged?.Invoke(proxy);
            }
        }

        public static void SetProxyDisconnected(Guid id)
        {
            if (Proxies.Any(x => x.ConnectionID == id))
            {
                var proxy = Proxies.First(x => x.ConnectionID == id);
                proxy.HasConnection = false;
                OnPluginProxyConnectStatusChanged?.Invoke(proxy);
            }
        }

        public static void AddProxy(CQPluginProxy proxy)
        {
            if (!Proxies.Any(x => x.ConnectionID == proxy.ConnectionID))
            {
                Proxies.Add(proxy);
                OnPluginProxyAdded?.Invoke(proxy);
            }
        }

        public static CQPluginProxy GetProxyByAuthCode(int authCode)
        {
            return Proxies.FirstOrDefault(x => x.AppInfo.AuthCode == authCode);
        }

        public bool LoadPlugins()
        {
            foreach (var item in Directory.GetFiles(@"data\plugins", "*.dll"))
            {
                Process? pluginProcess = StartPluginProcess(item);
                if (pluginProcess != null)
                {
                    PluginProcess.Add(pluginProcess.Id, new AppInfo { PluginPath = item });
                }
            }
            return true;
        }

        public bool WaitAppInfo(int pid)
        {
            bool result = false;
            for (int i = 0; i < AppConfig.LoadTimeout / 100; i++)
            {
                if (!PluginProcess.ContainsKey(pid))
                {
                    result = false;
                    break;
                }
                if (!string.IsNullOrEmpty(PluginProcess[pid].AppId))
                {
                    result = true;
                    break;
                }
                Thread.Sleep(100);
            }
            return result;
        }

        public InvokeResult Invoke(CQPluginProxy target, string function, params object[] args)
        {
            string guid = Guid.NewGuid().ToString();
            var r = target.Invoke(new InvokeBody { GUID = guid, Function = function, Args = args });
            if (!r.Success)
            {
                LogHelper.Error("调用失败", $"调用方法: {function}, 错误信息: {r.Message}");
            }
            return r;
        }

        public int InvokeEvent(CQPluginProxy target, PluginEventType eventType, params object[] args)
        {
            if (eventType == PluginEventType.Menu || target.AppInfo._event.Any(x => x.id == (int)eventType))
            {
                var r = Invoke(target, $"InvokeEvent_{eventType}", args);
                return !r.Success ? -1 : Convert.ToInt32(r.Result);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 向所有 Enabled 的插件按优先级发送事件，当某个插件返回 1 时阻塞后续调用
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="args">参数</param>
        /// <returns>阻塞的插件</returns>
        public CQPluginProxy InvokeEvent(PluginEventType eventType, params object[] args)
        {
            foreach (var item in Proxies.Where(x => x.Enabled && x.AppInfo._event.Any(o => o.id == (int)eventType))
                .OrderByDescending(x => x.AppInfo._event.First(o => o.id == (int)eventType).priority))
            {
                int ret = InvokeEvent(item, eventType, args);
                if (ret == 1)
                {
                    return item;
                }
            }
            return null;
        }

        #region 测试事件调用

        public int Event_OnPrivateMsg(CQPluginProxy target, int subType, int msgId, long fromQQ, string msg, int font)
        {
            return InvokeEvent(target, PluginEventType.PrivateMsg, subType, msgId, fromQQ, msg, font);
        }

        public int Event_OnGroupMsg(CQPluginProxy target, int subType, int msgId, long fromGroup, long fromQQ, string fromAnonymous, string msg, int font)
        {
            return InvokeEvent(target, PluginEventType.GroupMsg, subType, msgId, fromGroup, fromQQ, fromAnonymous, msg, font);
        }

        public int Event_OnDiscussMsg(CQPluginProxy target, int subType, int msgId, long fromNative, long fromQQ, string msg, int font)
        {
            return InvokeEvent(target, PluginEventType.DiscussMsg, subType, msgId, fromNative, fromQQ, msg, font);
        }

        public int Event_OnUpload(CQPluginProxy target, int subType, int sendTime, long fromGroup, long fromQQ, string file)
        {
            return InvokeEvent(target, PluginEventType.Upload, subType, sendTime, fromGroup, fromQQ, file);
        }

        public int Event_OnAdminChange(CQPluginProxy target, int subType, int sendTime, long fromGroup, long beingOperateQQ)
        {
            return InvokeEvent(target, PluginEventType.AdminChange, subType, sendTime, fromGroup, beingOperateQQ);
        }

        public int Event_OnGroupMemberDecrease(CQPluginProxy target, int subType, int sendTime, long fromGroup, long fromQQ, long beingOperateQQ)
        {
            return InvokeEvent(target, PluginEventType.GroupMemberDecrease, subType, sendTime, fromGroup, fromQQ, beingOperateQQ);
        }

        public int Event_OnGroupMemberIncrease(CQPluginProxy target, int subType, int sendTime, long fromGroup, long fromQQ, long beingOperateQQ)
        {
            return InvokeEvent(target, PluginEventType.GroupMemberIncrease, subType, sendTime, fromGroup, fromQQ, beingOperateQQ);
        }

        public int Event_OnGroupBan(CQPluginProxy target, int subType, int sendTime, long fromGroup, long fromQQ, long beingOperateQQ, long duration)
        {
            return InvokeEvent(target, PluginEventType.GroupBan, subType, sendTime, fromGroup, fromQQ, beingOperateQQ, duration);
        }

        public int Event_OnFriendAdded(CQPluginProxy target, int subType, int sendTime, long fromQQ)
        {
            return InvokeEvent(target, PluginEventType.FriendAdded, subType, sendTime, fromQQ);
        }

        public int Event_OnFriendAddRequest(CQPluginProxy target, int subType, int sendTime, long fromQQ, string msg, string responseFlag)
        {
            return InvokeEvent(target, PluginEventType.FriendRequest, subType, sendTime, fromQQ, msg, responseFlag);
        }

        public int Event_OnGroupAddRequest(CQPluginProxy target, int subType, int sendTime, long fromGroup, long fromQQ, string msg, string responseFlag)
        {
            return InvokeEvent(target, PluginEventType.GroupAddRequest, subType, sendTime, fromGroup, fromQQ, msg, responseFlag);
        }

        public int Event_OnStartUp(CQPluginProxy target)
        {
            return InvokeEvent(target, PluginEventType.StartUp);
        }

        public int Event_OnExit(CQPluginProxy target)
        {
            return InvokeEvent(target, PluginEventType.Exit);
        }

        public int Event_OnEnable(CQPluginProxy target)
        {
            return InvokeEvent(target, PluginEventType.Enable);
        }

        public int Event_OnDisable(CQPluginProxy target)
        {
            return InvokeEvent(target, PluginEventType.Disable);
        }

        #endregion 测试事件调用

        public Process? StartPluginProcess(string item)
        {
            string arguments = $"-PID {PID} -AutoExit {AppConfig.PluginExitWhenCoreExit} -Path {item} -WS {AppConfig.WebSocketURL}";
            Process? pluginProcess = null;
            var startConfig = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\{AppDomain.CurrentDomain.FriendlyName}",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (!AppConfig.DebugMode)
            {
                startConfig.UseShellExecute = false;
                startConfig.CreateNoWindow = true;
                startConfig.RedirectStandardOutput = true;
                pluginProcess = Process.Start(startConfig);
                Task.Run(() =>
                {
                    while (pluginProcess?.StandardOutput != null && !pluginProcess.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(pluginProcess.StandardOutput.ReadLine());
                    }
                });
            }
            else
            {
                pluginProcess = Process.Start(startConfig);
            }
            return pluginProcess;
        }

        public void ReloadAllPlugins()
        {
            foreach (var item in Proxies)
            {
                ReloadPlugin(item);
            }
        }

        public void ReloadPlugin(CQPluginProxy plugin)
        {
            InvokeEvent(plugin, PluginEventType.Disable);
            plugin.KillProcess();
            if (!AppConfig.RestartPluginIfDead)
            {
                LogHelper.Info("重启插件", $"{plugin.PluginName} 重启");
                Process? pluginProcess = StartPluginProcess(plugin.AppInfo.PluginPath);
                if (pluginProcess != null)
                {
                    PluginProcess.Add(pluginProcess.Id, new AppInfo { PluginPath = plugin.AppInfo.PluginPath });
                    WaitAppInfo(pluginProcess.Id);
                }
            }
            else
            {
                for (int i = 0; i < AppConfig.LoadTimeout / 100; i++)
                {
                    if (PluginProcess.Any(x => x.Value.AppId == plugin.PluginId))
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        public bool SetPluginEnabled(CQPluginProxy plugin, bool enabled)
        {
            if (plugin == null || plugin.HasConnection == false)
            {
                return false;
            }
            bool success = false;
            if (enabled)
            {
                if (plugin.Enabled)
                {
                    return true;
                }
                success = InvokeEvent(plugin, PluginEventType.Enable) == 0;
                success = success && (InvokeEvent(plugin, PluginEventType.StartUp) == 0);
            }
            else
            {
                if (!plugin.Enabled)
                {
                    return true;
                }
                success = InvokeEvent(plugin, PluginEventType.Disable) == 0;
                success = success && (InvokeEvent(plugin, PluginEventType.Exit) == 0);
            }
            string logMessage = $"插件 {plugin.PluginName} {{0}}";
            if (success)
            {
                plugin.Enabled = enabled;
                OnPluginEnableChanged?.Invoke(plugin);
                logMessage = string.Format(logMessage, enabled ? "启用成功" : "停用成功");
            }
            else
            {
                logMessage = string.Format(logMessage, enabled ? "启用失败" : "停用失败");
            }
            LogHelper.WriteLog(LogLevel.InfoSuccess, "改变插件状态", logMessage);
            return success;
        }

        private void StartPluginMonitor()
        {
            try
            {
                var monitor = Task.Run(() =>
                {
                    while (true)
                    {
                        for (int i = 0; i < PluginProcess.Count; i++)
                        {
                            var plugin = PluginProcess.ElementAt(i);
                            try
                            {
                                _ = Process.GetProcessById(plugin.Key);
                            }
                            catch
                            {
                                LogHelper.Error("插件进程监控", $"[{plugin.Key}]{plugin.Value.name} 进程不存在");
                                PluginProcess.Remove(plugin.Key);
                                if (AppConfig.RestartPluginIfDead)
                                {
                                    LogHelper.Info("插件进程监控", $"{plugin.Value.name} 重启");
                                    Process? pluginProcess = StartPluginProcess(plugin.Value.PluginPath);
                                    if (pluginProcess != null)
                                    {
                                        PluginProcess.Add(pluginProcess.Id, plugin.Value);
                                    }
                                }
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
            }
            catch (AggregateException ex)
            {
                foreach (var item in ex.InnerExceptions)
                {
                    LogHelper.Error("插件进程监控", ex);
                }
            }
        }
    }
}