using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using System.IO;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public static class NetUtil
{
    private static readonly HttpClient Client = new();

    public static async Task<byte[]> DownloadFromUrlAsync(string url)
    {
        return await Client.GetByteArrayAsync(url).ConfigureAwait(false);
    }

    public static async Task<Download<ISpeciesForm>> DownloadEntityAsync(this IAttachment att)
    {
        var result = new Download<ISpeciesForm> { SanitizedFileName = Format.Sanitize(att.Filename) };

        var atType = GetAttachmentType(att);
        if (atType is AttachmentType.Invalid)
        {
            result.ErrorMessage = $"{result.SanitizedFileName}: Invalid size.";
            return result;
        }

        string url = att.Url;

        // Download the resource and load the bytes into a buffer.
        var buffer = await DownloadFromUrlAsync(url).ConfigureAwait(false);

        // Ensure it actually converts into a file we can use.
        var prefer = EntityFileExtension.GetContextFromExtension(result.SanitizedFileName);

        ISpeciesForm? entity = atType switch
        {
            AttachmentType.Wondercard => MysteryGift.GetMysteryGift(buffer, Path.GetExtension(result.SanitizedFileName)),
            _ => EntityFormat.GetFromBytes(buffer, prefer)
        };

        if (entity == null)
        {
            result.ErrorMessage = $"{result.SanitizedFileName}: Invalid attachment.";
            return result;
        }

        result.Data = entity;
        result.Success = true;
        return result;
    }

    public static AttachmentType GetAttachmentType(IAttachment att)
    {
        if (EntityDetection.IsSizePlausible(att.Size))
            return AttachmentType.PKM;
        if (MysteryGift.IsMysteryGift(att.Size))
            return AttachmentType.Wondercard;

        return AttachmentType.Invalid;
    }
}

public sealed class Download<T> where T : class
{
    public bool Success;
    public T? Data;
    public string? SanitizedFileName;
    public string? ErrorMessage;
}

public enum AttachmentType
{
    Invalid = 0,
    PKM = 1,
    Wondercard = 2,
}
