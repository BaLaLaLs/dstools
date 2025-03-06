using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using dstools.Models;
using dstools.ViewModels;

namespace dstools.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public OllamaService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<OllamaInfo> GetOllamaInfo()
    {
        var info = new OllamaInfo();

        try
        {
            string ollamaPath = GetOllamaPath();
            if (string.IsNullOrEmpty(ollamaPath))
            {
                info.InstallStatus = InstallStatus.NotInstalled;
                info.RunningStatus = RunningStatus.Stopped;
                return info;
            }

            // 检查版本
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
                info.InstallStatus = InstallStatus.NotInstalled;
                return info;
            }

            const string prefix = "ollama version is ";
            info.Version = version.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? version[prefix.Length..].Trim()
                : version;

            info.InstallStatus = InstallStatus.Installed;

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

            info.RunningStatus = !string.IsNullOrEmpty(output) ? RunningStatus.Running : RunningStatus.Stopped;
            // 获取模型安装路径
            info.ModelInstallPath = GetModelInstallPath();
            // 如果正在运行，获取已安装的模型
            if (info.RunningStatus == RunningStatus.Running)
            {
                info.InstalledModels = new ObservableCollection<ModelInfo>(await GetInstalledModels());
                info.AvailableModels = GetDefaultModels();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取Ollama信息时出错: {ex.Message}");
            info.InstallStatus = InstallStatus.NotInstalled;
            info.RunningStatus = RunningStatus.Stopped;
        }

        return info;
    }

    public async Task<bool> InstallOllama(IProgress<double> progress)
    {
        try
        {
            string downloadUrl =
                "https://ghfast.top/https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe";
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string setupPath = Path.Combine(appDirectory, "OllamaSetup.exe");

            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
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
                    progress.Report((double)totalBytesRead / totalBytes * 100);
                }
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = setupPath,
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };
            process.Start();
            await Task.Run(() =>
            {
                process.WaitForExit();
                if (File.Exists(setupPath))
                {
                    File.Delete(setupPath);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"安装Ollama时出错: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartOllama()
    {
        try
        {
            string ollamaPath = GetOllamaPath();
            if (string.IsNullOrEmpty(ollamaPath))
            {
                return false;
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
            await Task.Delay(2000); // 等待服务启动
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动Ollama时出错: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopOllama()
    {
        try
        {
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
            await Task.Run(() => psProcess.WaitForExit());
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"停止Ollama时出错: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ModelInfo>> GetInstalledModels()
    {
        var models = new List<ModelInfo>();
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:11434/api/tags");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    var tagsResponse = JsonSerializer.Deserialize(content, OllamaJsonContext.Default.TagsResponse);
                    if (tagsResponse?.Models != null)
                    {
                        models = tagsResponse.Models;
                        // 转换时间格式
                        foreach (var model in models)
                        {
                            model.Size /= 1024 * 1024 * 1024; // 转换为GB
                            // 转换时间格式
                            if (DateTime.TryParse(model.ModifiedAt, out var dateTime))
                            {
                                model.ModifiedAt = dateTime.ToString("yyyy-MM-dd HH:mm");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取模型列表时出错: {ex.Message}");
        }

        return models;
    }
    
    public async Task<bool> DeleteModel(string modelName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, "http://localhost:11434/api/delete")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(
                        new DeleteModelRequest { Model = modelName },
                        OllamaJsonContext.Default.DeleteModelRequest
                    ),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"删除模型时出错: {ex.Message}");
            return false;
        }
    }

    private List<AvailableModel> GetDefaultModels()
    {
        return new List<AvailableModel>
        {
            new()
            {
                Name = "deepseek-coder:1.5b", Size = 4.2, Description = "DeepSeek Coder 6.7B 基础版",
                Url = ""
            },
            new()
            {
                Name = "DeepSeek-R1-Distill-Qwen-7B-GGUF", Size = 4.68, Description = "DeepSeek-R1-Distill-Qwen-7B-GGUF",
                Url = "modelscope.cn/unsloth/DeepSeek-R1-Distill-Qwen-7B-GGUF:DeepSeek-R1-Distill-Qwen-7B-Q4_K_M.gguf"
            },
            new() { Name = "deepseek-r1:14b", Size = 19.5, Description = "deepseek-r1:14b版", Url = "modelscope.cn/unsloth/DeepSeek-R1-Distill-Qwen-14B-GGUF:Q4_K_M" },
            new()
            {
                Name = "deepseek-coder:6.7b-instruct", Size = 4.2, Description = "DeepSeek Coder 6.7B 对话版", Url = ""
            },
            new()
            {
                Name = "deepseek-coder:33b-instruct", Size = 19.5, Description = "DeepSeek Coder 33B 对话版", Url = ""
            }
        };
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
    // 获取Ollama 模型安装位置
    public string GetModelInstallPath()
    {
        try
        {
            // 首先尝试从环境变量获取
            string modelPath = Environment.GetEnvironmentVariable("OLLAMA_MODELS", EnvironmentVariableTarget.User) ?? "";
            
            // 如果环境变量未设置，使用默认路径
            if (string.IsNullOrEmpty(modelPath))
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                modelPath = Path.Combine(userProfile, ".ollama", "models");
            }
            
            return modelPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取模型路径时出错: {ex.Message}");
            return string.Empty;
        }
    }
    // 下载模型
    public async Task<bool> PullModel(String modelName)
    {
        try
        {
            // 创建请求内容
            var requestContent = new StringContent(
                JsonSerializer.Serialize(
                    new PullModelRequest { Model = modelName },
                    OllamaJsonContext.Default.PullModelRequest
                ),
                Encoding.UTF8,
                "application/json"
            );

            // 发送请求到 Ollama API
            var response = await _httpClient.PostAsync("http://localhost:11434/api/pull", requestContent);
            
            // 检查响应状态
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"拉取模型失败: {response.StatusCode}");
                return false;
            }

            // 读取响应流
            using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());
            string line;
            
            // 处理流式响应
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                // 解析 JSON 响应
                var pullResponse = JsonSerializer.Deserialize(line, OllamaJsonContext.Default.PullModelResponse);
                if (pullResponse == null) continue;
                
                // 检查是否有错误
                if (!string.IsNullOrEmpty(pullResponse.Error))
                {
                    Debug.WriteLine($"拉取模型时出错: {pullResponse.Error}");
                    return false;
                }
                
                // 检查是否完成
                if (pullResponse.Status == "success")
                {
                    return true;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"拉取模型时出错: {ex.Message}");
            return false;
        }
    }
}