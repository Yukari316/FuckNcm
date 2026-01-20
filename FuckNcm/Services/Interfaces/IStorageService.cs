using System.Collections.ObjectModel;
using System.IO;
using FuckNcm.Models;

namespace FuckNcm.Services.Interfaces;

public interface IStorageService
{
    public ObservableCollection<AudioFileInfo> SourceAudioFiles { get; }

    public Config Config { get; }

    public void LoadAudioFiles(string path, bool fromSource, SearchOption searchOption = SearchOption.AllDirectories);

    public void ParseNcmFiles(string outputDir);

    public void InitConfig();

    public void SaveConfig();
}