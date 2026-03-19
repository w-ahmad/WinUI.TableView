using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace TableViewNuGetSample.Views;

/// <summary>
/// A sample page demonstrating WinUI.TableView features.
/// </summary>
public partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncThemeToggle();
    }

    private void OnThemeToggleToggled(object sender, RoutedEventArgs e)
    {
        if (XamlRoot?.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = ThemeToggleSwitch.IsOn ? ElementTheme.Dark : ElementTheme.Light;
        RequestedTheme = root.RequestedTheme;
    }

    public List<string> RatingOptions { get; } = ["★", "★★", "★★★", "★★★★", "★★★★★"];

    public ObservableCollection<ProductRow> ProductRows { get; } =
    [
        new ProductRow { Name = "Surface Laptop 6", Category = "Laptops", Price = 1299.99, Stock = 45, IsAvailable = true, DateAdded = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★★" },
        new ProductRow { Name = "Surface Pro 11", Category = "Tablets", Price = 999.99, Stock = 120, IsAvailable = true, DateAdded = new DateTimeOffset(2025, 3, 10, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★" },
        new ProductRow { Name = "Arc Mouse", Category = "Accessories", Price = 79.99, Stock = 300, IsAvailable = true, DateAdded = new DateTimeOffset(2024, 11, 1, 0, 0, 0, TimeSpan.Zero), Rating = "★★★" },
        new ProductRow { Name = "Surface Headphones 3", Category = "Audio", Price = 249.99, Stock = 0, IsAvailable = false, DateAdded = new DateTimeOffset(2025, 1, 20, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★" },
        new ProductRow { Name = "Xbox Wireless Controller", Category = "Gaming", Price = 64.99, Stock = 500, IsAvailable = true, DateAdded = new DateTimeOffset(2024, 9, 5, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★★" },
        new ProductRow { Name = "Surface Dock 2", Category = "Accessories", Price = 259.99, Stock = 80, IsAvailable = true, DateAdded = new DateTimeOffset(2024, 7, 12, 0, 0, 0, TimeSpan.Zero), Rating = "★★★" },
        new ProductRow { Name = "Surface Studio Monitor", Category = "Monitors", Price = 1599.99, Stock = 15, IsAvailable = true, DateAdded = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★★" },
        new ProductRow { Name = "Designer Compact Keyboard", Category = "Accessories", Price = 69.99, Stock = 0, IsAvailable = false, DateAdded = new DateTimeOffset(2024, 12, 8, 0, 0, 0, TimeSpan.Zero), Rating = "★★" },
        new ProductRow { Name = "Surface Go 4", Category = "Tablets", Price = 579.99, Stock = 200, IsAvailable = true, DateAdded = new DateTimeOffset(2025, 2, 14, 0, 0, 0, TimeSpan.Zero), Rating = "★★★" },
        new ProductRow { Name = "Surface Earbuds", Category = "Audio", Price = 199.99, Stock = 60, IsAvailable = true, DateAdded = new DateTimeOffset(2025, 4, 22, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★" },
        new ProductRow { Name = "Surface Laptop Studio 2", Category = "Laptops", Price = 1999.99, Stock = 30, IsAvailable = true, DateAdded = new DateTimeOffset(2024, 10, 18, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★★" },
        new ProductRow { Name = "Ergonomic Mouse", Category = "Accessories", Price = 49.99, Stock = 400, IsAvailable = true, DateAdded = new DateTimeOffset(2024, 8, 3, 0, 0, 0, TimeSpan.Zero), Rating = "★★★★" },
    ];

    public ObservableCollection<ExplorerItemRow> ExplorerRows { get; } =
    [
        new ExplorerItemRow
        {
            Name = "drop_amd64fre (1).zip",
            DateModified = "3/16/2026 7:54 PM",
            Type = "Compressed (zipped) Folder",
            Size = "205,602 KB",
            GroupLabel = "Earlier this week",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "drop_amd64fre (1)",
            DateModified = "3/16/2026 7:55 PM",
            Type = "File folder",
            Size = string.Empty,
            GroupLabel = "Earlier this week",
            IconGlyph = "\uE8B7"
        },
        new ExplorerItemRow
        {
            Name = "AppxManifest(1).xml",
            DateModified = "3/12/2026 7:29 PM",
            Type = "Microsoft Edge HTML Document",
            Size = "125 KB",
            GroupLabel = "Last week",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "AppxManifest.xml",
            DateModified = "3/12/2026 6:49 PM",
            Type = "Microsoft Edge HTML Document",
            Size = "135 KB",
            GroupLabel = "Last week",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "bootstrap-copilot.md",
            DateModified = "3/6/2026 11:11 AM",
            Type = "Markdown Source File",
            Size = "11 KB",
            GroupLabel = "Earlier this month",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "PhotosViewPrototype1.zip",
            DateModified = "3/6/2026 11:01 AM",
            Type = "Compressed (zipped) Folder",
            Size = "475,881 KB",
            GroupLabel = "Earlier this month",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "InputHost.zip",
            DateModified = "2/20/2026 8:59 PM",
            Type = "Compressed (zipped) Folder",
            Size = "996 KB",
            GroupLabel = "Last month",
            IconGlyph = "\uE8A5"
        },
        new ExplorerItemRow
        {
            Name = "Scenario1.html",
            DateModified = "2/16/2026 6:02 PM",
            Type = "Microsoft Edge HTML Document",
            Size = "20 KB",
            GroupLabel = "Last month",
            IconGlyph = "&#xE8A5;"
        },
    ];

    public ObservableCollection<TaskProcessRow> TaskRows { get; } =
    [
        new TaskProcessRow
        {
            Name = "Calendar (2)",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "16.8 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Apps",
            IsExpanded = false,
            IconGlyph = "\uE787",
            Children =
            [
                new TaskProcessRow
                {
                    Name = "Runtime Broker",
                    Status = string.Empty,
                    Cpu = "0%",
                    Memory = "6.3 MB",
                    Disk = "0 MB/s",
                    Network = "0 Mbps",
                    Category = "Apps",
                    IconGlyph = "\uE7BA"
                },
            ]
        },
        new TaskProcessRow
        {
            Name = "Files (2)",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "17.6 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Apps",
            IsExpanded = false,
            IconGlyph = "\uE8B7",
            Children =
            [
                new TaskProcessRow
                {
                    Name = "rSearch.exe (32 bit)",
                    Status = "Efficiency mode",
                    Cpu = "0%",
                    Memory = "89.7 MB",
                    Disk = "0 MB/s",
                    Network = "0 Mbps",
                    Category = "Apps",
                    IconGlyph = "\uE721"
                },
            ]
        },
        new TaskProcessRow
        {
            Name = "Microsoft Edge (3)",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "1,222.7 MB",
            Disk = "0.1 MB/s",
            Network = "0.1 Mbps",
            Category = "Apps",
            IsExpanded = false,
            IconGlyph = "\uE774",
            Children =
            [
                new TaskProcessRow
                {
                    Name = "GPU Process",
                    Status = string.Empty,
                    Cpu = "0%",
                    Memory = "73.2 MB",
                    Disk = "0 MB/s",
                    Network = "0 Mbps",
                    Category = "Apps",
                    IconGlyph = "\uE943"
                },
                new TaskProcessRow
                {
                    Name = "Utility: Network Service",
                    Status = string.Empty,
                    Cpu = "0%",
                    Memory = "24.5 MB",
                    Disk = "0 MB/s",
                    Network = "0 Mbps",
                    Category = "Apps",
                    IconGlyph = "\uE968"
                },
            ]
        },
        new TaskProcessRow
        {
            Name = "Visual Studio Code",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "3,672.3 MB",
            Disk = "1.3 MB/s",
            Network = "0 Mbps",
            Category = "Apps",
            IsExpanded = false,
            IconGlyph = "\uEA1F"
        },
        new TaskProcessRow
        {
            Name = "Windows Explorer",
            Status = string.Empty,
            Cpu = "0.3%",
            Memory = "182.0 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Apps",
            IconGlyph = "\uE8B7"
        },
        new TaskProcessRow
        {
            Name = "Antimalware Service Executable",
            Status = string.Empty,
            Cpu = "1.1%",
            Memory = "210.8 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Background processes",
            IconGlyph = "\uE74D"
        },
        new TaskProcessRow
        {
            Name = "App Actions",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "9.2 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Background processes",
            IconGlyph = "\uE7C3"
        },
        new TaskProcessRow
        {
            Name = "C/C++ Extension for Visual Studio Code",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "1,838.0 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Background processes",
            IsExpanded = false,
            IconGlyph = "\uE943"
        },
        new TaskProcessRow
        {
            Name = "agency.exe",
            Status = string.Empty,
            Cpu = "0%",
            Memory = "136.2 MB",
            Disk = "0 MB/s",
            Network = "0 Mbps",
            Category = "Background processes",
            IconGlyph = "\uE7BA"
        },
    ];

    private void SyncThemeToggle()
    {
        if (XamlRoot?.Content is not FrameworkElement root)
        {
            return;
        }

        var effectiveTheme = root.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        ThemeToggleSwitch.IsOn = effectiveTheme == ElementTheme.Dark;
    }

    public sealed class ProductRow : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _category = string.Empty;
        private double _price;
        private int _stock;
        private bool _isAvailable;
        private DateTimeOffset _dateAdded;
        private string _rating = "★★★";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public int Stock
        {
            get => _stock;
            set { _stock = value; OnPropertyChanged(nameof(Stock)); }
        }

        public bool IsAvailable
        {
            get => _isAvailable;
            set { _isAvailable = value; OnPropertyChanged(nameof(IsAvailable)); }
        }

        public DateTimeOffset DateAdded
        {
            get => _dateAdded;
            set { _dateAdded = value; OnPropertyChanged(nameof(DateAdded)); }
        }

        public string Rating
        {
            get => _rating;
            set { _rating = value; OnPropertyChanged(nameof(Rating)); }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class ExplorerItemRow
    {
        public string Name { get; set; } = string.Empty;

        public string DateModified { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string GroupLabel { get; set; } = string.Empty;

        public string IconGlyph { get; set; } = "\uE8A5";
    }

    public sealed class TaskProcessRow
    {
        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Cpu { get; set; } = string.Empty;

        public string Memory { get; set; } = string.Empty;

        public string Disk { get; set; } = string.Empty;

        public string Network { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public bool IsExpanded { get; set; }

        public string IconGlyph { get; set; } = "\uE7BA";

        public List<TaskProcessRow> Children { get; set; } = [];
    }
}
