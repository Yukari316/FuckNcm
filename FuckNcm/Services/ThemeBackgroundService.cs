using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Styling;
using FuckNcm.Services.Interfaces;
using SukiUI;
using SukiUI.Controls;
using SukiUI.MessageBox;

namespace FuckNcm.Services;

/// <summary>
/// 主题背景服务 - 提供随主题变化的半透明背景色和背景图片
/// </summary>
public sealed class ThemeBackgroundService : IThemeBackgroundService
{
    private byte _darkOpacity  = 0x10;
    private byte _lightOpacity = 0x7D;

    // 背景图片相关

    public ThemeBackgroundService()
    {
        SukiTheme.GetInstance().OnBaseThemeChanged += _ =>
        {
            UpdateBackgroundColor();
            Utils.StorageService.Config.DarkTheme = IsDarkMode;
            Utils.StorageService.SaveConfig();
        };
        SukiTheme.GetInstance()
                 .ChangeBaseTheme(Utils.StorageService.Config.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light);
        UpdateBackgroundColor();

        if (!string.IsNullOrEmpty(Utils.StorageService.Config.BackgroundImagePath)
            && File.Exists(Utils.StorageService.Config.BackgroundImagePath))
            SetBackgroundImage(Utils.StorageService.Config.BackgroundImagePath);
        else
            SetDefaultBackgroundImage();
    }

    /// <summary>
    /// 半透明卡片背景色（只读，随主题自动变化）
    /// </summary>
    public SolidColorBrush CardBackground
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    } = null!;

    /// <summary>
    /// 当前主题的背景透明度 (0-255)
    /// </summary>
    public byte Opacity
    {
        get => IsDarkMode ? _darkOpacity : _lightOpacity;
        set
        {
            ref byte target = ref IsDarkMode ? ref _darkOpacity : ref _lightOpacity;
            if (target == value) return;
            target = value;
            OnPropertyChanged();
            UpdateBackgroundColor();
        }
    }

    /// <summary>
    /// 暗色主题透明度 (0-255)
    /// </summary>
    public byte DarkOpacity
    {
        get => _darkOpacity;
        set
        {
            if (_darkOpacity == value) return;
            _darkOpacity = value;
            OnPropertyChanged();
            if (IsDarkMode) UpdateBackgroundColor();
        }
    }

    /// <summary>
    /// 亮色主题透明度 (0-255)
    /// </summary>
    public byte LightOpacity
    {
        get => _lightOpacity;
        set
        {
            if (_lightOpacity == value) return;
            _lightOpacity = value;
            OnPropertyChanged();
            if (!IsDarkMode) UpdateBackgroundColor();
        }
    }

    /// <summary>
    /// 背景图片
    /// </summary>
    public IImage BackgroundImage
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 背景图片透明度 (0.0 - 1.0)
    /// </summary>
    public double BackgroundImageOpacity
    {
        get;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(field - clamped) > 0.001)
            {
                field = clamped;
                OnPropertyChanged();
            }
        }
    } = 0.15;

    /// <summary>
    /// 背景图片拉伸方式
    /// </summary>
    public Stretch BackgroundImageStretch
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = Stretch.UniformToFill;

    /// <summary>
    /// 是否为暗色模式
    /// </summary>
    public bool IsDarkMode => SukiTheme.GetInstance().ActiveBaseTheme == ThemeVariant.Dark;

    /// <summary>
    /// 设置当前主题背景透明度 (0.0 - 1.0)
    /// </summary>
    public void SetOpacity(double opacity)
    {
        Opacity = (byte)(Math.Clamp(opacity, 0.0, 1.0) * 255);
    }

    /// <summary>
    /// 设置背景图片
    /// </summary>
    /// <param name="imagePath">图片路径，传入 null 或空字符串清除背景图片</param>
    /// <returns>是否设置成功</returns>
    public async void SetBackgroundImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            BackgroundImage = null;
        try
        {
            if (!File.Exists(imagePath))
            {
                SetDefaultBackgroundImage();
                await SukiMessageBox.ShowDialog(new SukiMessageBoxHost
                {
                    IconPreset          = SukiMessageBoxIcons.Error,
                    Header              = "Error",
                    Content             = "背景图片文件不存在，已恢复为默认背景。",
                    ActionButtonsPreset = SukiMessageBoxButtons.Close
                });
                return;
            }

            BackgroundImage = new Bitmap(imagePath);

            Utils.StorageService.Config.BackgroundImagePath = imagePath;
            Utils.StorageService.SaveConfig();
        }
        catch (Exception e)
        {
            SetDefaultBackgroundImage();
            await SukiMessageBox.ShowDialog(new SukiMessageBoxHost
            {
                IconPreset          = SukiMessageBoxIcons.Error,
                Header              = "Error",
                Content             = $"读取背景图片时发生错误，已恢复为默认背景。\n\n {e}",
                ActionButtonsPreset = SukiMessageBoxButtons.Close
            });
        }
    }

    /// <summary>
    /// 清除背景图片
    /// </summary>
    public void SetDefaultBackgroundImage()
    {
        BackgroundImage = null;

        Utils.StorageService.Config.BackgroundImagePath = string.Empty;
        Utils.StorageService.SaveConfig();

        using MemoryStream ms    = new(Utils.GetEmbeddedResourceStream("Resources.background.jpg"));
        Bitmap             image = new(ms);

        BackgroundImage = image;
    }

    /// <summary>
    /// 更新组件背景颜色
    /// </summary>
    public void UpdateBackgroundColor()
    {
        Color baseColor = IsDarkMode ? Colors.Black : Colors.DarkGray;
        byte opacity = IsDarkMode ? _darkOpacity : _lightOpacity;
        BackgroundImageOpacity = IsDarkMode ? 0.15 : 0.6;
        CardBackground = new SolidColorBrush(Color.FromArgb(opacity, baseColor.R, baseColor.G, baseColor.B));
        Utils.MainWindow?.DarkModeIcon.IsVisible = IsDarkMode;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}