namespace Solamirare;

/// <summary>
/// 常用 MIME 类型枚举
/// </summary>
public enum HttpMimeTypes : uint
{
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown = 0,

    // Text
    /// <summary>
    /// text/plain - 纯文本
    /// </summary>
    TextPlain = 1,
    /// <summary>
    /// text/html - HTML 文档
    /// </summary>
    TextHtml = 2,
    /// <summary>
    /// text/css - CSS 样式表
    /// </summary>
    TextCss = 3,
    /// <summary>
    /// text/javascript - JavaScript 脚本
    /// </summary>
    TextJavascript = 4,
    /// <summary>
    /// text/xml - XML 数据
    /// </summary>
    TextXml = 5,
    /// <summary>
    /// text/csv - 逗号分隔值
    /// </summary>
    TextCsv = 6,
    /// <summary>
    /// text/markdown - Markdown 文档
    /// </summary>
    TextMarkdown = 7,
    /// <summary>
    /// text/calendar - iCalendar 数据
    /// </summary>
    TextCalendar = 8,
    /// <summary>
    /// text/yaml - YAML 数据
    /// </summary>
    TextYaml = 9,

    // Application
    /// <summary>
    /// application/json - JSON 数据
    /// </summary>
    ApplicationJson = 100,
    /// <summary>
    /// application/xml - XML 应用数据
    /// </summary>
    ApplicationXml = 101,
    /// <summary>
    /// application/octet-stream - 二进制流
    /// </summary>
    ApplicationOctetStream = 102,
    /// <summary>
    /// application/pdf - PDF 文档
    /// </summary>
    ApplicationPdf = 103,
    /// <summary>
    /// application/zip - ZIP 压缩包
    /// </summary>
    ApplicationZip = 104,
    /// <summary>
    /// application/gzip - Gzip 压缩数据
    /// </summary>
    ApplicationGzip = 105,
    /// <summary>
    /// application/x-www-form-urlencoded - 表单数据
    /// </summary>
    ApplicationXWwwFormUrlencoded = 106,
    /// <summary>
    /// application/wasm - WebAssembly
    /// </summary>
    ApplicationWasm = 107,
    /// <summary>
    /// application/xhtml+xml - XHTML 文档
    /// </summary>
    ApplicationXhtml = 108,
    /// <summary>
    /// application/ld+json - JSON-LD 数据
    /// </summary>
    ApplicationLdJson = 109,
    /// <summary>
    /// application/rtf - 富文本格式
    /// </summary>
    ApplicationRtf = 110,
    /// <summary>
    /// application/x-tar - Tar 归档
    /// </summary>
    ApplicationTar = 111,
    /// <summary>
    /// application/vnd.rar - RAR 压缩包
    /// </summary>
    ApplicationRar = 112,
    /// <summary>
    /// application/x-7z-compressed - 7-zip 压缩包
    /// </summary>
    Application7z = 113,
    /// <summary>
    /// application/x-bzip2 - Bzip2 压缩数据
    /// </summary>
    ApplicationBzip2 = 114,
    /// <summary>
    /// application/epub+zip - 电子出版物
    /// </summary>
    ApplicationEpub = 115,
    /// <summary>
    /// application/java-archive - Java 归档 (JAR)
    /// </summary>
    ApplicationJar = 116,

    // Office - Microsoft
    /// <summary>
    /// application/msword - Word 文档
    /// </summary>
    ApplicationDoc = 150,
    /// <summary>
    /// application/vnd.openxmlformats-officedocument.wordprocessingml.document - Word (OpenXML)
    /// </summary>
    ApplicationDocx = 151,
    /// <summary>
    /// application/vnd.ms-excel - Excel 表格
    /// </summary>
    ApplicationXls = 152,
    /// <summary>
    /// application/vnd.openxmlformats-officedocument.spreadsheetml.sheet - Excel (OpenXML)
    /// </summary>
    ApplicationXlsx = 153,
    /// <summary>
    /// application/vnd.ms-powerpoint - PowerPoint 演示文稿
    /// </summary>
    ApplicationPpt = 154,
    /// <summary>
    /// application/vnd.openxmlformats-officedocument.presentationml.presentation - PowerPoint (OpenXML)
    /// </summary>
    ApplicationPptx = 155,

