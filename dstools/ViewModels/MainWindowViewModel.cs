using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [ObservableProperty]
    private ObservableCollection<string> _installedModels = new();

    private System.Timers.Timer? _modelRefreshTimer;

    public MainWindowViewModel()
    {
        InitializeHardwareMonitor();
        CheckOllamaStatus();
        
        // 如果 Ollama 已安装，则获取模型列表
        if (InstallStatus == InstallStatus.Installed)
        {
            _ = FetchInstalledModels();
            
            // 设置定时器，每30秒刷新一次模型列表
            _modelRefreshTimer = new System.Timers.Timer(30000);
            _modelRefreshTimer.Elapsed += async (s, e) => 
            {
                if (RunningStatus == RunningStatus.Running)
                {
                    await FetchInstalledModels();
                }
            };
            _modelRefreshTimer.Start();
        }
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
        
        // 在状态检查后，如果 Ollama 正在运行，则获取模型列表
        if (InstallStatus == InstallStatus.Installed && RunningStatus == RunningStatus.Running)
        {
            _ = FetchInstalledModels();
            
            // 如果定时器尚未创建，则创建定时器
            if (_modelRefreshTimer == null)
            {
                _modelRefreshTimer = new System.Timers.Timer(30000);
                _modelRefreshTimer.Elapsed += async (s, e) => 
                {
                    if (RunningStatus == RunningStatus.Running)
                    {
                        await FetchInstalledModels();
                    }
                };
                _modelRefreshTimer.Start();
            }
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
    private async Task StartOllama()
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
            
            // 等待 Ollama 服务启动
            await Task.Delay(2000);
            
            // 获取模型列表
            await FetchInstalledModels();
            
            // 如果定时器尚未创建，则创建定时器
            if (_modelRefreshTimer == null)
            {
                _modelRefreshTimer = new System.Timers.Timer(30000);
                _modelRefreshTimer.Elapsed += async (s, e) => 
                {
                    if (RunningStatus == RunningStatus.Running)
                    {
                        await FetchInstalledModels();
                    }
                };
                _modelRefreshTimer.Start();
            }
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
            
            // 清空模型列表
            InstalledModels.Clear();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"停止Ollama失败：{ex.Message}";
            Debug.WriteLine($"停止Ollama时发生错误: {ex.Message}");
        }
    }

    // 添加获取已安装模型的方法
    private async Task FetchInstalledModels()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            Debug.WriteLine("正在尝试获取模型列表...");
            
            var response = await client.GetAsync("http://localhost:11434/api/tags");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"API 响应内容: {content}");
                
                if (string.IsNullOrEmpty(content))
                {
                    Debug.WriteLine("API 响应内容为空");
                    InstalledModels.Clear();
                    return;
                }
                
                try
                {
                    // 使用源生成的序列化器
                    var tagsResponse = JsonSerializer.Deserialize(content, OllamaJsonContext.Default.TagsResponse);
                    
                    if (tagsResponse != null && tagsResponse.Models.Count > 0)
                    {
                        var modelNames = tagsResponse.Models
                            .Select(m => m.Name)
                            .OrderBy(name => name)
                            .ToList();
                        
                        Debug.WriteLine($"找到 {modelNames.Count} 个模型");
                        
                        // 在UI线程上更新集合
                        await Task.Run(() => 
                        {
                            InstalledModels.Clear();
                            foreach (var model in modelNames)
                            {
                                InstalledModels.Add(model);
                            }
                        });
                    }
                    else
                    {
                        Debug.WriteLine("未找到模型或解析失败");
                        InstalledModels.Clear();
                    }
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON 解析错误: {ex.Message}");
                    Debug.WriteLine($"JSON 内容: {content}");
                    InstalledModels.Clear();
                }
            }
            else
            {
                Debug.WriteLine($"API 请求失败，状态码: {response.StatusCode}");
                InstalledModels.Clear();
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HTTP 请求错误: {ex.Message}");
            InstalledModels.Clear();
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("请求超时");
            InstalledModels.Clear();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取模型列表时出错: {ex.Message}");
            InstalledModels.Clear();
        }
    }
}

[JsonSerializable(typeof(TagsResponse))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
partial class OllamaJsonContext : JsonSerializerContext
{
}

public class TagsResponse
{
    [JsonPropertyName("models")]
    public List<ModelInfo> Models { get; set; } = new();
}

public class ModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public string ModifiedAt { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;

}