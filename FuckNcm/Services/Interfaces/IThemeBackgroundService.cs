using System.ComponentModel;
using Avalonia.Media;

namespace FuckNcm.Services.Interfaces;

public interface IThemeBackgroundService : INotifyPropertyChanged
{
    /// <summary>
    /// 半透明卡片背景色（只读，随主题自动变化）
    /// </summary>
    public SolidColorBrush CardBackground { get; }

    /// <summary>
    /// 当前主题的背景透明度 (0-255)
    /// </summary>
    public byte Opacity { get; set; }

    /// <summary>
    /// 暗色主题透明度 (0-255)
    /// </summary>
    public byte DarkOpacity { get; set; }

    /// <summary>
    /// 亮色主题透明度 (0-255)
    /// </summary>
    public byte LightOpacity { get; set; }

    /// <summary>
    /// 背景图片
    /// </summary>
    public IImage BackgroundImage { get; }

    /// <summary>
    /// 背景图片透明度 (0.0 - 1.0)
    /// </summary>
    public double BackgroundImageOpacity { get; set; }

    /// <summary>
    /// 背景图片拉伸方式
    /// </summary>
    public Stretch BackgroundImageStretch { get; set; }

    /// <summary>
    /// 是否为暗色模式
    /// </summary>
    public bool IsDarkMode { get; }

    /// <summary>
    /// 设置当前主题背景透明度 (0.0 - 1.0)
    /// </summary>
    public void SetOpacity(double opacity);

    /// <summary>
    /// 设置背景图片
    /// </summary>
    /// <param name="imagePath">图片路径，传入 null 或空字符串清除背景图片</param>
    /// <returns>是否设置成功</returns>
    public void SetBackgroundImage(string imagePath);

    /// <summary>
    /// 清除背景图片
    /// </summary>
    public void SetDefaultBackgroundImage();

    /// <summary>
    /// 更新组件背景颜色
    /// </summary>
    public void UpdateBackgroundColor();
}