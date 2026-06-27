using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
#if WINDOWS
using WinUI.TableView.Collections;
#endif

namespace WinUI.TableView;

/// <summary>
/// Represents a collection of <see cref="TableViewRowHighlight"/> for a TableView.
/// </summary>
public partial class TableViewRowHighlightsCollection : DependencyObjectCollection<TableViewRowHighlight>
{
    private readonly List<TableViewRowHighlight> _itemsCopy = []; // To keep a copy of the items to unhook removed items

    /// <summary>
    /// Occurs when the collection or one of its highlights changes.
    /// </summary>
    internal event EventHandler? HighlightsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowHighlightsCollection"/> class.
    /// </summary>
    public TableViewRowHighlightsCollection()
    {
        VectorChanged += OnVectorChanged;
    }

    /// <summary>
    /// Handles changes to the underlying vector of <see cref="DependencyObject"/> items.
    /// </summary>
    private void OnVectorChanged(IObservableVector<DependencyObject> sender, IVectorChangedEventArgs args)
    {
        foreach (var highlight in _itemsCopy)
        {
            highlight.Changed -= OnHighlightChanged;
        }

        _itemsCopy.Clear();

        for (var i = 0; i < Count; i++)
        {
            if (this[i] is TableViewRowHighlight highlight)
            {
                _itemsCopy.Add(highlight);
                highlight.Changed += OnHighlightChanged;
            }
        }

        HighlightsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles changes to an individual highlight in the collection.
    /// </summary>
    private void OnHighlightChanged(object? sender, EventArgs e)
    {
        HighlightsChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents a highlight for a single row in a TableView. The colors are applied
/// the same way as the <see cref="TableView.AlternateRowBackground"/> and
/// <see cref="TableView.AlternateRowForeground"/> brushes and take precedence over them.
/// </summary>
public partial class TableViewRowHighlight : DependencyObject
{
    /// <summary>
    /// Occurs when one of the highlight properties changes.
    /// </summary>
    internal event EventHandler? Changed;

    /// <summary>
    /// Gets or sets the index of the row to highlight.
    /// </summary>
    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the highlighted row.
    /// </summary>
    public Brush? Background
    {
        get => (Brush?)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush for the highlighted row.
    /// </summary>
    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Handles changes to the highlight properties.
    /// </summary>
    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewRowHighlight highlight)
        {
            highlight.Changed?.Invoke(highlight, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Identifies the <see cref="Index"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(TableViewRowHighlight), new PropertyMetadata(-1, OnPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="Background"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(TableViewRowHighlight), new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="Foreground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(TableViewRowHighlight), new PropertyMetadata(null, OnPropertyChanged));
}
