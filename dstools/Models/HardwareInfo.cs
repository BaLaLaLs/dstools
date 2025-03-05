namespace dstools.Models;

public class HardwareInfo
{
    public string CpuName { get; set; } = string.Empty;
    public string GpuName { get; set; } = string.Empty;
    public double GpuMemory { get; set; }
    public double TotalMemory { get; set; }
}