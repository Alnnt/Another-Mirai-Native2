﻿using Another_Mirai_Native.Config;
using System;
using System.Collections.Generic;
using System.Linq;
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
                AppConfig.LoadConfig();
                AppConfig.IsCore = true;
                App.Main();
            }
            else
            {
                Another_Mirai_Native.Entry.Main(args);
            }
        }
    }
}