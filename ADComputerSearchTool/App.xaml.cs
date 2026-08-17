using ADComputerSearchTool.Services;
using ADComputerSearchTool.Services.Interfaces;
using ADComputerSearchTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace ADComputerSearchTool;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        ServiceCollection services =
            new ServiceCollection();

        ConfigureServices(
            services);

        _serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
    }

    private static void ConfigureServices(
        IServiceCollection services)
    {
        services.AddSingleton<
            IIpAddressService,
            DnsIpAddressService>();

        services.AddSingleton<
            IActiveDirectoryService,
            ActiveDirectoryService>();

        services.AddSingleton<
            IClipboardService,
            ClipboardService>();

        services.AddSingleton<
            IExcelExportService,
            ExcelExportService>();

        services.AddSingleton<
            IFileDialogService,
            FileDialogService>();

        services.AddSingleton<
            MainWindowViewModel>();

        services.AddSingleton<
            MainWindow>();
    }

    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow mainWindow =
            _serviceProvider
                .GetRequiredService<MainWindow>();

        MainWindow =
            mainWindow;

        mainWindow.Show();
    }

    protected override void OnExit(
        ExitEventArgs e)
    {
        _serviceProvider.Dispose();

        base.OnExit(e);
    }
}