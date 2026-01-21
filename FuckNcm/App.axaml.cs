using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FuckNcm.Services;
using FuckNcm.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace FuckNcm;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        StorageService storageService = new();
        Utils.ServiceCollection.AddSingleton<IStorageService>(storageService);
        Utils.ServiceCollection.AddSingleton<IThemeBackgroundService>(
            new ThemeBackgroundService(storageService.Config));
        Utils.ServiceCollection.AddSingleton<ISukiDialogManager>(new SukiDialogManager());
        Utils.RebuildServiceProvider();

        //在所有服务注册完成后再绑定主题切换事件，防止DI提前被加载
        Utils.ThemeBackgroundService.BindingSukiThemeChangeEvent();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}