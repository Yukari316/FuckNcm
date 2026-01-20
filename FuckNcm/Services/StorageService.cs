using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FuckNcm.Models;
using FuckNcm.Ncm;
using FuckNcm.Services.Interfaces;
using Newtonsoft.Json;
using SukiUI.Controls;
using SukiUI.MessageBox;

namespace FuckNcm.Services;

public class StorageService : IStorageService
{
    // 数据集合
    public  ObservableCollection<AudioFileInfo>         SourceAudioFiles      { get; } = [];
    public  Config                                      Config                { get; private set; }
    private ConcurrentDictionary<string, int>           SourceAudioFilesIndex { get; } = new();
    private ConcurrentDictionary<string, AudioFileInfo> TargetAudioFiles      { get; } = new();

    private readonly string   _configPath          = Path.Combine(AppContext.BaseDirectory, "config.json");
    private readonly string[] _supportedExtensions = [".ncm", ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg"];

#region AudioFile

    public void LoadAudioFiles(string path, bool fromSource, SearchOption searchOption = SearchOption.AllDirectories)
    {
        //禁用UI交互
        Dispatcher.UIThread.Post(() => { Utils.MainWindow.SetButtonEnable(false); });
        if (fromSource)
        {
            SourceAudioFilesIndex.Clear();
            Dispatcher.UIThread.Post(() =>
            {
                SourceAudioFiles.Clear();
                Utils.MainWindow.TotalFilesCountTextBlock.Text = "0";
            });
        }
        else
        {
            TargetAudioFiles.Clear();
        }

        if (!Directory.Exists(path))
            return;
        List<Exception> loadErrors = new();
        List<string> audioFiles = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                                           .Where(file => _supportedExtensions.Contains(
                                                      Path.GetExtension(file).ToLower()))
                                           .ToList();
        int  fileCount = 0;
        bool isLoading = true;
        //UI刷新线程
        Task.Run(async () =>
        {
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (isLoading)
            {
                await Task.Delay(500);
                Dispatcher.UIThread.Post(() =>
                {
                    Utils.MainWindow.UpdateStatus("加载中...",
                                                  $"{fileCount}/{audioFiles.Count}",
                                                  (int)(fileCount / (audioFiles.Count * 1.0) * 100));
                });
            }

            Dispatcher.UIThread.Post(() =>
            {
                Utils.MainWindow.UpdateStatus("加载完成");
                Utils.MainWindow.TotalFilesCountTextBlock.Text = audioFiles.Count.ToString();
            });
        });
        foreach (string file in audioFiles)
            try
            {
                AudioFileInfo audioFileInfo = LoadAudioFile(file);
                //既不是音乐也不是ncm文件
                if (audioFileInfo is null) continue;
                if (fromSource)
                {
                    SourceAudioFilesIndex.TryAdd(file, fileCount);
                    Dispatcher.UIThread.Post(() => { SourceAudioFiles.Add(audioFileInfo); });
                }
                else
                {
                    TargetAudioFiles.TryAdd(audioFileInfo.FilePath, audioFileInfo);
                }

                fileCount++;
            }
            catch (Exception e)
            {
                loadErrors.Add(e);
            }

        isLoading = false;
        GC.Collect();
        //恢复UI交互
        Dispatcher.UIThread.Post(() => { Utils.MainWindow.SetButtonEnable(true); });
        if (loadErrors.Count > 0)
            Dispatcher.UIThread.Post(async void () =>
            {
                await SukiMessageBox.ShowDialog(new SukiMessageBoxHost
                {
                    IconPreset = SukiMessageBoxIcons.Error,
                    Header = $"加载部分文件时发生错误[错误数量:{loadErrors.Count}]",
                    Content = string.Join("\n------------------------\n", loadErrors.Select(ex => ex.Message)),
                    ActionButtonsPreset = SukiMessageBoxButtons.Close
                });
            });
    }

    public async void ParseNcmFiles(string outputDir)
    {
        //禁用UI交互
        Dispatcher.UIThread.Post(() => { Utils.MainWindow.SetButtonEnable(false); });
        int          successCount = 0;
        int          failCount    = 0;
        List<string> failMessages = new();
        bool         isParsing    = true;
        string       parsingFile  = "";
        //UI刷新线程
        _ = Task.Run(async () =>
        {
            while (isParsing)
            {
                await Task.Delay(500);
                Dispatcher.UIThread.Post(() =>
                {
                    Utils.MainWindow.UpdateStatus($"正在处理文件{parsingFile}...",
                                                  $"{successCount + failCount}/{SourceAudioFiles.Count}",
                                                  (int)((successCount + failCount)
                                                        / (SourceAudioFiles.Count * 1.0)
                                                        * 100));
                });
            }

            Dispatcher.UIThread.Post(() => Utils.MainWindow.UpdateStatus("加载完成"));
        });
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < SourceAudioFiles.Count; i++)
        {
            AudioFileInfo audioFileInfo = SourceAudioFiles[i];
            parsingFile = audioFileInfo.FileName;
            UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.PARSING);
            if (!string.IsNullOrEmpty(audioFileInfo.FilePath) && Path.GetExtension(audioFileInfo.FilePath) != ".ncm")
            {
                //移动非ncm文件到输出目录
                string moveTargetPath = $@"{outputDir}\{audioFileInfo.FileName}";
                bool   isMoved        = false;
                if (!File.Exists(moveTargetPath))
                {
                    File.Move(audioFileInfo.FilePath, moveTargetPath);
                    isMoved = true;
                }
                else
                {
                    FileInfo existedFileInfo = new(moveTargetPath);
                    FileInfo sourceFileInfo  = new(audioFileInfo.FilePath);
                    if (sourceFileInfo.Length > existedFileInfo.Length)
                    {
                        File.Delete(moveTargetPath);
                        File.Move(audioFileInfo.FilePath, moveTargetPath);
                        isMoved = true;
                    }
                }

                if (isMoved)
                    UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.MOVED, new Track(moveTargetPath));
                else
                    UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.SKIPPED);
                successCount++;
                continue;
            }

            try
            {
                (NcmParseStatus status, Track track) =
                    await NcmDump.DumpNcmFile(audioFileInfo.FilePath, outputDir);
                switch (status)
                {
                    case NcmParseStatus.OK:
                        successCount++;
                        UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.PARSED, track);
                        break;
                    case NcmParseStatus.EXISTS:
                        successCount++;
                        UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.SKIPPED, track);
                        break;
                    case NcmParseStatus.NOT_NCM_FILE:
                        failCount++;
                        failMessages.Add($"文件:{audioFileInfo.FileName} 失败原因:不是NCM文件");
                        UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.ERROR);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                failMessages.Add($"文件:{audioFileInfo.FileName} 失败原因:{e.Message}");
                UpdateAudioFileItem(audioFileInfo.FilePath, ItemStatus.ERROR);
                failCount++;
            }
        }

        GC.Collect();
        Dispatcher.UIThread.Post(() => { Utils.MainWindow.SetButtonEnable(true); });
        if (failMessages.Count > 0)
            Dispatcher.UIThread.Post(async void () =>
            {
                await SukiMessageBox.ShowDialog(new SukiMessageBoxHost
                {
                    IconPreset          = SukiMessageBoxIcons.Error,
                    Header              = $"处理部分文件时发生错误[错误数量:{failMessages.Count}]",
                    Content             = string.Join("\n------------------------\n", failMessages.Select(ex => ex)),
                    ActionButtonsPreset = SukiMessageBoxButtons.Close
                });
            });
        // ReSharper disable once RedundantAssignment
        isParsing = false;
    }

    private void UpdateAudioFileItem(string path, ItemStatus status, Track tagInfo = null)
    {
        if (!SourceAudioFilesIndex.TryGetValue(path, out int index)) return;
        if (tagInfo is null)
        {
            AudioFileInfo oldStatus = SourceAudioFiles[index];
            AudioFileInfo newStatus = new()
            {
                FilePath  = path,
                TrackInfo = oldStatus.TrackInfo,
                Status    = status
            };
            Dispatcher.UIThread.Post(() => SourceAudioFiles[index] = newStatus);
            return;
        }

        //new file replace
        AudioFileInfo newFileInfo = new()
        {
            TrackInfo = tagInfo,
            Cover     = LoadCoverImage(tagInfo),
            FilePath  = tagInfo.Path,
            Status    = status
        };
        Dispatcher.UIThread.Post(() => SourceAudioFiles[index] = newFileInfo);
        SourceAudioFilesIndex.TryRemove(path, out _);
        SourceAudioFilesIndex.TryAdd(newFileInfo.FilePath, index);
    }

    private static AudioFileInfo LoadAudioFile(string path)
    {
        if (Path.GetExtension(path).ToLower() == ".ncm")
            return new AudioFileInfo
            {
                FilePath = path,
                Status   = ItemStatus.UNPARSED
            };

        Track musicTag = new(path);
        if (musicTag.AudioFormat.Name == "Unknown") return null;
        Console.WriteLine($"Loaded {musicTag.Title}.");
        return new AudioFileInfo
        {
            TrackInfo     = musicTag,
            Cover         = LoadCoverImage(musicTag),
            FilePath      = path,
            FileSizeBytes = new FileInfo(path).Length,
            Status        = ItemStatus.UNPARSED
        };
    }

    private static Bitmap LoadCoverImage(Track track)
    {
        if (track.EmbeddedPictures.Count == 0)
            return null;

        using MemoryStream ms = new(track.EmbeddedPictures[0].PictureData);
        return new Bitmap(ms);
    }

#endregion

#region ConfigFile

    public void InitConfig()
    {
        if (File.Exists(_configPath))
        {
            string configJson = File.ReadAllText(_configPath);
            Config = JsonConvert.DeserializeObject<Config>(configJson);
        }
        else
        {
            string defaultConfig = Encoding.UTF8.GetString(Utils.GetEmbeddedResourceStream("Resources.config.json"));
            File.WriteAllText(_configPath, defaultConfig);
            Config = JsonConvert.DeserializeObject<Config>(defaultConfig);
        }
    }

    public void SaveConfig()
    {
        string configJson = JsonConvert.SerializeObject(Config, Formatting.Indented);
        File.WriteAllText(_configPath, configJson);
    }

#endregion
}