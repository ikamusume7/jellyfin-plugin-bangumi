using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Bangumi.Configuration;
#if !EMBY
using FuzzySharp;
using Levenshtein = Fastenshtein.Levenshtein;
#endif

namespace Jellyfin.Plugin.Bangumi.Model;

public class Subject
{
    private static PluginConfiguration Configuration => Plugin.Instance!.Configuration;

    public int Id { get; set; }

    public SubjectType Type { get; set; }

    [JsonIgnore]
    public string? Name => Configuration.TranslationPreference switch
    {
        TranslationPreferenceType.Chinese => string.IsNullOrEmpty(ChineseName) ? OriginalName : ChineseName,
        TranslationPreferenceType.Original => OriginalName,
        _ => OriginalName
    };

    [JsonIgnore]
    public string OriginalName => WebUtility.HtmlDecode(OriginalNameRaw);

    [JsonPropertyName("name")]
    public string OriginalNameRaw { get; set; } = "";

    [JsonIgnore]
    public string? ChineseName => WebUtility.HtmlDecode(ChineseNameRaw);

    [JsonPropertyName("name_cn")]
    public string? ChineseNameRaw { get; set; }

    [JsonIgnore]
    public string? Summary => SummaryRaw?.ToMarkdown();

    [JsonPropertyName("summary")]
    public string? SummaryRaw { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("air_date")]
    public string? Date2 { get; set; }

    [JsonIgnore]
    public string? AirDate => Date ?? Date2;

    [JsonIgnore]
    public string? ProductionYear => AirDate?.Length >= 4 ? AirDate?[..4] : null;

    public Dictionary<string, string>? Images { get; set; }

    [JsonIgnore]
    public string? DefaultImage => Images?["large"];

    [JsonPropertyName("eps")]
    public int? EpisodeCount { get; set; }

    [JsonPropertyName("rating")]
    public Rating? Rating { get; set; }

    [JsonPropertyName("tags")]
    public IEnumerable<Tag> AllTags { get; set; } = [];

    [JsonPropertyName("nsfw")]
    public bool IsNSFW { get; set; }

    public string? Platform { get; set; }

    [JsonPropertyName("meta_tags")]
    public IEnumerable<string> MetaTags { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<string> PopularTags => AllTags
        .OrderByDescending(tag => tag.Count)
        .Select(tag => tag.Name);

    [JsonIgnore]
    public IEnumerable<string> GenreTags => AllTags
        .Where(tag => Tag.GetCommonTagList(Type).Contains(tag.Name))
        .OrderByDescending(tag => tag.Count)
        .Select(tag => tag.Name)
        .Take(4);

    [JsonPropertyName("infobox")]
    public JsonElement? JsonInfoBox
    {
        get => null;
        set => InfoBox = InfoBox.ParseJson(value!.Value);
    }

    [JsonIgnore]
    public InfoBox? InfoBox { get; set; }

    [JsonIgnore]
    public string? OfficialWebSite => InfoBox?.Get("官方网站");

    [JsonIgnore]
    public IEnumerable<string>? Alias => InfoBox?.GetList("别名");

    [JsonIgnore]
    public DateTime? EndDate
    {
        get
        {
            var dateStr = InfoBox?.Get("播放结束");
            if (dateStr != null && DateTime.TryParseExact(dateStr, "yyyy年MM月dd日", CultureInfo.GetCultureInfo("zh-CN"), DateTimeStyles.None, out var date))
                return date;
            return null;
        }
    }

    [JsonIgnore]
    public string? BroadcastTime => InfoBox?.Get("放送时间");

    [JsonIgnore]
    public DayOfWeek? BroadcastWeekday
    {
        get
        {
            var raw = InfoBox?.Get("放送星期");
            return raw switch
            {
                "星期一" or "周一" => DayOfWeek.Monday,
                "星期二" or "周二" => DayOfWeek.Tuesday,
                "星期三" or "周三" => DayOfWeek.Wednesday,
                "星期四" or "周四" => DayOfWeek.Thursday,
                "星期五" or "周五" => DayOfWeek.Friday,
                "星期六" or "周六" => DayOfWeek.Saturday,
                "星期日" or "星期天" or "周日" or "周天" => DayOfWeek.Sunday,
                _ => null
            };
        }
    }

    /// <summary>Animation studio from infobox (动画制作 or 制作公司).</summary>
    [JsonIgnore]
    public string? AnimationStudio => InfoBox?.Get("动画制作") ?? InfoBox?.Get("制作公司");

    /// <summary>
    /// All production/broadcast entities: 动画制作, 制作公司, 放送电视台, 网络.
    /// Multiple values per key (newline-separated in the infobox) are each emitted as separate entries.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<string> AllStudios =>
        new[] { "动画制作", "制作公司", "放送电视台", "网络" }
            .SelectMany(k => InfoBox?.GetList(k) ?? Enumerable.Empty<string>())
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<Subject> SortBySimilarity(IEnumerable<Subject> list, string keyword)
    {
#if EMBY
        return list;
#else
        var instance = new Levenshtein(keyword);
        return list
            .OrderBy(subject =>
                Math.Min(
                    instance.DistanceFrom(subject.ChineseName ?? subject.OriginalName),
                    instance.DistanceFrom(subject.OriginalName)
                )
            );
#endif
    }

    public static IEnumerable<Subject> SortByFuzzScore(IEnumerable<Subject> list, string keyword, int minScore = 0)
    {
#if EMBY
        return list;
#else
        keyword = keyword.ToLower();

        var score = list.Select(subject =>
            {
                var chineseNameScore = string.IsNullOrEmpty(subject.ChineseName)
                    ? 0
                    : Fuzz.Ratio(subject.ChineseName.ToLower(), keyword);
                var originalNameScore = Fuzz.Ratio(subject.OriginalName.ToLower(), keyword);
                var aliasScore = subject.Alias?.Select(alias => Fuzz.Ratio(alias.ToLower(), keyword)) ?? [];

                var maxScore = Math.Max(chineseNameScore, Math.Max(originalNameScore, aliasScore.DefaultIfEmpty(int.MinValue).Max()));

                return new
                {
                    Subject = subject,
                    Score = maxScore
                };
            })
            .Where(pair => pair.Score >= minScore)
            .OrderByDescending(pair => pair.Score)
            .Select(pair => pair.Subject);

        return score;
#endif
    }
}
