// SplashAddon.cs
// Author: Artzox

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace SplashAddon
{
    public class SplashAddonPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private SplashAddonSettings _settings;
        private DateTime _gameStartTimestamp;

        public override Guid Id { get; } = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        public SplashAddonPlugin(IPlayniteAPI api) : base(api)
        {
            _settings = LoadPluginSettings<SplashAddonSettings>() ?? new SplashAddonSettings();
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            var settingsView = new SplashAddonSettingsView();
            settingsView.DataContext = _settings;
            return settingsView;
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            SavePluginSettings(_settings);
            base.OnApplicationStopped(args);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            _gameStartTimestamp = DateTime.Now;
            if (_settings.UseGameStartedTimer)
            {
                // Show splash screen, but let OnGameStarted handle the timer
                ShowSplashScreen(args.Game, 0, false);
            }
            else
            {
                // Show splash screen with its own timer
                ShowSplashScreen(args.Game, _settings.GetDurationForGame(args.Game.Id.ToString(), args.Game.Platforms?.FirstOrDefault()?.Name ?? string.Empty), true);
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (_settings.UseGameStartedTimer)
            {
                TimeSpan elapsed = DateTime.Now - _gameStartTimestamp;
                int remainingDuration = _settings.GetDurationForGame(args.Game.Id.ToString(), args.Game.Platforms?.FirstOrDefault()?.Name ?? string.Empty) - (int)elapsed.TotalSeconds;

                if (remainingDuration > 0)
                {
                    // This will find the active splash window and set its close timer
                    SetCloseTimer(remainingDuration);
                }
                else
                {
                    // If the time has already passed, close the window immediately
                    SetCloseTimer(0);
                }
            }
        }

        private void SetCloseTimer(int durationInSeconds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var splashWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.Title == "SplashAddonSplashScreen");
                if (splashWindow == null) return;

                var closeTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(durationInSeconds)
                };

                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                    Storyboard.SetTarget(fadeOut, splashWindow);
                    Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Window.OpacityProperty));
                    var fadeStoryboard = new Storyboard();
                    fadeStoryboard.Children.Add(fadeOut);
                    fadeStoryboard.Completed += (s2, e2) =>
                    {
                        try
                        {
                            splashWindow.Close();
                        }
                        catch { }
                    };
                    fadeStoryboard.Begin();
                };

                if (durationInSeconds > 0)
                {
                    closeTimer.Start();
                }
                else
                {
                    // Immediately trigger close if duration is 0
                    closeTimer.Stop();
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                    Storyboard.SetTarget(fadeOut, splashWindow);
                    Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Window.OpacityProperty));
                    var fadeStoryboard = new Storyboard();
                    fadeStoryboard.Children.Add(fadeOut);
                    fadeStoryboard.Completed += (s2, e2) =>
                    {
                        try
                        {
                            splashWindow.Close();
                        }
                        catch { }
                    };
                    fadeStoryboard.Begin();
                }
            });
        }

        private void ShowSplashScreen(Game game, int durationInSeconds, bool startTimerImmediately)
        {
            if (_settings.ExcludedGameIds.Any(id => id.Trim() == game.Id.ToString()))
            {
                return;
            }

            string platformName = game.Platforms?.FirstOrDefault()?.Name ?? string.Empty;
            int duration = _settings.GetDurationForGame(game.Id.ToString(), platformName);

            if (duration <= 0)
            {
                duration = _settings.SplashScreenDuration;
                if (duration <= 0)
                {
                    duration = 1;
                }
            }

            string bgImagePath = game.BackgroundImage;
            string resolvedBgPath = null;
            if (!string.IsNullOrEmpty(bgImagePath))
            {
                try
                {
                    if (bgImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedBgPath = bgImagePath;
                    }
                    else
                    {
                        string playniteDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                        if (!Path.IsPathRooted(bgImagePath))
                        {
                            resolvedBgPath = Path.Combine(playniteDir, "library", "files", bgImagePath);
                        }
                        else
                        {
                            resolvedBgPath = bgImagePath;
                        }
                        if (!File.Exists(resolvedBgPath))
                        {
                            resolvedBgPath = null;
                        }
                    }
                }
                catch { }
            }
            string logoPath = null;
            try
            {
                string extraMetadataDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "ExtraMetadata", "Games", game.Id.ToString(), "Logo.png");
                if (File.Exists(extraMetadataDir))
                    logoPath = extraMetadataDir;
            }
            catch { }
            var splashWindow = new Window
            {
                Title = "SplashAddonSplashScreen", // Added title for lookup
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Topmost = true,
                Background = Brushes.Black,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Opacity = 0
            };
            Image bgImage = null;
            if (!string.IsNullOrEmpty(resolvedBgPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(resolvedBgPath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bgImage = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };
                }
                catch { }
            }
            if (bgImage == null)
                bgImage = new Image { Stretch = Stretch.UniformToFill };
            Image logoImage = null;
            if (!string.IsNullOrEmpty(logoPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    logoImage = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.Uniform,
                        Width = 300,
                        Height = 100,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(20, 0, 0, 20)
                    };
                }
                catch { }
            }
            var grid = new Grid();
            grid.Children.Add(bgImage);
            if (logoImage != null)
                grid.Children.Add(logoImage);
            splashWindow.Content = grid;
            var storyboard = new Storyboard();
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1)
            };
            Storyboard.SetTarget(fadeIn, splashWindow);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(Window.OpacityProperty));
            storyboard.Children.Add(fadeIn);

            if (startTimerImmediately)
            {
                var closeTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(duration)
                };
                closeTimer.Tick += (s, e) =>
                {
                    closeTimer.Stop();
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                    Storyboard.SetTarget(fadeOut, splashWindow);
                    Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Window.OpacityProperty));
                    var fadeStoryboard = new Storyboard();
                    fadeStoryboard.Children.Add(fadeOut);
                    fadeStoryboard.Completed += (s2, e2) =>
                    {
                        try
                        {
                            splashWindow.Close();
                        }
                        catch { }
                    };
                    fadeStoryboard.Begin();
                };
                splashWindow.Loaded += (s, e) =>
                {
                    storyboard.Begin();
                    closeTimer.Start();
                };
            }
            else
            {
                splashWindow.Loaded += (s, e) =>
                {
                    storyboard.Begin();
                };
            }

            try
            {
                splashWindow.Show();
                splashWindow.Activate();
            }
            catch { }
        }
    }
}