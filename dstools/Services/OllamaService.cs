using System;
using System.Collections.Generic;
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

            // 如果正在运行，获取已安装的模型
            if (info.RunningStatus == RunningStatus.Running)
            {
                info.InstalledModels = await GetInstalledModels();
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

    public async Task<bool> InstallModel(string modelName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetOllamaPath(),
                    Arguments = $"pull {modelName}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"安装模型时出错: {ex.Message}");
            return false;
        }
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
            new() { Name = "deepseek-coder:6.7b", Size = 4.2, Description = "DeepSeek Coder 6.7B 基础版" },
            new() { Name = "deepseek-coder:33b", Size = 19.5, Description = "DeepSeek Coder 33B 基础版" },
            new() { Name = "deepseek-coder:6.7b-instruct", Size = 4.2, Description = "DeepSeek Coder 6.7B 对话版" },
            new() { Name = "deepseek-coder:33b-instruct", Size = 19.5, Description = "DeepSeek Coder 33B 对话版" }
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
}