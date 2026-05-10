
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Solamirare;



/// <summary>
/// 服务器变量集合
/// </summary>
public static unsafe class ServerVariables
{

    internal static ValueDictionary<HttpMimeTypes, UnManagedMemory<byte>> Mimetypes;

    static ServerVariables()
    {
        initMimetypes();
    }

    static void initMimetypes()
    {
        Mimetypes = new ValueDictionary<HttpMimeTypes, UnManagedMemory<byte>>(64);

        static void AddMime(HttpMimeTypes type, ReadOnlySpan<byte> value)
        {
            var mem = new UnManagedMemory<byte>((uint)value.Length, (uint)value.Length);
            value.CopyTo(mem.AsSpan());
            Mimetypes.AddOrUpdate(type, mem);
        }

        AddMime(HttpMimeTypes.TextPlain, "text/plain; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextHtml, "text/html; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextCss, "text/css; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextJavascript, "text/javascript; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextXml, "text/xml; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextCsv, "text/csv; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextMarkdown, "text/markdown; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextCalendar, "text/calendar; charset=utf-8"u8);
        AddMime(HttpMimeTypes.TextYaml, "text/yaml; charset=utf-8"u8);

        AddMime(HttpMimeTypes.ApplicationJson, "application/json"u8);
        AddMime(HttpMimeTypes.ApplicationXml, "application/xml"u8);
        AddMime(HttpMimeTypes.ApplicationOctetStream, "application/octet-stream"u8);
        AddMime(HttpMimeTypes.ApplicationPdf, "application/pdf"u8);
        AddMime(HttpMimeTypes.ApplicationZip, "application/zip"u8);
        AddMime(HttpMimeTypes.ApplicationGzip, "application/gzip"u8);
        AddMime(HttpMimeTypes.ApplicationXWwwFormUrlencoded, "application/x-www-form-urlencoded"u8);
        AddMime(HttpMimeTypes.ApplicationWasm, "application/wasm"u8);
        AddMime(HttpMimeTypes.ApplicationXhtml, "application/xhtml+xml"u8);
        AddMime(HttpMimeTypes.ApplicationLdJson, "application/ld+json"u8);
        AddMime(HttpMimeTypes.ApplicationRtf, "application/rtf"u8);
        AddMime(HttpMimeTypes.ApplicationTar, "application/x-tar"u8);
        AddMime(HttpMimeTypes.ApplicationRar, "application/vnd.rar"u8);
        AddMime(HttpMimeTypes.Application7z, "application/x-7z-compressed"u8);
        AddMime(HttpMimeTypes.ApplicationBzip2, "application/x-bzip2"u8);
        AddMime(HttpMimeTypes.ApplicationEpub, "application/epub+zip"u8);
        AddMime(HttpMimeTypes.ApplicationJar, "application/java-archive"u8);

        AddMime(HttpMimeTypes.ApplicationDoc, "application/msword"u8);
        AddMime(HttpMimeTypes.ApplicationDocx, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"u8);
        AddMime(HttpMimeTypes.ApplicationXls, "application/vnd.ms-excel"u8);
        AddMime(HttpMimeTypes.ApplicationXlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"u8);
        AddMime(HttpMimeTypes.ApplicationPpt, "application/vnd.ms-powerpoint"u8);
        AddMime(HttpMimeTypes.ApplicationPptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation"u8);

        AddMime(HttpMimeTypes.ApplicationOdt, "application/vnd.oasis.opendocument.text"u8);
        AddMime(HttpMimeTypes.ApplicationOds, "application/vnd.oasis.opendocument.spreadsheet"u8);
        AddMime(HttpMimeTypes.ApplicationOdp, "application/vnd.oasis.opendocument.presentation"u8);

        AddMime(HttpMimeTypes.ImageJpeg, "image/jpeg"u8);
        AddMime(HttpMimeTypes.ImagePng, "image/png"u8);
        AddMime(HttpMimeTypes.ImageGif, "image/gif"u8);
        AddMime(HttpMimeTypes.ImageSvgXml, "image/svg+xml"u8);
        AddMime(HttpMimeTypes.ImageWebp, "image/webp"u8);
        AddMime(HttpMimeTypes.ImageIcon, "image/x-icon"u8);
        AddMime(HttpMimeTypes.ImageBmp, "image/bmp"u8);
        AddMime(HttpMimeTypes.ImageTiff, "image/tiff"u8);
        AddMime(HttpMimeTypes.ImageAvif, "image/avif"u8);

        AddMime(HttpMimeTypes.AudioMpeg, "audio/mpeg"u8);
        AddMime(HttpMimeTypes.AudioOgg, "audio/ogg"u8);
        AddMime(HttpMimeTypes.VideoMp4, "video/mp4"u8);
        AddMime(HttpMimeTypes.VideoWebm, "video/webm"u8);
        AddMime(HttpMimeTypes.AudioAac, "audio/aac"u8);
        AddMime(HttpMimeTypes.AudioMidi, "audio/midi"u8);
        AddMime(HttpMimeTypes.AudioWav, "audio/wav"u8);
        AddMime(HttpMimeTypes.AudioWeba, "audio/webm"u8);
        AddMime(HttpMimeTypes.VideoAvi, "video/x-msvideo"u8);
        AddMime(HttpMimeTypes.VideoMpeg, "video/mpeg"u8);
        AddMime(HttpMimeTypes.VideoOgv, "video/ogg"u8);
        AddMime(HttpMimeTypes.VideoTs, "video/mp2t"u8);
        AddMime(HttpMimeTypes.Video3gp, "video/3gpp"u8);
        AddMime(HttpMimeTypes.Video3g2, "video/3gpp2"u8);

        AddMime(HttpMimeTypes.MultipartFormData, "multipart/form-data"u8);

        AddMime(HttpMimeTypes.FontWoff, "font/woff"u8);
        AddMime(HttpMimeTypes.FontWoff2, "font/woff2"u8);
        AddMime(HttpMimeTypes.FontTtf, "font/ttf"u8);
        AddMime(HttpMimeTypes.FontOtf, "font/otf"u8);
    }

}
