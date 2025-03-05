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
using dstools.Models;
using dstools.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace dstools.ViewModels;

public enum InstallStatus
{
    NotInstalled,    // 未安装
    Installed        // 已安装
}

public enum RunningStatus
{
    Running,         // 运行中
    Stopped,          // 已停止
    NotRunning
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IHardwareService _hardwareService;
    private readonly IOllamaService _ollamaService;
    
    [ObservableProperty]
    private HardwareInfo _hardwareInfo = new();

    [ObservableProperty]
    private OllamaInfo _ollamaInfo = new();
    
    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _hasError;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public MainWindowViewModel(
        IHardwareService hardwareService,
        IOllamaService ollamaService)
    {
        _hardwareService = hardwareService;
        _ollamaService = ollamaService;
        
        Initialize();
    }

    private async void Initialize()
    {
        // 获取硬件信息
        HardwareInfo = _hardwareService.GetHardwareInfo();
        
        // 获取 Ollama 信息
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }

    [RelayCommand]
    private async Task InstallOllama()
    {
        IsDownloading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        
        var progress = new Progress<double>(value => DownloadProgress = value);
        var success = await _ollamaService.InstallOllama(progress);
        
        if (!success)
        {
            HasError = true;
            ErrorMessage = "安装失败";
        }
        
        IsDownloading = false;
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }

    [RelayCommand]
    private async Task StartOllama()
    {
            HasError = false;
            ErrorMessage = string.Empty;
            
        var success = await _ollamaService.StartOllama();
        if (!success)
        {
            HasError = true;
            ErrorMessage = "启动失败";
        }
        
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }
    
    [RelayCommand]
    private async Task StopOllama()
    {
            HasError = false;
            ErrorMessage = string.Empty;
            
        var success = await _ollamaService.StopOllama();
        if (!success)
        {
            HasError = true;
            ErrorMessage = "停止失败";
        }
        
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }

    [RelayCommand]
    private async Task InstallModel(string modelName)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        
        var success = await _ollamaService.InstallModel(modelName);
        if (!success)
        {
            HasError = true;
            ErrorMessage = "模型安装失败";
        }
        
        // 刷新模型列表
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }

    [RelayCommand]
    private async Task DeleteModel(string modelName)
    {
        // 显示确认对话框
        var messageBoxStandardWindow = MessageBoxManager
            .GetMessageBoxStandard(
                "确认删除",
                $"确定要删除模型 {modelName} 吗？",
                ButtonEnum.YesNo,
                Icon.Question);

        var result = await messageBoxStandardWindow.ShowAsync();
        
        if (result != ButtonResult.Yes)
        {
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        
        var success = await _ollamaService.DeleteModel(modelName);
        if (!success)
        {
            HasError = true;
            ErrorMessage = "模型删除失败";
        }
        
        // 刷新模型列表
        OllamaInfo = await _ollamaService.GetOllamaInfo();
    }
}

[JsonSerializable(typeof(TagsResponse))]
[JsonSerializable(typeof(List<ModelInfo>))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(DeleteModelRequest))]
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

public class DeleteModelRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}