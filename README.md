# WinUI.TableView
WinUI.TableView is a lightweight and fast data grid control made for WinUI apps. It's derived from ListView so you will experience fluent look and feel in your project. It comes with all the essential features you need, plus extras like an Excel-like column filter, options buttons (for columns and the Table) and easy data export.

[![cd-build](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml/badge.svg)](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml)
[![nuget](https://img.shields.io/nuget/v/WinUI.TableView)](https://www.nuget.org/packages/WinUI.TableView/)

### Features
- __Sorting:__ Easily sort data by clicking on column headers or from column options menu.
- __Auto-generating Columns:__ Automatically generate columns based on the data source.
- __Editing:__ Modify cell content directly within the TableView by duble tapping on a cell.
- __Copy:__ Copy row content, with the option to include or exclude column headers.
- __Excel-like Column Filter:__ A key feature that allows users to filter data within columns, enhancing data exploration and analysis.
- __Export fuctionality:__ Built-in export functionality to export data to CSV format. This feature can be enabled by setting the `ShowExportOptions = true`. 				
Developers also have the flexibility to implement their own export functionality using two public methods provided:	
	- `GetRowsContent(bool includeHeaders, char separator)`: Retrieves the content of all rows in the table.
	- `GetSelectedRowsContent(bool includeHeaders, char separator)`: Retrieves the content of selected rows in the table.
- __Control-wide Option Button:__ Provides quick access to various control-wide actions such as:	
  - Select All
  - Deselect All
  - Copy <sub>(Copy selected rows content to clipboard)</sub>
  - Copy with Headers <sub>(Copy selected rows content including headers to clipboard)</sub>
  - Clear sorting <sub>(Enabled only when any column is sorted)</sub>
  - Clear filtering <sub>(Enabled only when any column is filterd)</sub>
  - Export All to CSV <sub>(Visible only if enabled)</sub>
  - Export Selected to CSV	<sub>(Visible only if enabled)</sub>

### Examples
- Editing
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Editing1.png)
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Editing2.png)

- Sorting
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Sorting.png)

- Excel like column Filter
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Filter.png)

- Options button with Export options enabled
![image](https://raw.githubusercontent.com/w-ahmad/WinUI.TableView/main/screenshots/Options.png)

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
- [CommunityToolkit.WinUI.Controls.Sizers](https://www.nuget.org/packages/CommunityToolkit.WinUI.Controls.Sizers/)
- [CommunityToolkit.WinUI.Converters](https://www.nuget.org/packages/CommunityToolkit.WinUI.Converters/)
- [CommunityToolkit.WinUI.Helpers](https://www.nuget.org/packages/CommunityToolkit.WinUI.Helpers/)
- [Microsoft.WindowsAppSDK](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/)
- [WinUIEx](https://www.nuget.org/packages/WinUIEx/)

### Contributing
We welcome contributions from the community! If you find any issues or have suggestions for improvements, please submit them through the GitHub issue tracker or consider making a pull request.

### License
This project is licensed under the [MIT License](https://github.com/w-ahmad/WinUI.TableView?tab=MIT-1-ov-file).
