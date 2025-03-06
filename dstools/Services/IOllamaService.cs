using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using dstools.Models;
using dstools.ViewModels;

namespace dstools.Services;

public interface IOllamaService
{
    Task<OllamaInfo> GetOllamaInfo();
    Task<bool> InstallOllama(IProgress<double> progress);
    Task<bool> StartOllama();
    Task<bool> StopOllama();
    Task<List<ModelInfo>> GetInstalledModels();
    Task<bool> DeleteModel(string modelName);
    string GetModelInstallPath();
    Task<bool> PullModel(String modelName);
}