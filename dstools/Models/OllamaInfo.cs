using System.Collections.Generic;
using dstools.ViewModels;

namespace dstools.Models;

public class OllamaInfo
{
    public InstallStatus InstallStatus { get; set; }
    public RunningStatus RunningStatus { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<ModelInfo> InstalledModels { get; set; } = new();
    public List<AvailableModel> AvailableModels { get; set; } = new();
}

public class AvailableModel
{
    public string Name { get; set; } = string.Empty;
    public double Size { get; set; }
    public string Description { get; set; } = string.Empty;
}