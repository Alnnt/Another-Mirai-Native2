﻿using Another_Mirai_Native.Model;
using Another_Mirai_Native.UI.Pages;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Another_Mirai_Native.UI.Controls
{
    public static class ChatDetailListItem_Common
    {
        public static Dictionary<string, BitmapImage> CachedImage { get; set; } = new();

        public static double ImageMaxHeight { get; set; } = 450;

        public static TextBox BuildAtElement(string nick)
        {
            var textBox = BuildTextElement($" @{nick} ");
            //textBox.SetResourceReference(TextBox.ForegroundProperty, "SystemControlHighlightAccentBrush");
            return textBox;
        }

        public static Grid BuildImageElement(CQCode cqCode, double maxWidth)
        {
            ImageBrush CreateImageBrush(BitmapImage image)
            {
                var brush = new ImageBrush(image)
                {
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    TileMode = TileMode.None
                };
                return brush;
            }
            void SetBackground(Dispatcher dispatcher, Viewbox viewBox, ProgressRing progressRing, BitmapImage bitmapImage)
            {
                dispatcher.BeginInvoke(() =>
                {
                    Image image = new();
                    image.Stretch = Stretch.Uniform;
                    image.Source = bitmapImage;

                    RectangleGeometry clipGeometry = new RectangleGeometry
                    {
                        RadiusX = 10,
                        RadiusY = 10,
                        Rect = new Rect(0, 0, bitmapImage.Width, bitmapImage.Height)
                    };
                    image.Clip = clipGeometry;
                    viewBox.Child = image;

                    viewBox.Visibility = Visibility.Visible;
                    progressRing.Visibility = Visibility.Collapsed;
                });
            }
            Grid grid = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 100,
                MinWidth = 100,
                MaxHeight = ImageMaxHeight
            };

            var viewBox = new Viewbox()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 100,
                MinWidth = 100,
                MaxHeight = ImageMaxHeight,
                Visibility = Visibility.Collapsed
            };
            grid.Children.Add(viewBox);
            viewBox.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    Debug.WriteLine("DbClick");
                }
            };
            viewBox.SetResourceReference(Border.BackgroundProperty, "SystemControlPageBackgroundChromeMediumLowBrush");
            RenderOptions.SetBitmapScalingMode(viewBox, BitmapScalingMode.Fant);
            var progressRing = new ProgressRing
            {
                IsActive = true,
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            grid.Children.Add(progressRing);

            string url = Extend.GetImageUrlOrPathFromCQCode(cqCode);
            if (CachedImage.TryGetValue(url, out BitmapImage? img))
            {
                SetBackground(viewBox.Dispatcher, viewBox, progressRing, img);
                return grid;
            }

            var bitmapImage = new BitmapImage();
            bitmapImage.DownloadCompleted += (_, _) =>
            {
                if (CachedImage.ContainsKey(url))
                {
                    CachedImage[url] = bitmapImage;
                }
                else
                {
                    CachedImage.Add(url, bitmapImage);
                }
                SetBackground(viewBox.Dispatcher, viewBox, progressRing, bitmapImage);
            };
            bitmapImage.DownloadFailed += (_, _) =>
            {
                FontIcon fontIcon = new()
                {
                    Width = 16,
                    Height = 16,
                    FontSize = 16,
                    Glyph = "\uF384"
                };
                viewBox.Dispatcher.BeginInvoke(() =>
                {
                    viewBox.Child = fontIcon;
                });
            };
            ChatPage.Instance.Dispatcher.BeginInvoke(() =>
            {
                bitmapImage.BeginInit();
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                {
                    bitmapImage.UriSource = uri;
                }
                bitmapImage.EndInit();

                // local pic
                if (!url.StartsWith("http"))
                {
                    SetBackground(viewBox.Dispatcher, viewBox, progressRing, bitmapImage);
                }
            });
            return grid;
        }

        public static TextBox BuildTextElement(string text)
        {
            return new TextBox
            {
                Text = text,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
        }
    }
}
