using System;
using System.ComponentModel;

namespace WinUI.TableView;

public class TableViewAutoGeneratingColumnEventArgs : CancelEventArgs
{
    public TableViewAutoGeneratingColumnEventArgs(string propertyName, Type propertyType, TableViewColumn column)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        Column = column;
    }

    public string PropertyName { get; }
    public Type PropertyType { get; }
    public TableViewColumn Column { get; set; }
}
