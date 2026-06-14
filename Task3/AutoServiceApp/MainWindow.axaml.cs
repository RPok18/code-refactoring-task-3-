using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AutoServiceApp.Services;

namespace AutoServiceApp;

public partial class MainWindow : Window
{
    public AutoServiceManager Manager { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        Manager.Load();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
