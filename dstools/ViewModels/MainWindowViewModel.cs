using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using System;

namespace dstools.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _cpuName = string.Empty;

    [ObservableProperty]
    private double _cpuUsage = 0;

    [ObservableProperty]
    private double _cpuTemperature = 0;
    
    [ObservableProperty]
    private string _gpuName = string.Empty;

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

    private Computer _computer;

    public MainWindowViewModel()
    {
        InitializeHardwareMonitor();
    }

    private void InitializeHardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };
        
        _computer.Open();
        UpdateHardwareInfo();
        _computer.Close();
    }

    private void UpdateHardwareInfo()
    {
        foreach (var hardware in _computer.Hardware)
        {
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    CpuName = hardware.Name;
                    break;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuIntel:
                case HardwareType.GpuAmd:
                    GpuName = hardware.Name;
                    break;
                case HardwareType.Memory:
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Data && sensor.Name == "Used Memory")
                        {
                            TotalMemory = sensor.Value ?? 0;
                        }
                    }
                    break;
            }
        }
    }
}