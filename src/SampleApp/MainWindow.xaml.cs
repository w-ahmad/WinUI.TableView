using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SampleApp;

public sealed partial class MainWindow : Window
{
    private readonly List<DataGridDataItem> _items = new();
    private readonly ObservableCollection<string> _mountains = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        var lines = await File.ReadAllLinesAsync("Assets\\mtns.csv");

        foreach (var line in lines)
        {
            var values = line.Split(',');

            _items.Add(
                new DataGridDataItem()
                {
                    Rank = uint.Parse(values[0]),
                    Mountain = values[1],
                    Height_m = uint.Parse(values[2]),
                    Range = values[3],
                    Coordinates = values[4],
                    Prominence = uint.Parse(values[5]),
                    Parent_mountain = values[6],
                    First_ascent = DateTimeOffset.Parse(values[7], CultureInfo.InvariantCulture.DateTimeFormat),
                    Ascents = values[8],
                });
            _mountains.Add(values[1]);
        }

        tableView.ItemsSource = new ObservableCollection<DataGridDataItem>(_items);
    }

    private void OnLoadMoreButtonClick(object sender, RoutedEventArgs e)
    {
        var collection = (ObservableCollection<DataGridDataItem>)tableView.ItemsSource;
        _items.ForEach(collection.Add);
    }

    private void OnClearAndLoadButtonClick(object sender, RoutedEventArgs e)
    {
        tableView.ItemsSource = new ObservableCollection<DataGridDataItem>(_items);
    }
}
