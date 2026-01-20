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
    public static async Task<(NcmParseStatus status, Track tagInfo)> DumpNcmFile(
        string path,
        string outPath,
        bool   saveFirstArtist = false)
    {
        await using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);

        //检查文件头
        if (!fileStream.CheckHeader())
            return (NcmParseStatus.NOT_NCM_FILE, null);

        string fileName = Path.GetFileNameWithoutExtension(path);

        Console.WriteLine($"parsing file[{fileName}]...");

        fileStream.Seek(2, SeekOrigin.Current);

        //keybox
        byte[] box = fileStream.MakeKeyBox();
        Console.WriteLine($"get key box({box.Length})");

        //metainfo
        NetEaseMetaInfo meta = fileStream.ReadMeta();
        Console.WriteLine("get metainfo");
        //ext name
        string ext = string.IsNullOrEmpty(meta.Format)
            ? "mp3"
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
        string outputFilePath = $@"{outPath.Trim()}\{outputFileName}";

        //file exists
        if (File.Exists(outputFilePath))
            return (NcmParseStatus.EXISTS, new Track(outputFilePath));

        //crc32
        byte[] crc32bytes = new byte[4];
        fileStream.ReadExactly(crc32bytes);
        string crc32hash = $"0x{BitConverter.ToString(crc32bytes).Replace("-", string.Empty)}";
        Console.WriteLine($"get crc32({crc32hash})");

        //skip 5 character
        fileStream.Seek(5, SeekOrigin.Current);

        //cover
        uint imageLen = fileStream.ReadUint32();
        Console.WriteLine($"get cover image({imageLen})");

        //cover
        byte[] imageBytes = new byte[imageLen];
        fileStream.ReadExactly(imageBytes);

        //audio
        byte[] audioBytes = fileStream.ReadAudio(box);
        Console.WriteLine($"get audio({audioBytes.Length})");

        //保存文件
        Console.WriteLine("saving music...");
        string outFilePath = $@"{outPath.Trim('\\')}\{outputFileName}";
        await File.WriteAllBytesAsync(outFilePath, audioBytes);

        //写入tag
        Track track = new(outFilePath)
        {
            Title  = title,
            Artist = artist,
            Album  = meta.Album
        };
        track.AdditionalFields.Add("Subtitle", string.Join(";", meta.Alias));

        //cover
        if (imageBytes.Length == 0)
        {
            (bool success, byte[] data) =
                await Utils.DownloadAlbumCoverAsync(meta.AlbumPic);

            imageBytes = success ? data : [];
        }

        if (imageBytes.Length != 0)
        {
            PictureInfo cover = PictureInfo.fromBinaryData(imageBytes, PictureInfo.PIC_TYPE.CD);
            track.EmbeddedPictures.Add(cover);
        }

        Console.WriteLine("saving music tags...");
        await track.SaveAsync();

        return (NcmParseStatus.OK, track);
    }
}