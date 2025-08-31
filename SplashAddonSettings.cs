// SplashAddonSettings.cs
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
    public class SplashAddonSettings : ObservableObject, ISettings
    {
        private List<string> _excludedGameIds = new List<string>();
        private int _splashScreenDuration = 10;
        private bool _usePlatformSpecificTimers = false;
        private Dictionary<string, int> _gameSpecificDurations = new Dictionary<string, int>();
        private bool _useGameStartedTimer = false;

        // Private fields for logo size
        private int _logoSize = 300;

        // Hardcoded properties for the 10 platforms and their durations
        private string _platform1;
        private int _duration1;
        private string _platform2;
        private int _duration2;
        private string _platform3;
        private int _duration3;
        private string _platform4;
        private int _duration4;
        private string _platform5;
        private int _duration5;
        private string _platform6;
        private int _duration6;
        private string _platform7;
        private int _duration7;
        private string _platform8;
        private int _duration8;
        private string _platform9;
        private int _duration9;
        private string _platform10;
        private int _duration10;

        [JsonProperty("ExcludedGameIds")]
        public List<string> ExcludedGameIds { get => _excludedGameIds; set => SetValue(ref _excludedGameIds, value); }

        [JsonProperty("SplashScreenDuration")]
        public int SplashScreenDuration { get => _splashScreenDuration; set => SetValue(ref _splashScreenDuration, value); }

        [JsonProperty("UsePlatformSpecificTimers")]
        public bool UsePlatformSpecificTimers { get => _usePlatformSpecificTimers; set => SetValue(ref _usePlatformSpecificTimers, value); }

        [JsonProperty("GameSpecificDurations")]
        public Dictionary<string, int> GameSpecificDurations { get => _gameSpecificDurations; set => SetValue(ref _gameSpecificDurations, value); }

        [JsonProperty("UseGameStartedTimer")]
        public bool UseGameStartedTimer { get => _useGameStartedTimer; set => SetValue(ref _useGameStartedTimer, value); }

        // Mapped property for logo size
        [JsonProperty("LogoSize")]
        public int LogoSize { get => _logoSize; set => SetValue(ref _logoSize, value); }

        // Mapped properties for the 10 hardcoded UI elements
        [JsonProperty("Platform1")]
        public string Platform1 { get => _platform1; set => SetValue(ref _platform1, value); }
        [JsonProperty("Duration1")]
        public int Duration1 { get => _duration1; set => SetValue(ref _duration1, value); }
        [JsonProperty("Platform2")]
        public string Platform2 { get => _platform2; set => SetValue(ref _platform2, value); }
        [JsonProperty("Duration2")]
        public int Duration2 { get => _duration2; set => SetValue(ref _duration2, value); }
        [JsonProperty("Platform3")]
        public string Platform3 { get => _platform3; set => SetValue(ref _platform3, value); }
        [JsonProperty("Duration3")]
        public int Duration3 { get => _duration3; set => SetValue(ref _duration3, value); }
        [JsonProperty("Platform4")]
        public string Platform4 { get => _platform4; set => SetValue(ref _platform4, value); }
        [JsonProperty("Duration4")]
        public int Duration4 { get => _duration4; set => SetValue(ref _duration4, value); }
        [JsonProperty("Platform5")]
        public string Platform5 { get => _platform5; set => SetValue(ref _platform5, value); }
        [JsonProperty("Duration5")]
        public int Duration5 { get => _duration5; set => SetValue(ref _duration5, value); }
        [JsonProperty("Platform6")]
        public string Platform6 { get => _platform6; set => SetValue(ref _platform6, value); }
        [JsonProperty("Duration6")]
        public int Duration6 { get => _duration6; set => SetValue(ref _duration6, value); }
        [JsonProperty("Platform7")]
        public string Platform7 { get => _platform7; set => SetValue(ref _platform7, value); }
        [JsonProperty("Duration7")]
        public int Duration7 { get => _duration7; set => SetValue(ref _duration7, value); }
        [JsonProperty("Platform8")]
        public string Platform8 { get => _platform8; set => SetValue(ref _platform8, value); }
        [JsonProperty("Duration8")]
        public int Duration8 { get => _duration8; set => SetValue(ref _duration8, value); }
        [JsonProperty("Platform9")]
        public string Platform9 { get => _platform9; set => SetValue(ref _platform9, value); }
        [JsonProperty("Duration9")]
        public int Duration9 { get => _duration9; set => SetValue(ref _duration9, value); }
        [JsonProperty("Platform10")]
        public string Platform10 { get => _platform10; set => SetValue(ref _platform10, value); }
        [JsonProperty("Duration10")]
        public int Duration10 { get => _duration10; set => SetValue(ref _duration10, value); }

        [JsonIgnore]
        public string ExcludedGameIdsText
        {
            get => string.Join(Environment.NewLine, ExcludedGameIds ?? new List<string>());
            set
            {
                var ids = value?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 ?.Select(id => id.Trim())
                                 ?.Where(id => !string.IsNullOrEmpty(id))
                                 ?.Distinct()
                                 ?.ToList() ?? new List<string>();
                ExcludedGameIds = ids;
                OnPropertyChanged(nameof(ExcludedGameIdsText));
            }
        }

        [JsonIgnore]
        public string GameSpecificDurationsText
        {
            get
            {
                if (GameSpecificDurations == null || !GameSpecificDurations.Any())
                    return string.Empty;

                return string.Join(Environment.NewLine,
                    GameSpecificDurations.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            }
            set
            {
                ParseGameSpecificDurations(value);
                OnPropertyChanged(nameof(GameSpecificDurationsText));
            }
        }

        public void ParseGameSpecificDurations(string rawText)
        {
            var durations = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(rawText))
            {
                var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    var parts = trimmedLine.Split(':');
                    int duration;
                    if (parts.Length == 2 &&
                        !string.IsNullOrWhiteSpace(parts[0]) &&
                        int.TryParse(parts[1].Trim(), out duration) &&
                        duration > 0)
                    {
                        durations[parts[0].Trim()] = duration;
                    }
                }
            }
            GameSpecificDurations = durations;
        }

        public SplashAddonSettings()
        {
            if (GameSpecificDurations == null)
                GameSpecificDurations = new Dictionary<string, int>();
        }

        [JsonIgnore]
        public static List<string> AvailablePlatforms => new List<string>
        {
            "",
            "3DO Interactive Multiplayer",
            "Amstrad CPC",
            "Apple II",
            "Atari 2600",
            "Atari 5200",
            "Atari 7800",
            "Atari 8-bit",
            "Atari Jaguar",
            "Atari Lynx",
            "Atari ST/STE",
            "Bandai WonderSwan",
            "Bandai WonderSwan Color",
            "Coleco ColecoVision",
            "Commodore Amiga",
            "Commodore Amiga CD32",
            "Commodore Amiga CDTV",
            "Commodore Plus/4",
            "Commodore VIC20",
            "GCE Vectrex",
            "Macintosh",
            "Magnavox Odyssey 2",
            "Mattel Intellivision",
            "Microsoft MSX",
            "Microsoft MSX2",
            "Microsoft Xbox",
            "Microsoft Xbox 360",
            "Microsoft Xbox One",
            "Microsoft Xbox Series",
            "NEC PC-98",
            "NEC PC-FX",
            "NEC SuperGrafx",
            "NEC TurboGrafx 16",
            "NEC TurboGrafx-CD",
            "Nintendo 3DS",
            "Nintendo 64",
            "Nintendo DS",
            "Nintendo DSi",
            "Nintendo Entertainment System",
            "Nintendo Family Computer Disk System",
            "Nintendo Game Boy",
            "Nintendo Game Boy Advance",
            "Nintendo Game Boy Color",
            "Nintendo GameCube",
            "Nintendo SNES",
            "Nintendo Switch",
            "Nintendo Switch 2",
            "Nintendo Virtual Boy",
            "Nintendo Wii",
            "Nintendo Wii U",
            "PC",
            "PC (DOS)",
            "PC (Linux)",
            "PC (Windows)",
            "Philips CD-i",
            "PlayStation",
            "PlayStation 2",
            "PlayStation 3",
            "PlayStation 4",
            "PlayStation 5",
            "PlayStation Portable",
            "PlayStation Vita",
            "Sega 32X",
            "Sega CD",
            "Sega Dreamcast",
            "Sega Game Gear",
            "Sega Genesis",
            "Sega Master System",
            "Sega Saturn",
            "SNK Neo Geo",
            "SNK Neo Geo CD",
            "SNK Neo Geo Pocket",
            "SNK Neo Geo Pocket Color",
            "Sony PSP"
        };

        public void BeginEdit() { }
        public void CancelEdit() { }
        public void EndEdit() { }

        public ISettings GetDefaults()
        {
            // Return a new instance with default values
            return new SplashAddonSettings();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (SplashScreenDuration <= 0)
            {
                errors.Add("Splash Screen Duration must be greater than 0.");
            }
            if (LogoSize < 0)
            {
                errors.Add("Logo size must be greater than or equal to 0.");
            }
            if (GameSpecificDurations != null)
            {
                foreach (var kvp in GameSpecificDurations)
                {
                    if (kvp.Value <= 0)
                    {
                        errors.Add($"Duration for game ID '{kvp.Key}' must be greater than 0.");
                    }
                }
            }
            return errors.Count == 0;
        }

        public int GetDurationForGame(string gameId, string platformName)
        {
            // Priority 1: Check for game-specific duration
            if (GameSpecificDurations != null && !string.IsNullOrEmpty(gameId) && GameSpecificDurations.ContainsKey(gameId))
            {
                return GameSpecificDurations[gameId];
            }

            // Priority 2: Check for platform-specific duration if the option is enabled
            if (UsePlatformSpecificTimers && !string.IsNullOrEmpty(platformName))
            {
                var platformDurations = new Dictionary<string, int>();

                // Safely add platform durations, checking for null/empty keys
                if (!string.IsNullOrEmpty(Platform1)) platformDurations.Add(Platform1, Duration1);
                if (!string.IsNullOrEmpty(Platform2)) platformDurations.Add(Platform2, Duration2);
                if (!string.IsNullOrEmpty(Platform3)) platformDurations.Add(Platform3, Duration3);
                if (!string.IsNullOrEmpty(Platform4)) platformDurations.Add(Platform4, Duration4);
                if (!string.IsNullOrEmpty(Platform5)) platformDurations.Add(Platform5, Duration5);
                if (!string.IsNullOrEmpty(Platform6)) platformDurations.Add(Platform6, Duration6);
                if (!string.IsNullOrEmpty(Platform7)) platformDurations.Add(Platform7, Duration7);
                if (!string.IsNullOrEmpty(Platform8)) platformDurations.Add(Platform8, Duration8);
                if (!string.IsNullOrEmpty(Platform9)) platformDurations.Add(Platform9, Duration9);
                if (!string.IsNullOrEmpty(Platform10)) platformDurations.Add(Platform10, Duration10);

                // Try exact match first
                if (platformDurations.ContainsKey(platformName))
                {
                    var duration = platformDurations[platformName];
                    if (duration > 0)
                    {
                        return duration;
                    }
                }

                // Try partial matches for common platform name variations
                var normalizedPlatform = platformName.ToLowerInvariant();
                foreach (var kvp in platformDurations)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        var key = kvp.Key.ToLowerInvariant();
                        if ((key.Contains(normalizedPlatform) || normalizedPlatform.Contains(key)) && kvp.Value > 0)
                            return kvp.Value;
                    }
                }
            }

            // Priority 3: Use default duration as the ultimate fallback
            return SplashScreenDuration;
        }
    }
}
