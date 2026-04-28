using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Bangumi.Configuration;

public enum TranslationPreferenceType
{
    Original,
    Chinese
}

public enum EpisodeParserType
{
    Basic,
    AnitomySharp
}

public class LibraryBangumiSettings
{
    public bool Enabled { get; set; } = false;
    public bool OfflineOnly { get; set; } = false;
}

/// <summary>
/// XML-serializable key-value entry for per-library Bangumi settings.
/// </summary>
public class LibrarySettingsEntry
{
    public string LibraryId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public bool OfflineOnly { get; set; } = false;
}

public class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = false;

    public bool OfflineOnly { get; set; } = false;

    /// <summary>
    /// Per-library Bangumi settings. Keyed by library ItemId (GUID string).
    /// Uses a List for XML serialization compatibility (Dictionary not supported).
    /// </summary>
    public List<LibrarySettingsEntry> LibrarySettings { get; set; } = [];

    public TranslationPreferenceType TranslationPreference { get; set; } = TranslationPreferenceType.Chinese;

    public TranslationPreferenceType PersonTranslationPreference { get; set; } = TranslationPreferenceType.Original;

    public int RequestTimeout { get; set; } = 5000;

    public string BaseServerUrl { get; set; } = "https://api.bgm.tv";

    public bool ReportPlaybackStatusToBangumi { get; set; } = true;

    public bool SkipNSFWPlaybackReport { get; set; } = true;

    public bool ReportManualStatusChangeToBangumi { get; set; } = false;

    public bool TrustExistedBangumiId { get; set; } = false;

    public bool UseBangumiSeasonTitle { get; set; } = true;

    public bool AlwaysGetTitleByAnitomySharp { get; set; }

    public bool UseTestingSearchApi { get; set; }

    public int SeasonGuessMaxSearchCount { get; set; } = 2;

    public bool SortByFuzzScore { get; set; } = false;

    public bool RefreshRecentEpisodeWhenArchiveUpdate { get; set; } = false;

    public bool RefreshRatingWhenArchiveUpdate { get; set; } = false;

    public int DaysBeforeUsingArchiveData { get; set; } = 14;

    public int RatingUpdateMinInterval { get; set; } = 14;

    public EpisodeParserType EpisodeParser { get; set; } = EpisodeParserType.Basic;

    public bool AlwaysReplaceEpisodeNumber { get; set; }

    public bool ProcessMultiSeasonFolderByAnitomySharp { get; set; } = false;
    
    public bool MovieEpisodeDetectionByAnitomySharp { get; set; } = false;

    public string? ProxyServerUrl { get; set; }

    public bool UseOriginalTitleFirst { get; set; }

    /// <summary>
    /// Newline-separated list of tags that should never be written to Jellyfin items.
    /// </summary>
    public string TagBlockList { get; set; } = string.Empty;

    public HashSet<string> GetTagBlockSet() =>
        new HashSet<string>(
            TagBlockList.Split('\n', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
}