    // Office - OpenDocument
    /// <summary>
    /// application/vnd.oasis.opendocument.text - OpenDocument 文本
    /// </summary>
    ApplicationOdt = 160,
    /// <summary>
    /// application/vnd.oasis.opendocument.spreadsheet - OpenDocument 表格
    /// </summary>
    ApplicationOds = 161,
    /// <summary>
    /// application/vnd.oasis.opendocument.presentation - OpenDocument 演示文稿
    /// </summary>
    ApplicationOdp = 162,

    // Image
    /// <summary>
    /// image/jpeg - JPEG 图片
    /// </summary>
    ImageJpeg = 200,
    /// <summary>
    /// image/png - PNG 图片
    /// </summary>
    ImagePng = 201,
    /// <summary>
    /// image/gif - GIF 图片
    /// </summary>
    ImageGif = 202,
    /// <summary>
    /// image/svg+xml - SVG 矢量图
    /// </summary>
    ImageSvgXml = 203,
    /// <summary>
    /// image/webp - WebP 图片
    /// </summary>
    ImageWebp = 204,
    /// <summary>
    /// image/x-icon - 图标文件
    /// </summary>
    ImageIcon = 205,
    /// <summary>
    /// image/bmp - BMP 图片
    /// </summary>
    ImageBmp = 206,
    /// <summary>
    /// image/tiff - TIFF 图片
    /// </summary>
    ImageTiff = 207,
    /// <summary>
    /// image/avif - AVIF 图片
    /// </summary>
    ImageAvif = 208,

    // Audio & Video
    /// <summary>
    /// audio/mpeg - MP3 音频
    /// </summary>
    AudioMpeg = 300,
    /// <summary>
    /// audio/ogg - OGG 音频
    /// </summary>
    AudioOgg = 301,
    /// <summary>
    /// video/mp4 - MP4 视频
    /// </summary>
    VideoMp4 = 302,
    /// <summary>
    /// video/webm - WebM 视频
    /// </summary>
    VideoWebm = 303,
    /// <summary>
    /// audio/aac - AAC 音频
    /// </summary>
    AudioAac = 304,
    /// <summary>
    /// audio/midi - MIDI 音频
    /// </summary>
    AudioMidi = 305,
    /// <summary>
    /// audio/wav - WAV 音频
    /// </summary>
    AudioWav = 306,
    /// <summary>
    /// audio/webm - WEBM 音频
    /// </summary>
    AudioWeba = 307,
    /// <summary>
    /// video/x-msvideo - AVI 视频
    /// </summary>
    VideoAvi = 308,
    /// <summary>
    /// video/mpeg - MPEG 视频
    /// </summary>
    VideoMpeg = 309,
    /// <summary>
    /// video/ogg - OGG 视频
    /// </summary>
    VideoOgv = 310,
    /// <summary>
    /// video/mp2t - MPEG 传输流
    /// </summary>
    VideoTs = 311,
    /// <summary>
    /// video/3gpp - 3GPP 容器
    /// </summary>
    Video3gp = 312,
    /// <summary>
    /// video/3gpp2 - 3GPP2 容器
    /// </summary>
    Video3g2 = 313,

    // Multipart
    /// <summary>
    /// multipart/form-data - 多部分表单数据
    /// </summary>
    MultipartFormData = 400,

    // Font
    /// <summary>
    /// font/woff - WOFF 字体
    /// </summary>
    FontWoff = 500,
    /// <summary>
    /// font/woff2 - WOFF2 字体
    /// </summary>
    FontWoff2 = 501,
    /// <summary>
    /// font/ttf - TTF 字体
    /// </summary>
    FontTtf = 502,
    /// <summary>
    /// font/otf - OTF 字体
    /// </summary>
    FontOtf = 503
}