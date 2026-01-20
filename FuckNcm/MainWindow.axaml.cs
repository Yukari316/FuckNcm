using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.MessageBox;
using Microsoft.Extensions.DependencyInjection;

namespace FuckNcm;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // 在容器中注册窗口
        Utils.ServiceCollection.AddSingleton(this);
        // 初始化背景服务并订阅事件
        Utils.ThemeBackgroundService.PropertyChanged += OnBackgroundServicePropertyChanged;
        // 初始化必要组件
        MusicDataGrid.ItemsSource = Utils.StorageService.SourceAudioFiles;
        DialogHost.Manager        = Utils.ServiceProvider.GetService<ISukiDialogManager>()!;

        AutoResetEvent loadedEvent = new(false);
        Loaded += (_, _) => loadedEvent.Set();
        // 等待窗口加载完成后再执行初始化操作
        Task.Run(() =>
        {
            loadedEvent.WaitOne();
            Dispatcher.UIThread.Post(() =>
            {
                Utils.ThemeBackgroundService.UpdateBackgroundColor();
                TargetPathTextBox.Text = Utils.StorageService.Config.LastTargetDir;
            });
        });
    }

    /// <summary>
    /// 在主题背景服务属性变化时调用
    /// </summary>
    private void OnBackgroundServicePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Utils.ThemeBackgroundService.CardBackground)) return;
        SolidColorBrush background = Utils.ThemeBackgroundService.CardBackground;
        ConfigBorder.Background  = background;
        ContentBorder.Background = background;
        StatusBorder.Background  = background;
    }

    /// <summary>
    /// 设置背景透明度 (0.0 - 1.0)
    /// </summary>
    public void SetBackgroundOpacity(double opacity)
    {
        Utils.ThemeBackgroundService.SetOpacity(opacity);
    }

    public void UpdateStatus(string statusText, string progressText = "", int progressValue = 0)
    {
        // 防止在初始化期间调用时出现空引用
        if (StatusTextBlock == null
            || ProgressTextBlock == null
            || SyncProgressBar == null) return;

        // 更新状态栏
        StatusTextBlock.Text      = statusText;
        ProgressTextBlock.Text    = progressText;
        SyncProgressBar.Value     = progressValue;
        SyncProgressBar.IsVisible = !string.IsNullOrEmpty(progressText);
    }

#region 菜单事件处理

    private async void OnStartSyncClick(object sender, RoutedEventArgs e)
    {
        // TODO: 实现实际的同步逻辑,添加必要的检查
        if (Utils.StorageService.SourceAudioFiles.Count == 0
            || string.IsNullOrEmpty(TargetPathTextBox.Text)
            || !Directory.Exists(TargetPathTextBox.Text))
        {
            await SukiMessageBox.ShowDialog(new SukiMessageBoxHost
            {
                IconPreset          = SukiMessageBoxIcons.Warning,
                Header              = "Warning",
                Content             = "请确保已选择有效的源目录和目标目录，且源目录中包含有效的音乐文件。",
                ActionButtonsPreset = SukiMessageBoxButtons.Close
            });
            return;
        }

        string targetDir = TargetPathTextBox.Text;
        _ = Task.Run(() =>
        {
            Utils.StorageService.ParseNcmFiles(targetDir);
            return Task.CompletedTask;
        });
    }

#endregion

#region 主题切换

    private void OnToggleThemeClick(object sender, RoutedEventArgs e)
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
    }

    private async void OnSetBackgroundImageClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "选择背景图片",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("图片文件") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }
            ]
        });

        if (files.Count <= 0) return;
        string path = files[0].Path.LocalPath;

        Utils.ThemeBackgroundService.SetBackgroundImage(path);
    }

    private void OnSetDefaultBackgroundImage(object sender, RoutedEventArgs e)
    {
        Utils.ThemeBackgroundService.SetDefaultBackgroundImage();
    }

#endregion

#region 帮助菜单

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        // 简单的消息提示
        DialogHost.Manager.CreateDialog()
                  .WithTitle("关于 FuckNcm")
                  .WithContent($"神秘NCM导歌小工具\n作者:Yukari316\nVersion:{Assembly.GetExecutingAssembly().GetName().Version}")
                  .WithActionButton("Close", _ => { }, true)
                  .TryShow();
    }

#endregion

#region 目录选择

    private async void OnBrowseSourceClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title         = "选择源目录",
            AllowMultiple = false
        });
        //TODO load files
        if (folders.Count <= 0) return;
        SourcePathTextBox.Text = folders[0].Path.LocalPath;

        Utils.StorageService.Config.LastSourceDir = SourcePathTextBox.Text;
        Utils.StorageService.SaveConfig();

        OpenSourcePathButton.IsEnabled = false;
        string sourceDir = SourcePathTextBox.Text;
        _ = Task.Run(() =>
        {
            Utils.StorageService.LoadAudioFiles(sourceDir, true);
            Dispatcher.UIThread.Post(() => OpenSourcePathButton.IsEnabled = true);
            return Task.CompletedTask;
        });
    }

    private async void OnBrowseTargetClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title         = "选择目标目录",
            AllowMultiple = false
        });
        if (folders.Count <= 0) return;
        TargetPathTextBox.Text = folders[0].Path.LocalPath;

        Utils.StorageService.Config.LastTargetDir = TargetPathTextBox.Text;
        Utils.StorageService.SaveConfig();
    }

#endregion

    public void SetButtonEnable(bool enable)
    {
        OpenSourcePathButton.IsEnabled = enable;
        OpenTargetPathButton.IsEnabled = enable;
        SyncButton.IsEnabled           = enable;
    }
}