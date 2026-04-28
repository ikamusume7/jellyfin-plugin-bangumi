using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Bangumi.Configuration;

public static class LibrarySettingsHelper
{
    /// <summary>
    /// Resolve the effective Bangumi settings for a given item path.
    /// Checks per-library settings first, then falls back to global config.
    /// </summary>
    public static (bool Enabled, bool OfflineOnly) GetEffectiveSettings(string? itemPath, ILibraryManager? libraryManager)
    {
        var config = Plugin.Instance!.Configuration;
        var globalEnabled = config.Enabled;
        var globalOffline = config.OfflineOnly;

        if (string.IsNullOrEmpty(itemPath) || libraryManager == null)
            return (globalEnabled, globalOffline);

        var normalizedPath = Path.GetFullPath(itemPath);

        try
        {
            var folders = libraryManager.GetVirtualFolders();
            foreach (var folder in folders)
            {
                var locations = folder.Locations ?? [];
                foreach (var loc in locations)
                {
                    var normalizedLoc = Path.GetFullPath(loc);
                    if (normalizedPath.StartsWith(normalizedLoc, StringComparison.OrdinalIgnoreCase))
                    {
                        var libSettings = config.LibrarySettings.FirstOrDefault(e => e.LibraryId == folder.ItemId);
                        if (libSettings != null)
                            return (libSettings.Enabled, libSettings.OfflineOnly);
                        return (globalEnabled, globalOffline);
                    }
                }
            }
        }
        catch
        {
            // Fallback to global
        }

        return (globalEnabled, globalOffline);
    }

    /// <summary>
    /// Check if Bangumi is enabled for the given item path.
    /// </summary>
    public static bool IsEnabled(string? itemPath, ILibraryManager? libraryManager)
        => GetEffectiveSettings(itemPath, libraryManager).Enabled;

    /// <summary>
    /// Check if offline-only mode is active for the given item path.
    /// </summary>
    public static bool IsOfflineOnly(string? itemPath, ILibraryManager? libraryManager)
        => GetEffectiveSettings(itemPath, libraryManager).OfflineOnly;
}
