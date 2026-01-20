using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Media.Imaging;

namespace FuckNcm.Models;

/// <summary>
/// 音乐文件信息模型
/// </summary>
public class AudioFileInfo
{
    public Track TrackInfo { get; set; }

    /// <summary>
    /// 歌曲封面路径
    /// </summary>
    public Bitmap Cover { get; set; }

    /// <summary>
    /// 歌曲标题
    /// </summary>
    public string Title
    {
        get => TrackInfo?.Title ?? string.Empty;
        set
        {
            Task.Run(async () =>
            {
                TrackInfo.Title = value;
                await TrackInfo.SaveAsync();
            });
        }
    }

    /// <summary>
    /// 歌曲作者
    /// </summary>
    public string Artist
    {
        get => TrackInfo?.Artist ?? string.Empty;
        set
        {
            Task.Run(async () =>
            {
                TrackInfo.Artist = value;
                await TrackInfo.SaveAsync();
            });
        }
    }

    /// <summary>
    /// 歌曲专辑
    /// </summary>
    public string Album
    {
        get => TrackInfo?.Album ?? string.Empty;
        set
        {
            Task.Run(async () =>
            {
                TrackInfo.Album = value;
                await TrackInfo.SaveAsync();
            });
        }
    }

    public string FilePath { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FileDirPath => Path.GetDirectoryName(FilePath);

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Tag版本
    /// </summary>
    public string TagVersion => TrackInfo?.SupportedMetadataFormats.FirstOrDefault(n => n.Name.Contains("ID3"))?.Name
                                ?? "Unknown";

    /// <summary>
    /// 编码器
    /// </summary>
    public string Encoder => TrackInfo?.AudioFormat.MimeList.FirstOrDefault() ?? string.Empty;

    /// <summary>
    /// 比特率 (kbps)
    /// </summary>
    public int BitRate => TrackInfo?.Bitrate ?? -1;

    /// <summary>
    /// 采样率 (Hz)
    /// </summary>
    public double SampleRate => TrackInfo?.SampleRate ?? -1;

    /// <summary>
    /// 时长
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMilliseconds(TrackInfo?.DurationMs ?? 0);

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime => File.GetLastWriteTime(FileDirPath);

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// 是否已处理
    /// </summary>
    public ItemStatus Status { get; set; }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string DumpStatus => Status switch
                                {
                                    ItemStatus.UNPARSED => "待处理",
                                    ItemStatus.PARSING  => "处理中",
                                    ItemStatus.PARSED   => "已完成",
                                    ItemStatus.ERROR    => "错误",
                                    ItemStatus.MOVED    => "已移动",
                                    ItemStatus.SKIPPED  => "已跳过",
                                    _                   => throw new ArgumentOutOfRangeException()
                                };


    /// <summary>
    /// 格式化的时长字符串
    /// </summary>
    public string DurationFormatted => Duration.ToString(@"mm\:ss");

    /// <summary>
    /// 格式化的比特率字符串
    /// </summary>
    public string BitRateFormatted => $"{BitRate} kbps";

    /// <summary>
    /// 格式化的采样率字符串
    /// </summary>
    public string SampleRateFormatted => $"{SampleRate} Hz";
}