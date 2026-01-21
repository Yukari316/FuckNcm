using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using FuckNcm.Models;
using Newtonsoft.Json.Linq;

namespace FuckNcm.Ncm;

public class NcmDump
{
    // NCM 文件结构偏移量
    private const int    HEADER_SKIP_BYTES = 2;     // 跳过的头部字节
    private const int    CRC32_SKIP_BYTES  = 5;     // CRC32 后跳过的字节
    private const string DEFAULT_FORMAT    = "mp3"; // 默认输出格式

    public static async Task<(NcmParseStatus status, Track tagInfo)> DumpNcmFile(
        string path,
        string outPath,
        bool   saveFirstArtist = false)
    {
        DateTime startTime = DateTime.Now;
        await using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);

        //检查文件头
        if (!fileStream.CheckHeader())
            return (NcmParseStatus.NOT_NCM_FILE, null);

        string fileName = Path.GetFileNameWithoutExtension(path);

        Console.WriteLine($"[NCM] 解析文件: {fileName}");

        fileStream.Seek(HEADER_SKIP_BYTES, SeekOrigin.Current);

        //keybox
        ReadOnlySpan<byte> box = fileStream.MakeKeyBox();
        Console.WriteLine($"[NCM] 获取KeyBox [{box.Length} bytes]");

        //metainfo
        NetEaseMetaInfo meta = fileStream.ReadMeta();
        Console.WriteLine($"[NCM] 获取元数据 id:[{meta.MusicId}]");
        //ext name
        string ext = string.IsNullOrEmpty(meta.Format)
            ? DEFAULT_FORMAT
            : meta.Format;

        //why netease?
        List<string> artists = meta.Artist
                                   .SelectMany(jArray => jArray)
                                   .Where((_, index) => index % 2 == 0)
                                   .Select(jToken => jToken.Value<string>())
                                   .ToList();

        string title          = meta.MusicName;
        string artist         = saveFirstArtist ? artists.FirstOrDefault() : string.Join(',', artists);
        string outputFileName = Utils.SanitizeFileName($"{artist} - {title}.{ext}");
        string outputFilePath = Path.Combine(outPath.Trim().Trim('\\'), outputFileName);

        //file exists
        if (File.Exists(outputFilePath))
        {
            Console.WriteLine("[NCM] 文件已存在，跳过此文件");
            return (NcmParseStatus.EXISTS, new Track(outputFilePath));
        }

        //crc32
        Span<byte> crc32 = stackalloc byte[4];
        fileStream.ReadExactly(crc32);
        string crc32hash = $"0x{Convert.ToHexString(crc32)}";
        Console.WriteLine($"[NCM] CRC32: {crc32hash}");

        //skip gap bytes
        fileStream.Seek(CRC32_SKIP_BYTES, SeekOrigin.Current);

        //cover
        uint imageLen = fileStream.ReadUint32();
        Console.WriteLine($"[NCM] 封面大小: [{imageLen} bytes]");

        //cover
        byte[] image = new byte[imageLen];
        fileStream.ReadExactly(image);

        //audio
        ReadOnlyMemory<byte> audio = fileStream.ReadAudio(box);
        Console.WriteLine($"[NCM] 音频大小: {audio.Length} bytes");

        //保存文件
        Console.WriteLine("[NCM] 保存音频文件...");
        await File.WriteAllBytesAsync(outputFilePath, audio);
        TimeSpan decryptTime = DateTime.Now - startTime;
        Console.WriteLine($"[NCM] 音频文件保存完成，耗时: {(int)decryptTime.TotalMilliseconds} ms");

        //写入tag
        Track track = new(outputFilePath)
        {
            Title  = title,
            Artist = artist,
            Album  = meta.Album
        };
        track.AdditionalFields.Add("Subtitle", string.Join(";", meta.Alias));

        //没有内嵌封面
        if (image.Length == 0)
        {
            Console.WriteLine("[NCM] 没有内嵌封面,尝试从网络获取封面...");
            (bool success, byte[] data) =
                await Utils.DownloadAlbumCoverAsync(meta.AlbumPic);
            image = success ? data : [];
        }

        if (image.Length != 0)
        {
            Console.WriteLine("[NCM] 添加内嵌封面...");
            PictureInfo cover = PictureInfo.fromBinaryData(image, PictureInfo.PIC_TYPE.CD);
            track.EmbeddedPictures.Add(cover);
        }

        Console.WriteLine("[NCM] 保存标签信息...");
        await track.SaveAsync();
        TimeSpan tagSaveTime = DateTime.Now - startTime + decryptTime;
        Console.WriteLine($"[NCM] 标签信息保存完成，耗时: {(int)tagSaveTime.TotalMilliseconds} ms");

        return (NcmParseStatus.OK, track);
    }
}