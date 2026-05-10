using System.Windows;
using SystemMonitor.WPF.ViewModels;

namespace SystemMonitor.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Minimise to tray instead of closing
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }
}
