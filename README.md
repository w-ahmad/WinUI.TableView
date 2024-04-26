# WinUI.TableView
WinUI.TableView is a lightweight and fast data grid control made for WinUI apps. It's derived from ListView so you will experience fluent look and feel in your project. It comes with all the essential features you need, plus extras like an Excel-like column filter, options buttons (for columns and the Table) and easy data export.

[![cd-build](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml/badge.svg)](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml)
[![nuget](https://img.shields.io/nuget/v/WinUI.TableView)](https://www.nuget.org/packages/WinUI.TableView/)

### Features
- __Auto-generating Columns:__ Automatically generate columns based on the data source.
- __Copy row content:__ TableView allows you to copy row content, with the option to include or exclude column headers.
- __Editing:__ Modify cell content directly within the TableView by duble tapping on a cell.
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Editing1.png)
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Editing2.png)
- __Sorting__
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Sorting.png)
- __Excel-like Column Filter:__ A key feature that allows users to filter data within columns, enhancing data exploration and analysis.
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Filter.png)
- __Export fuctionality:__ Built-in export functionality to export data to CSV format. This feature can be enabled by setting the `ShowExportOptions = true`. 				
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Options.png)
Developers also have the flexibility to implement their own export functionality using two public methods provided:	
	- `GetRowsContent(bool includeHeaders, char separator)`: Retrieves the content of all rows in the table.
	- `GetSelectedRowsContent(bool includeHeaders, char separator)`: Retrieves the content of selected rows in the table.

### Available Column Types
1. TableViewTextColumn
1. TableViewCheckBoxColumn
1. TableViewComboBoxColumn
1. TableViewNumberColumn
1. TableViewToggleSwitchColumn
1. TableViewTemplateColumn

### Limitations
- Data Source Limitation: WinUI.TableView only accepts data sources that implement `System.Collections.IList`. This is because WinUI.TableView internally uses own implementaion of [AdvancedCollectionView](https://www.nuget.org/packages/CommunityToolkit.WinUI.Collections) from the [CommunityToolkit](https://github.com/CommunityToolkit/Windows), which relies on IList-based collections.
- Cell Selection: Cell selection is not currently supported but it's planned for the future release.

### Dependencies
- [CommunityToolkit.WinUI.Behaviors](https://www.nuget.org/packages/CommunityToolkit.WinUI.Behaviors/)
- [Microsoft.WindowsAppSDK](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/)

### Contributing
We welcome contributions from the community! If you find any issues or have suggestions for improvements, please submit them through the GitHub issue tracker or consider making a pull request.

### License
This project is licensed under the [MIT License](https://github.com/w-ahmad/WinUI.TableView?tab=MIT-1-ov-file).
