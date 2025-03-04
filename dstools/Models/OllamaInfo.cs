using System.Collections.Generic;
using dstools.ViewModels;

namespace dstools.Models;

public class OllamaInfo
{
    public InstallStatus InstallStatus { get; set; }
    public RunningStatus RunningStatus { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<string> InstalledModels { get; set; } = new();
} 