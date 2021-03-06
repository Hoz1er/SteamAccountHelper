﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SteamAccountHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private RegistryKey steamKey;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class SteamAccountItem
        {
            public string AccountName { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistryKey userKey = Registry.CurrentUser;
                steamKey = GetRegistryKey(userKey, new string[] { "SOFTWARE", "Valve", "Steam" }, true);
                if (steamKey == null)
                {
                    MessageBox.Show("读取Steam注册表信息失败");
                    this.Close();
                    return;
                }
                string steamPath = Convert.ToString(steamKey.GetValue("SteamPath"));
                if (string.IsNullOrWhiteSpace(steamPath))
                {
                    MessageBox.Show("读取Steam路径信息失败");
                    this.Close();
                    return;
                }
                string accountConfigPath = System.IO.Path.Combine(steamPath, "config", "loginusers.vdf");
                LstAccount.Items.Clear();
                LstAccount.Items.Add(new SteamAccountItem() { AccountName = "使用其他账号登录" });
                SteamAccountItem accountItem = new SteamAccountItem();
                using (FileStream fs = new FileStream(accountConfigPath, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string curLine = sr.ReadLine().Trim();
                            if (curLine.StartsWith("\"AccountName\""))
                            {
                                accountItem = new SteamAccountItem();
                                accountItem.AccountName = curLine.Substring(13).Trim().Trim('"');
                            }
                            if (curLine.StartsWith("\"RememberPassword\""))
                            {
                                if ("1".Equals(GetValue(curLine, "\"RememberPassword\"")))
                                {
                                    LstAccount.Items.Add(accountItem);
                                }
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }

        private string GetValue(string curLine, string startStr)
        {
            string str = curLine.Substring(startStr.Length).Trim();
            return str.Substring(1, str.Length - 2);//去除包裹账号的左右双引号
        }

        private RegistryKey GetRegistryKey(RegistryKey parentKey, string[] subKeys, bool writable = false)
        {
            if (parentKey != null && subKeys.Length > 0)
            {
                string[] arrCurrentSubKeyNames = parentKey.GetSubKeyNames();
                for (int i = 0; i < arrCurrentSubKeyNames.Length; i++)
                {
                    if (string.Compare(arrCurrentSubKeyNames[i], subKeys[0], true) == 0)
                    {
                        RegistryKey childKey = parentKey.OpenSubKey(arrCurrentSubKeyNames[i], writable);
                        if (childKey == null || subKeys.Length < 2)
                        {
                            return childKey;
                        }
                        return GetRegistryKey(childKey, subKeys.Skip(1).ToArray(), writable);
                    }
                }
            }
            return null;
        }

        private void LstAccount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string account = string.Empty;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Process[] arrSteamProcess = Process.GetProcessesByName("steam");
                if (arrSteamProcess.Length > 0)
                {
                    if (chkAutoStop.IsChecked != true)
                    {
                        MessageBox.Show("steam正在运行中");
                        return;
                    }
                    else
                    {
                        try
                        {
                            foreach (Process item in arrSteamProcess)
                            {
                                item.Kill();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("尝试关闭Steam进程时出现异常：" + ex.Message);
                            return;
                        }
                        
                    }
                }

                if (sender is ListView lstView && lstView.SelectedValue is SteamAccountItem accountItem)
                {
                    if (lstView.SelectedIndex != 0)
                    {
                        account = accountItem.AccountName;
                    }
                }
                CallSteam(account);
            }

        }

        private void CallSteam(string account)
        {
            try
            {
                string steamExe = Convert.ToString(steamKey.GetValue("SteamExe"));
                steamKey.SetValue("AutoLoginUser", account);

                Process.Start(steamExe);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
