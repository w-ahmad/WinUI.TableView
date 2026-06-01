using Microsoft.UI.Xaml;
using WinUI.TableView;

namespace AotTestApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainViewModel.InitializeItems();
        ViewModel = new MainViewModel();
    }

    public MainViewModel ViewModel { get; }
}