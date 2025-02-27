using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace dstools.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _cpuName = string.Empty;
    
    [ObservableProperty]
    private string _gpuName = string.Empty;

    [ObservableProperty]
    private double _gpuMemory = 0;
    
    [ObservableProperty]
    private double _totalMemory = 32;

    [ObservableProperty]
    private string _ollamaStatus = "未启动";
    
    [ObservableProperty]
    private string _ollamaVersion = string.Empty;

    private Computer _computer;

    public MainWindowViewModel()
    {
        InitializeHardwareMonitor();
        CheckOllamaStatus();
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
            hardware.Update();
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    CpuName = hardware.Name;
                    break;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuIntel:
                case HardwareType.GpuAmd:
                    GpuName = hardware.Name;
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.SmallData && sensor.Name.Contains("Memory Total"))
                        {
                            GpuMemory = (sensor.Value ?? 0) / 1024;
                        }
                    }
                    break;
                case HardwareType.Memory:
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

    private void CheckOllamaStatus()
    {
        try 
        {
            string ollamaPath = GetOllamaPath();
            if (string.IsNullOrEmpty(ollamaPath))
            {
                OllamaStatus = "未安装";
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ollamaPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(version))
            {
                OllamaVersion = version;
                OllamaStatus = "已安装";
            }
        }
        catch (Exception)
        {
            OllamaStatus = "未安装";
            OllamaVersion = string.Empty;
        }
    }

    private string GetOllamaPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 检查默认安装路径
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string ollamaPath = Path.Combine(programFiles, "ollama", "ollama.exe");
            
            if (File.Exists(ollamaPath))
            {
                return ollamaPath;
            }

            // 检查 PATH 环境变量
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string path in pathEnv.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(path, "ollama.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Unix-like 系统通常安装在 /usr/local/bin
            string ollamaPath = "/usr/local/bin/ollama";
            if (File.Exists(ollamaPath))
            {
                return ollamaPath;
            }

            // 检查 PATH 环境变量
            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string path in pathEnv.Split(Path.PathSeparator))
            {
                string fullPath = Path.Combine(path, "ollama");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return string.Empty;
    }
}