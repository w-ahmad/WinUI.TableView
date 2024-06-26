﻿using Microsoft.UI.Xaml;

namespace WinUI.TableView;

public class TableViewTemplateColumn : TableViewColumn
{
    public override FrameworkElement GenerateElement()
    {
        return (FrameworkElement)CellTemplate.LoadContent();
    }

    public override FrameworkElement GenerateEditingElement()
    {
        return EditingTemplate is not null ? (FrameworkElement)EditingTemplate.LoadContent() : (FrameworkElement)CellTemplate.LoadContent();
    }

    public DataTemplate CellTemplate
    {
        get => (DataTemplate)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public DataTemplate EditingTemplate
    {
        get => (DataTemplate)GetValue(EditingTemplateProperty);
        set => SetValue(EditingTemplateProperty, value);
    }

    public static readonly DependencyProperty CellTemplateProperty = DependencyProperty.Register(nameof(CellTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(null));
    public static readonly DependencyProperty EditingTemplateProperty = DependencyProperty.Register(nameof(EditingTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(null));
}
