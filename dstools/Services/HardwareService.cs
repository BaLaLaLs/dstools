using System;
using System.Linq;
using dstools.Models;
using LibreHardwareMonitor.Hardware;

namespace dstools.Services;

public class HardwareService : IHardwareService
{
    private readonly Computer _computer;

    public HardwareService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };
    }

    public HardwareInfo GetHardwareInfo()
    {
        var info = new HardwareInfo();
        
        _computer.Open();
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    info.CpuName = hardware.Name;
                    break;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuIntel:
                case HardwareType.GpuAmd:
                    info.GpuName = hardware.Name;
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.SmallData && 
                            sensor.Name.Contains("Memory Total"))
                        {
                            info.GpuMemory = (sensor.Value ?? 0) / 1024;
                        }
                    }
                    break;
                case HardwareType.Memory:
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Data && sensor.Name == "Memory Used")
                        {
                            info.TotalMemory = sensor.Value ?? 0;
                        }
                    }
                    break;
            }
        }
        _computer.Close();
        
        return info;
    }
} 