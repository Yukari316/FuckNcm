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
        Utils.ServiceCollection.AddSingleton<IStorageService>(new StorageService());
        Utils.StorageService.InitConfig();
        Utils.ServiceCollection.AddSingleton<IThemeBackgroundService>(new ThemeBackgroundService());
        Utils.ServiceCollection.AddSingleton<ISukiDialogManager>(new SukiDialogManager());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}