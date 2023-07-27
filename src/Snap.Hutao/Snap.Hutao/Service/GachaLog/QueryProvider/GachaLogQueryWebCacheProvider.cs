﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO;
using Snap.Hutao.Service.Game;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Web.Request.QueryString;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Snap.Hutao.Service.GachaLog.QueryProvider;

/// <summary>
/// 浏览器缓存方法
/// </summary>
[HighQuality]
[ConstructorGenerated]
[Injection(InjectAs.Transient)]
internal sealed partial class GachaLogQueryWebCacheProvider : IGachaLogQueryProvider
{
    private readonly IGameService gameService;
    private readonly MetadataOptions metadataOptions;

    /// <inheritdoc/>
    public string Name { get => nameof(GachaLogQueryWebCacheProvider); }

    /// <summary>
    /// 获取缓存文件路径
    /// </summary>
    /// <param name="path">游戏路径</param>
    /// <returns>缓存文件路径</returns>
    public static string GetCacheFile(string path)
    {
        string exeName = Path.GetFileName(path);
        string dataFolder = exeName == GameConstants.GenshinImpactFileName
            ? GameConstants.GenshinImpactData
            : GameConstants.YuanShenData;

        DirectoryInfo webCacheFolder = new(Path.Combine(Path.GetDirectoryName(path)!, dataFolder, "webCaches"));
        Regex versionRegex = VersionRegex();
        DirectoryInfo? lastestVersionCacheFolder = webCacheFolder
            .EnumerateDirectories()
            .Where(dir => versionRegex.IsMatch(dir.Name))
            .MaxBy(dir => new Version(dir.Name));

        lastestVersionCacheFolder ??= webCacheFolder;
        return Path.Combine(lastestVersionCacheFolder.FullName, @"Cache\Cache_Data\data_2");
    }

    /// <inheritdoc/>
    public async Task<ValueResult<bool, GachaLogQuery>> GetQueryAsync()
    {
        (bool isOk, string path) = await gameService.GetGamePathAsync().ConfigureAwait(false);

        if (isOk && (!string.IsNullOrEmpty(path)))
        {
            string cacheFile = GetCacheFile(path);

            using (TempFile? tempFile = TempFile.CopyFrom(cacheFile))
            {
                if (tempFile.TryGetValue(out TempFile file))
                {
                    using (FileStream fileStream = new(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (MemoryStream memoryStream = new())
                        {
                            await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                            string? result = Match(memoryStream, cacheFile.Contains(GameConstants.GenshinImpactData));

                            if (!string.IsNullOrEmpty(result))
                            {
                                QueryString query = QueryString.Parse(result.TrimEnd("#/log"));
                                string queryLanguageCode = query["lang"];
                                if (metadataOptions.IsCurrentLocale(queryLanguageCode))
                                {
                                    return new(true, new(result));
                                }
                                else
                                {
                                    string message = string.Format(
                                        SH.ServiceGachaLogUrlProviderUrlLanguageNotMatchCurrentLocale,
                                        queryLanguageCode,
                                        metadataOptions.LocaleName);
                                    return new(false, message);
                                }
                            }
                            else
                            {
                                return new(false, SH.ServiceGachaLogUrlProviderCacheUrlNotFound);
                            }
                        }
                    }
                }

                return new(false, string.Format(Regex.Unescape(SH.ServiceGachaLogUrlProviderCachePathNotFound), cacheFile));
            }
        }
        else
        {
            return new(false, SH.ServiceGachaLogUrlProviderCachePathInvalid);
        }
    }

    private static string? Match(MemoryStream stream, bool isOversea)
    {
        ReadOnlySpan<byte> span = stream.ToArray();
        ReadOnlySpan<byte> match = isOversea
            ? "https://webstatic-sea.hoyoverse.com/genshin/event/e20190909gacha-v2/index.html"u8
            : "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v2/index.html"u8;

        int index = span.LastIndexOf(match);
        if (index >= 0)
        {
            int length = span[index..].IndexOf("\0"u8);
            return Encoding.UTF8.GetString(span.Slice(index, length));
        }

        return null;
    }

    [GeneratedRegex("^[1-9]+?\\.[0-9]+?\\.[0-9]+?\\.[0-9]+?$")]
    private static partial Regex VersionRegex();
}