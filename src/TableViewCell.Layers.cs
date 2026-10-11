using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace WinUI.TableView;

public partial class TableViewCell
{
    private const string SelectionLayerName = "SelectionBorder";
    private const string BackgroundLayerName = "BackgroundBorder";

    [ThreadStatic]
    private static ResourceDictionary? t_layerResources;

    [ThreadStatic]
    private static Thickness? t_selectionBorderThickness;

    /// <summary>
    /// Gets the thickness of the border a selected or current cell draws around itself, which the cell makes room for
    /// when it is as wide as its content needs whether or not the border is there yet.
    /// </summary>
    private Thickness SelectionBorderThickness
    {
        get
        {
            if (_selectionBorder is not null)
            {
                return _selectionBorder.BorderThickness;
            }

            // A template that has a root for the layers but not the selection layer gets one of the default thickness.
            return _layersRoot is not null ? t_selectionBorderThickness ??= GetDefaultSelectionBorderThickness() : default;
        }
    }

    /// <summary>
    /// Shows the cell's background in a layer of its own behind its content.
    /// </summary>
    /// <remarks>
    /// The layer is added the first time the cell has a background that can be seen. A cell has none by default, and
    /// most never do, so most do not have the layer.
    /// </remarks>
    private void UpdateBackgroundLayer()
    {
        if (_layersRoot is null || _templateHasBackgroundLayer)
        {
            return;
        }

        var background = Background;

        if (!IsVisibleFill(background))
        {
            if (_backgroundBorder is not null)
            {
                _backgroundBorder.Background = null;
            }

            return;
        }

        _backgroundBorder ??= AddLayer(BackgroundLayerName, "TableViewCellBackgroundLayerStyle", 0);
        _backgroundBorder.Background = background;
    }

    /// <summary>
    /// Shows whether the cell is selected and whether it is the current cell, in a layer of its own behind its content.
    /// </summary>
    /// <remarks>
    /// The layer is added the first time the cell is selected or current, and kept, so it is added to the cells that
    /// have been, not to every cell the table shows.
    /// </remarks>
    private void UpdateSelectionLayer()
    {
        if (_layersRoot is null || _templateHasSelectionLayer)
        {
            return;
        }

        var isSelected = _appliedSelected is true;
        var isCurrent = _appliedCurrent is true;

        if (_selectionBorder is null)
        {
            if (!isSelected && !isCurrent)
            {
                return;
            }

            // Behind the content and in front of the background.
            _selectionBorder = AddLayer(SelectionLayerName, null, _rootBorder is not null ? Math.Max(0, _layersRoot.Children.IndexOf(_rootBorder)) : 0);
        }

        _selectionBorder.Style = GetLayerStyle((isSelected, isCurrent) switch
        {
            (true, true) => "TableViewCellSelectedCurrentLayerStyle",
            (true, false) => "TableViewCellSelectedLayerStyle",
            (false, true) => "TableViewCellCurrentLayerStyle",
            _ => "TableViewCellSelectionLayerStyle"
        });
    }

    private Border AddLayer(string name, string? styleKey, int index)
    {
        var layer = new Border { Name = name };

        if (styleKey is not null)
        {
            layer.Style = GetLayerStyle(styleKey);
        }

        _layersRoot!.Children.Insert(index, layer);

        // During a resize preview the layers are arranged wider than the cell, which only happens when it is arranged.
        if (_resizePreviewActive)
        {
            InvalidateArrange();
        }

        return layer;
    }

    /// <summary>
    /// Gets whether a brush paints anything, since the default style gives a cell a brush that does not.
    /// </summary>
    private static bool IsVisibleFill(Brush? brush)
    {
        return brush switch
        {
            null => false,
            SolidColorBrush solid => solid.Opacity > 0d && solid.Color.A > 0,
            _ => true
        };
    }

    /// <summary>
    /// Gets one of the styles of the layers a cell adds. They are loaded once per UI thread, since a style belongs to
    /// the thread that loaded it.
    /// </summary>
    private static Style? GetLayerStyle(string key)
    {
        t_layerResources ??= new ResourceDictionary { Source = new Uri("ms-appx:///WinUI.TableView/Themes/TableViewCellLayers.xaml") };

        return t_layerResources[key] as Style;
    }

    private static Thickness GetDefaultSelectionBorderThickness()
    {
        // What the app overrode, else what the default theme says.
        return Application.Current?.Resources.TryGetValue("TableViewCellSelectionBorderThickness", out var value) is true && value is Thickness thickness
            ? thickness
            : new Thickness(2);
    }
}
