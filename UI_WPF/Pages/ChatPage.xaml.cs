﻿using Another_Mirai_Native.Config;
using Another_Mirai_Native.Model;
using Another_Mirai_Native.Model.Enums;
using Another_Mirai_Native.Native;
using Another_Mirai_Native.UI.Controls;
using Another_Mirai_Native.UI.Converters;
using Another_Mirai_Native.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Another_Mirai_Native.UI.Pages
{
    /// <summary>
    /// ChatPage.xaml 的交互逻辑
    /// </summary>
    public partial class ChatPage : Page, INotifyPropertyChanged
    {
        public ChatPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ChatListItemViewModel> ChatList { get; set; } = new();

        public ObservableCollection<ChatDetailItemViewModel> DetailList { get; set; } = new();

        public string GroupName { get; set; } = "BBB";

        private bool FormLoaded { get; set; }

        private Dictionary<long, ObservableCollection<ChatDetailItemViewModel>> FriendChatHistory { get; set; } = new();

        private Dictionary<long, FriendInfo> FriendInfoCache { get; set; } = new();

        private Dictionary<long, ObservableCollection<ChatDetailItemViewModel>> GroupChatHistory { get; set; } = new();

        private Dictionary<long, GroupInfo> GroupInfoCache { get; set; } = new();

        private Dictionary<long, Dictionary<long, GroupMemberInfo>> GroupMemberCache { get; set; } = new();

        private ChatListItemViewModel SelectedItem => (ChatListItemViewModel)ChatListDisplay.SelectedItem;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void AddGroupChatItem(long group, long qq, string msg, DetailItemType itemType)
        {
            if (GroupChatHistory.TryGetValue(group, out var chatHistory))
            {
                if (chatHistory.Count > AppConfig.Instance.MessageCacheSize)
                {
                    chatHistory.RemoveAt(0);
                }
                chatHistory.Add(BuildChatDetailItem(group, msg, GetGroupMemberNick(group, qq), ChatAvatar.AvatarTypes.QQGroup, itemType));
            }
            else
            {
                GroupChatHistory.Add(group, new ObservableCollection<ChatDetailItemViewModel>());
                GroupChatHistory[group].Add(BuildChatDetailItem(group, msg, GetGroupMemberNick(group, qq), ChatAvatar.AvatarTypes.QQGroup, itemType));
            }
            OnPropertyChanged(nameof(DetailList));
        }

        private void AddPrivateChatItem(long qq, string msg, DetailItemType itemType)
        {
            if (FriendChatHistory.TryGetValue(qq, out var chatHistory))
            {
                if (chatHistory.Count > AppConfig.Instance.MessageCacheSize)
                {
                    chatHistory.RemoveAt(0);
                }
                chatHistory.Add(BuildChatDetailItem(qq, msg, GetFriendNick(qq), ChatAvatar.AvatarTypes.QQPrivate, itemType));
            }
            else
            {
                FriendChatHistory.Add(qq, new ObservableCollection<ChatDetailItemViewModel>());
                FriendChatHistory[qq].Add(BuildChatDetailItem(qq, msg, GetFriendNick(qq), ChatAvatar.AvatarTypes.QQPrivate, itemType));
            }
            OnPropertyChanged(nameof(DetailList));
        }

        private ChatDetailItemViewModel BuildChatDetailItem(long qq, string msg, string nick, Controls.ChatAvatar.AvatarTypes avatarType, DetailItemType itemType)
        {
            return new ChatDetailItemViewModel
            {
                AvatarType = avatarType,
                Content = msg,
                DetailItemType = itemType,
                Id = qq,
                Nick = nick,
                Time = DateTime.Now,
            };
        }

        private string GetFriendNick(long qq)
        {
            try
            {
                if (FriendInfoCache.TryGetValue(qq, out var info))
                {
                    return info.Nick;
                }
                else
                {
                    var ls = ProtocolManager.Instance.CurrentProtocol.GetRawFriendList(false);
                    string r = "未定义昵称";
                    foreach (var item in ls)
                    {
                        if (FriendInfoCache.ContainsKey(item.QQ))
                        {
                            FriendInfoCache[item.QQ] = item;
                        }
                        else
                        {
                            FriendInfoCache.Add(item.QQ, item);
                        }
                        if (item.QQ == qq)
                        {
                            r = item.Nick;
                        }
                    }
                    return r;
                }
            }
            catch
            {
                return qq.ToString();
            }
        }

        private string GetGroupMemberNick(long group, long qq)
        {
            try
            {
                if (GroupMemberCache.TryGetValue(group, out var dict) && dict.TryGetValue(qq, out var info))
                {
                    return info.Nick;
                }
                else
                {
                    if (GroupMemberCache.ContainsKey(group) is false)
                    {
                        GroupMemberCache.Add(group, new Dictionary<long, GroupMemberInfo>());
                    }
                    if (GroupMemberCache[group].ContainsKey(qq) is false)
                    {
                        var memberInfo = ProtocolManager.Instance.CurrentProtocol.GetRawGroupMemberInfo(group, qq, false);
                        GroupMemberCache[group].Add(qq, memberInfo);
                    }
                    return GroupMemberCache[group][qq].Nick;
                }
            }
            catch
            {
                return qq.ToString();
            }
        }

        private string GetGroupName(long groupId)
        {
            try
            {
                if (GroupInfoCache.TryGetValue(groupId, out var info))
                {
                    return info.Name;
                }
                else
                {
                    GroupInfoCache.Add(groupId, ProtocolManager.Instance.CurrentProtocol.GetRawGroupInfo(groupId, false));
                    return GroupInfoCache[groupId].Name;
                }
            }
            catch
            {
                return groupId.ToString();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (FormLoaded)
            {
                return;
            }
            FormLoaded = true;
            if (UIConfig.Instance.ChatEnabled is false)
            {
                return;
            }
            //var converter = new ColorOpacityConverter();
            //var brush = (Brush)FindResource("SystemControlBackgroundChromeMediumBrush");
            //var convertedBrush = (Brush)converter.Convert(brush, typeof(Brush), 0.4, null);

            //ChatContainer.Background = convertedBrush; 

            PluginManagerProxy.OnGroupBan += PluginManagerProxy_OnGroupBan;
            PluginManagerProxy.OnGroupAdded += PluginManagerProxy_OnGroupAdded;
            PluginManagerProxy.OnGroupMsg += PluginManagerProxy_OnGroupMsg;
            PluginManagerProxy.OnGroupLeft += PluginManagerProxy_OnGroupLeft;
            PluginManagerProxy.OnAdminChanged += PluginManagerProxy_OnAdminChanged;
            PluginManagerProxy.OnFriendAdded += PluginManagerProxy_OnFriendAdded;
            PluginManagerProxy.OnPrivateMsg += PluginManagerProxy_OnPrivateMsg;
        }

        private void PluginManagerProxy_OnAdminChanged(long group, long qq, QQGroupMemberType type)
        {
            if (GroupMemberCache.TryGetValue(group, out var dict) && dict.TryGetValue(qq, out var info))
            {
                if (info.MemberType != QQGroupMemberType.Creator)
                {
                    info.MemberType = type;
                }
                else
                {
                    // 群主 未定义操作
                }
            }
        }

        private void PluginManagerProxy_OnFriendAdded(long qq)
        {
            GetFriendNick(qq);
            // 额外实现
        }

        private void PluginManagerProxy_OnGroupAdded(long group, long qq)
        {
            if (GroupMemberCache.TryGetValue(group, out var dict) && dict.ContainsKey(qq))
            {
                AddGroupChatItem(group, qq, $"{GetGroupMemberNick(group, qq)} 加入了本群", DetailItemType.Notice);
            }
        }

        private void PluginManagerProxy_OnGroupBan(long group, long qq, long operatedQQ, long time)
        {
            if (GroupMemberCache.TryGetValue(group, out var dict) && dict.ContainsKey(qq))
            {
                AddGroupChatItem(group, qq, $"{GetGroupMemberNick(group, qq)} 禁言了 {GetGroupMemberNick(group, operatedQQ)} {time}秒", DetailItemType.Notice);
            }
        }

        private void PluginManagerProxy_OnGroupLeft(long group, long qq)
        {
            if (GroupMemberCache.TryGetValue(group, out var dict) && dict.ContainsKey(qq))
            {
                dict.Remove(qq);
                AddGroupChatItem(group, qq, $"{GetGroupMemberNick(group, qq)}离开了群", DetailItemType.Notice);
            }
        }

        private void PluginManagerProxy_OnGroupMsg(long group, long qq, string msg)
        {
            AddGroupChatItem(group, qq, msg, DetailItemType.Receive);
            var item = ChatList.FirstOrDefault(x => x.Id == group && x.AvatarType == ChatAvatar.AvatarTypes.Fallback);
            if (item != null)
            {
                item.GroupName = GetGroupName(group);
                item.Detail = $"{GetGroupMemberNick(group, qq)}: {msg}";
                item.Time = DateTime.Now;
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                {
                    ChatList.Add(new ChatListItemViewModel
                    {
                        AvatarType = ChatAvatar.AvatarTypes.Fallback,
                        Detail = $"{GetGroupMemberNick(group, qq)}: {msg}",
                        GroupName = GetGroupName(group),
                        Id = group,
                        Time = DateTime.Now,
                    });
                });
            }
            ReorderChatList();
        }

        private void PluginManagerProxy_OnPrivateMsg(long qq, string msg)
        {
            AddPrivateChatItem(qq, msg, DetailItemType.Receive);

            var item = ChatList.FirstOrDefault(x => x.Id == qq && x.AvatarType == ChatAvatar.AvatarTypes.QQPrivate);
            if (item != null)
            {
                item.GroupName = GetFriendNick(qq);
                item.Detail = msg;
                item.Time = DateTime.Now;
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                {
                    ChatList.Add(new ChatListItemViewModel
                    {
                        AvatarType = ChatAvatar.AvatarTypes.QQPrivate,
                        Detail = msg,
                        GroupName = GetFriendNick(qq),
                        Id = qq,
                        Time = DateTime.Now,
                    });
                });
            }
            ReorderChatList();
        }

        private void ReorderChatList()
        {
            Dispatcher.BeginInvoke(() =>
            {
                ChatList = ChatList.OrderByDescending(x => x.Time).ToObservableCollection();
                OnPropertyChanged(nameof(ChatList));

                EmptyHint.Visibility = ChatList.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        private void ChatListDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = SelectedItem;
            if (item != null)
            {
                if (item.AvatarType == ChatAvatar.AvatarTypes.QQPrivate)
                {
                    if (FriendChatHistory.TryGetValue(item.Id, out var msg))
                    {
                        DetailList = msg;
                    }
                    else
                    {
                        FriendChatHistory.Add(item.Id, new());
                        DetailList = FriendChatHistory[item.Id];
                    }
                }
                else
                {
                    if (GroupChatHistory.TryGetValue(item.Id, out var msg))
                    {
                        DetailList = msg;
                    }
                    else
                    {
                        GroupChatHistory.Add(item.Id, new());
                        DetailList = GroupChatHistory[item.Id];
                    }
                }
            }
            OnPropertyChanged(nameof(DetailList));
        }

        private void FaceBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AtBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PictureBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AudioBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                return;
            }
            if (SelectedItem.AvatarType == ChatAvatar.AvatarTypes.QQPrivate)
            {
                AddPrivateChatItem(AppConfig.Instance.CurrentQQ, SendText.Text, DetailItemType.Send);
            }
            else
            {
                AddGroupChatItem(SelectedItem.Id, AppConfig.Instance.CurrentQQ, SendText.Text, DetailItemType.Send);
            }
            SendText.Text = "";
        }
    }
}