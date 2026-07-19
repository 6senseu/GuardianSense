using System.Windows;
using Guardian.Dashboard.ViewModels;

namespace Guardian.Dashboard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel();
    }
}
