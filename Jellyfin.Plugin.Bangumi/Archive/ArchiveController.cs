using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bangumi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Bangumi.Archive;

[ApiController]
[Route("Plugins/Bangumi/Archive")]
public class OAuthController(ArchiveData archive)
    : ControllerBase
{
    [HttpGet("Status")]
    [Authorize]
    public Dictionary<string, object?> Status()
    {
        var totalSize = 0L;
        DateTime? lastModifyTime = null;

        var directory = new DirectoryInfo(archive.BasePath);
        if (!directory.Exists)
            return [];

        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if (lastModifyTime == null)
                lastModifyTime = info.LastWriteTime;
            else if (info.LastWriteTime.CompareTo(lastModifyTime) > 0)
                lastModifyTime = info.LastWriteTime;
            if (info is FileInfo fileInfo)
                totalSize += fileInfo.Length;
        }

        return new Dictionary<string, object?>
        {
            ["path"] = archive.BasePath,
            ["size"] = totalSize,
            ["time"] = lastModifyTime
        };
    }

    [HttpDelete("Store")]
    [Authorize]
    public bool Delete()
    {
        Directory.Delete(archive.BasePath, true);
        Directory.CreateDirectory(archive.BasePath);
        return false;
    }

    /// <summary>
    /// Rebuild all archive indexes from existing .jsonlines files in the archive directory.
    /// Use this after manually placing downloaded archive files into the archive folder.
    /// Required files: subject.jsonlines, episode.jsonlines, character.jsonlines, person.jsonlines,
    ///   subject-relations.jsonlines, subject-persons.jsonlines
    /// </summary>
    [HttpPost("RebuildIndex")]
    [Authorize]
    public async Task<ActionResult> RebuildIndex(CancellationToken cancellationToken)
    {
        foreach (var store in archive.Stores)
        {
            if (!System.IO.File.Exists(store.FilePath))
                return BadRequest($"Missing file: {store.FileName}. Place all .jsonlines files in {archive.BasePath} first.");
            await store.GenerateIndex(cancellationToken);
        }

        await archive.SubjectRelations.GenerateIndex(
            Path.Join(archive.BasePath, "subject-relations.jsonlines"), cancellationToken);
        await archive.SubjectEpisodeRelation.GenerateIndex(cancellationToken);
        await archive.SubjectPersonRelation.GenerateIndex(
            Path.Join(archive.BasePath, "subject-persons.jsonlines"), cancellationToken);

        return Ok(new { message = "Index rebuilt successfully.", path = archive.BasePath });
    }

    /// <summary>
    /// Returns the parsed infobox for a subject from the local archive.
    /// Falls back to the Bangumi API if the subject is not in the archive.
    /// </summary>
    [HttpGet("Subject/{subjectId:int}/Infobox")]
    [Authorize]
    public async Task<ActionResult<List<InfoBoxEntry>>> GetSubjectInfoBox(int subjectId, CancellationToken cancellationToken)
    {
        var archiveSubject = await archive.Subject.FindById(subjectId, cancellationToken);
        if (archiveSubject == null)
            return NotFound();

        var subject = archiveSubject.ToSubject();
        if (subject.InfoBox == null)
            return new List<InfoBoxEntry>();

        var result = new List<InfoBoxEntry>();
        foreach (var (key, value) in subject.InfoBox)
        {
            // Skip sub-keys like "别名/1" – they are already merged as newline-separated in the parent key
            if (key.Contains('/'))
                continue;
            result.Add(new InfoBoxEntry { Key = key, Value = value });
        }
        return result;
    }
}

public class InfoBoxEntry
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
