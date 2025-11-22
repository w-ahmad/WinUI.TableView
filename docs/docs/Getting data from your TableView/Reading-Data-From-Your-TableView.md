# Reading data from your TableView
There are many built-in functions to get data from a TableView. Each one has its own use and output. TableView data is often assigned to a `string`, separated by a character of your choice to make it readable and easy to extract and use later. This doc will go through all the methods that can be used to read data from a TableView.

## 1. Using `GetAllContent()`
`GetAllContent()` returns a `string` with all the TableView's content (rows, columns, optionally headers) separated with a specified character.
```cs
string data = TV.GetAllContent(true, ',');
```
It takes 2 arguments:
1. A `bool` that specifies if the column's *headers* (names) should be added as part of the extracted data.
2. A `char` (single character) that will be the character that will separate each cell.

Therefore if we have a TableView like this:
| Title       | Description                                |
| ----------- | ------------------------------------------ |
| Fix bug #43 | App crashes when collapsing NavigationView |
| Review PR   | Review the pull request by Georgios1999    |

The output would be:
```
Title,Description
Fix bug #43,App crashes when collapsing NavigationView
Review PR,Review the pull request by Georgios1999
```

## 2. Using `GetRowsContent()`
`GetRowsContent()` returns a `string` with all the content of the specified row(s) separated with a specified character.
```cs
string data = TV.GetRowsContent([0], false, ',');
```
It takes 3 arguments:
1. An `int[]` (array of integer numbers (so it can be more than one)) that specified which rows to read. For example `[0, 1]` will read the first two rows.
2. A `bool` that specifies if the column's *headers* (names) should be added as part of the extracted data.
3. A `char` (single character) that will be the character that will separate each cell.

Therefore if we have a TableView like this:
| Title       | Description                                |
| ----------- | ------------------------------------------ |
| Fix bug #43 | App crashes when collapsing NavigationView |
| Review PR   | Review the pull request by Georgios1999    |

The output would be:
```
Fix bug #43,App crashes when collapsing NavigationView
```

## 3. Using `GetCellsContent()`
`GetCellsContent()` returns a `string` with the content of the specified cell(s) separated with a specified character.
```cs
IEnumerable<TableViewCellSlot> slot = new[] { new TableViewCellSlot { Row = 0, Column = 1 } };
string data = TV.GetCellsContent(slot, false, ',');
```
It takes 3 arguments:
1. An `TableViewCellSlot`, which defines the slot of the column to read from.
2. A `bool` that specifies if the column's *headers* (names) should be added as part of the extracted data.
3. A `char` (single character) that will be the character that will separate each cell.

Therefore if we have a TableView like this:
| Title       | Description                                |
| ----------- | ------------------------------------------ |
| Fix bug #43 | App crashes when collapsing NavigationView |
| Review PR   | Review the pull request by Georgios1999    |

The output would be:
```
App crashes when collapsing NavigationView
```

## 4. Using `GetSelectedContent()`
`GetSelectedContent()` returns a `string` with the content of the items the user has selected with their cursor.
```cs
string data = TV.GetSelectedContent(true, ',')
```
It takes 3 arguments:
1. A `bool` that specifies if the column's *headers* (names) should be added as part of the extracted data.
2. A `char` (single character) that will be the character that will separate each cell.

## Honorable Mentions
### Extracting to a CSV
TableView can extract its contents to a CSV file to view in programs like Excel. This can be done using the corner button. See it in action in the [Samples app](https://github.com/w-ahmad/WinUI.TableView.SampleApp).

### Extracting from your set data (Not recommended)
**This approach is not recommended since it is unnecessarily complicated and might not work well for dynamic columns.** If you have set your columns up using code-behind like the approach mentioned [here](../Adding%20data%20to%20your%20TableView/Assigning-Columns-From-A-Collection.md). We're going to use the `People` example from there:
```cs
string data = People[0].Name.ToString() + "," + People[0].Age.ToString() + "," + People[0].IsActive.ToString();
```