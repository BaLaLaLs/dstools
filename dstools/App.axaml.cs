using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using dstools.Services;
using dstools.ViewModels;
using dstools.Views;
using Microsoft.Extensions.DependencyInjection;

namespace dstools;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 设置依赖注入
        var services = new ServiceCollection();
        
        // 注册服务
        services.AddSingleton<IHardwareService, HardwareService>();
        services.AddSingleton<IOllamaService, OllamaService>();
        
        // 注册 ViewModel
        services.AddTransient<MainWindowViewModel>();
        
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 避免 Avalonia 和 CommunityToolkit 的重复验证
            DisableAvaloniaDataAnnotationValidation();
            
            // 从服务容器中获取 ViewModel
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}