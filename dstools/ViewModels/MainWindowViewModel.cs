using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace dstools.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _cpuName = "Intel Core i7-12700K";

    [ObservableProperty]
    private double _cpuUsage = 0;

    [ObservableProperty]
    private double _cpuTemperature = 0;
    
    [ObservableProperty]
    private string _gpuName = "NVIDIA RTX 3080";

    [ObservableProperty]
    private double _gpuUsage = 0;

    [ObservableProperty]
    private double _gpuTemperature = 0;
    
    [ObservableProperty]
    private double _totalMemory = 32;

    [ObservableProperty]
    private double _usedMemory = 0;

    [ObservableProperty]
    private double _memoryUsage = 0;
    
    [ObservableProperty]
    private string _diskModel = "Samsung 970 EVO Plus";

    [ObservableProperty]
    private double _totalSpace = 1000;

    [ObservableProperty]
    private double _usedSpace = 0;

    public MainWindowViewModel()
    {
        StartHardwareMonitoring();
    }

    private void StartHardwareMonitoring()
    {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (sender, args) =>
        {
            UpdateHardwareInfo();
        };
        timer.Start();
    }

    private void UpdateHardwareInfo()
    {
        Random rand = new Random();
        CpuUsage = rand.Next(0, 100);
        CpuTemperature = 40 + rand.Next(0, 30);
        GpuUsage = rand.Next(0, 100);
        GpuTemperature = 35 + rand.Next(0, 35);
        UsedMemory = rand.Next(0, (int)TotalMemory);
        MemoryUsage = (UsedMemory / TotalMemory) * 100;
        UsedSpace = rand.Next(0, (int)TotalSpace);
    }
}