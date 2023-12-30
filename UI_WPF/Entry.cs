﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.DB;
using Another_Mirai_Native.RPC;
using Another_Mirai_Native.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Another_Mirai_Native.UI
{
    public class Entry
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                InitCore();
                App.Main();
            }
            else
            {
                Another_Mirai_Native.Entry.Main(args);
            }
        }

        private static void InitCore()
        {
            AppConfig.LoadConfig();
            AppConfig.IsCore = true;
            ServerManager.Server.OnShowErrorDialogCalled += DialogHelper.ShowErrorDialog;
            Another_Mirai_Native.Entry.CreateInitFolders();
            Another_Mirai_Native.Entry.InitExceptionCapture();
            if (AppConfig.UseDatabase && File.Exists(LogHelper.GetLogFilePath()) is false)
            {
                LogHelper.CreateDB();
            }
            ServerManager serverManager = new();
            if (serverManager.Build(AppConfig.ServerType) is false)
            {
                LogHelper.Debug("初始化", "构建服务器失败");
                return;
            }
            if (ServerManager.Server.SetConnectionConfig() is false)
            {
                LogHelper.Debug("初始化", "初始化连接参数失败，请检查配置内容");
                return;
            }
            if (!ServerManager.Server.Start())
            {
                LogHelper.Debug("初始化", "构建服务器失败");
                return;
            }
        }
    }
}