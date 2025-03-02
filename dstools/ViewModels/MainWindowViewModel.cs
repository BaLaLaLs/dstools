using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace dstools.ViewModels;

public enum InstallStatus
{
    NotInstalled,    // 未安装
    Installed        // 已安装
}

public enum RunningStatus
{
    Running,         // 运行中
    Stopped          // 已停止
}

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
    private InstallStatus _installStatus = InstallStatus.NotInstalled;

    [ObservableProperty]
    private RunningStatus _runningStatus = RunningStatus.Stopped;
    
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
                InstallStatus = InstallStatus.NotInstalled;
                RunningStatus = RunningStatus.Stopped;
                OllamaVersion = string.Empty;
                return;
            }

            // 检查版本以确认安装状态
            var versionProcess = new Process
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

            versionProcess.Start();
            string version = versionProcess.StandardOutput.ReadToEnd().Trim();
            versionProcess.WaitForExit();

            if (string.IsNullOrEmpty(version))
            {
                InstallStatus = InstallStatus.NotInstalled;
                RunningStatus = RunningStatus.Stopped;
                OllamaVersion = string.Empty;
                return;
            }

            const string prefix = "ollama version is ";
            OllamaVersion = version.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) 
                ? version[prefix.Length..].Trim() 
                : version;

            InstallStatus = InstallStatus.Installed;

            // 检查运行状态
            var psProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Get-Process ollama -ErrorAction SilentlyContinue\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            psProcess.Start();
            string output = psProcess.StandardOutput.ReadToEnd();
            psProcess.WaitForExit();

            RunningStatus = !string.IsNullOrEmpty(output) ? RunningStatus.Running : RunningStatus.Stopped;
        }
        catch (Exception)
        {
            InstallStatus = InstallStatus.NotInstalled;
            RunningStatus = RunningStatus.Stopped;
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
    [ObservableProperty]
    private double _downloadProgress = 0;

    [ObservableProperty]
    private bool _isDownloading = false;

    [ObservableProperty]
    private bool _hasError = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task InstallOllama()
    {
        IsDownloading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        DownloadProgress = 0;
        await DownloadAndInstallOllama();
        IsDownloading = false;
    }
    private async Task DownloadAndInstallOllama()
    {
        try
        {
            string downloadUrl = "https://ghfast.top/https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe";
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string setupPath = Path.Combine(appDirectory, "OllamaSetup.exe");

            using (var client = new HttpClient())
            {
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(setupPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;
                    if (totalBytes > 0)
                    {
                        DownloadProgress = (double)totalBytesRead / totalBytes * 100;
                    }
                }
            }

            // 启动安装程序
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = setupPath,
                    UseShellExecute = true,
                    Verb = "runas" // 请求管理员权限
                }
            };
            process.Start();

            // 等待安装完成后删除安装文件
            await Task.Run(() =>
            {
                process.WaitForExit();
                if (File.Exists(setupPath))
                {
                    File.Delete(setupPath);
                }
                CheckOllamaStatus(); // 刷新安装状态
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"下载安装包失败：{ex.Message}";
            Debug.WriteLine($"安装Ollama时发生错误: {ex.Message}");
            DownloadProgress = 0;
        }
    }
    [RelayCommand]
    private void StartOllama()
    {
        try
        {
            string ollamaPath = GetOllamaPath();
            if (string.IsNullOrEmpty(ollamaPath))
            {
                HasError = true;
                ErrorMessage = "未找到Ollama程序";
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ollamaPath,
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            RunningStatus = RunningStatus.Running;
            HasError = false;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"启动Ollama失败：{ex.Message}";
            Debug.WriteLine($"启动Ollama时发生错误: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void StopOllama()
    {
        try
        {
            // 使用PowerShell命令终止Ollama进程
            var psProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Stop-Process -Name ollama -Force -ErrorAction SilentlyContinue\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            psProcess.Start();
            psProcess.WaitForExit();
            
            // 更新状态
            RunningStatus = RunningStatus.Stopped;
            HasError = false;
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"停止Ollama失败：{ex.Message}";
            Debug.WriteLine($"停止Ollama时发生错误: {ex.Message}");
        }
    }
}