using System.Diagnostics;
using System.Management;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace dstools.Views;

public partial class MainWindow : Window
{
    private string? CPUName;

    public MainWindow()
    {
        InitializeComponent();
        
    }
}