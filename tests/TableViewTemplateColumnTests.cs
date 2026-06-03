using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewTemplateColumnTests
{
    [UITestMethod]
    public void TableViewTemplateColumn_UsesTemplatesForDisplayEditingAndRefresh()
    {
        var cellTemplate = TableViewColumnTestHelpers.CreateTemplate("display");
        var editingTemplate = TableViewColumnTestHelpers.CreateTemplate("editing");
        var column = new TableViewTemplateColumn
        {
            CellTemplate = cellTemplate,
            EditingTemplate = editingTemplate
        };

        var displayElement = (ContentControl)column.GenerateElement(new TableViewCell(), new ColumnTestItem());
        var editingElement = (ContentControl)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());
        var cell = new TableViewCell();

        column.RefreshElement(cell, new ColumnTestItem());

        Assert.IsFalse(column.CanSort);
        Assert.IsFalse(column.CanFilter);
        Assert.AreSame(cellTemplate, displayElement.ContentTemplate);
        Assert.AreSame(editingTemplate, editingElement.ContentTemplate);
        Assert.IsInstanceOfType<ContentControl>(cell.Content);
        Assert.AreSame(cellTemplate, ((ContentControl)cell.Content).ContentTemplate);
    }

    [UITestMethod]
    public void TableViewTemplateColumn_GenerateEditingElement_FallsBackToDisplayTemplate_WhenEditingTemplateIsMissing()
    {
        var cellTemplate = TableViewColumnTestHelpers.CreateTemplate("display");
        var column = new TableViewTemplateColumn
        {
            CellTemplate = cellTemplate
        };

        var editingElement = (ContentControl)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());

        Assert.AreSame(cellTemplate, editingElement.ContentTemplate);
    }

    [UITestMethod]
    public void TableViewTemplateColumn_UsesTemplateSelectors()
    {
        var displayTemplate = TableViewColumnTestHelpers.CreateTemplate("selected-display");
        var editingTemplate = TableViewColumnTestHelpers.CreateTemplate("selected-edit");
        var column = new TableViewTemplateColumn
        {
            CellTemplateSelector = new ConstantTemplateSelector(displayTemplate),
            EditingTemplateSelector = new ConstantTemplateSelector(editingTemplate)
        };

        var displayElement = (ContentControl)column.GenerateElement(new TableViewCell(), new ColumnTestItem());
        var editingElement = (ContentControl)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());

        Assert.AreSame(displayTemplate, displayElement.ContentTemplate);
        Assert.AreSame(editingTemplate, editingElement.ContentTemplate);
    }
}
