# Breaking changes: the row hosting rewrite and column virtualization

`TableView` used to derive from `ListView`, which is where its row virtualization, container generation, row
selection storage and the Windows row visual came from. It now derives from `Control` and hosts its rows in an
`ItemsRepeater` with the platform's virtualizing `StackLayout`, creating, recycling and positioning the rows itself.

This is a deliberate, semver-major change. It exists because the base class had become the limit: hit testing during
drag selection scanned every item on every pointer-move frame, row positions were maintained with a visual-tree
transform walk per row per scroll frame, cell selection materialised one entry per selected cell, and grouping had
to lean on `ListView`'s own grouped rendering, which only exists on Windows.

See [Overview](overview.md#architecture) for what replaced it and [Performance guidance](performance.md) for what
that changes in practice.

## What still works unchanged

Everything the control declared itself is untouched, and the members most applications use were re-declared on
`TableView` with the same names and types, so typical XAML and C# keeps compiling:

`ItemsSource`, `Items`, `SelectionMode`, `SelectedItem`, `SelectedIndex`, `SelectedItems`, `SelectedRanges`,
`SelectRange`, `DeselectRange`, `SelectAll`, `DeselectAll`, `SelectionChanged`, `ScrollIntoView`,
`ContainerFromIndex`, `ContainerFromItem`, `IndexFromContainer`, `ItemFromContainer`, `IncrementalLoadingTrigger`,
`IncrementalLoadingThreshold`, `DataFetchSize`, and every `TableView*` property, method and event.

`ScrollViewer.*` attached properties set on a `TableView` also still work, because they are attached properties and
do not depend on the base class.

## What changed

| Member | Was | Now | What to do |
|---|---|---|---|
| `TableView` base class | `ListView` | `Control` | Only matters if you assigned a `TableView` to a `ListView`/`ListViewBase`/`ItemsControl` variable or parameter, or based a `Style` on one |
| `TableViewRow` base class | `ListViewItem` | `ContentControl` | Same; `Content`, `ContentTemplate` and `IsSelected` are all still there. Setting `IsSelected` still selects or deselects the row's item, now through the table's selection, so it follows `SelectionMode` and `SelectionUnit`: a change the table does not allow (selection is off, or `SelectionUnit="Cell"`) is reverted |
| `Items` | `ItemCollection` | `IList<object>` | `Count`, indexing and `IndexOf` are unchanged. For change notifications use `TableView.CollectionView.VectorChanged` instead of `Items.VectorChanged` |
| `CanDragItems`, `CanReorderItems`, `ReorderMode`, `AllowDrop`-driven row reorder, `DragItemsStarting`, `DragItemsCompleted` | inherited from `ListViewBase` | **removed** | Row drag-reorder is not currently supported — see below |
| `ItemTemplate`, `ItemTemplateSelector`, `ItemsPanel`, `ItemContainerStyle`, `ItemContainerStyleSelector`, `ItemContainerTransitions`, `DisplayMemberPath` | inherited | **removed** | The row host builds rows itself. Use `CellStyle`, `ConditionalCellStyles`, `RowDetailsTemplate` or a `TableViewTemplateColumn` |
| `IsItemClickEnabled`, `ItemClick` | inherited | **removed** | Use `RowDoubleTapped`, `CellDoubleTapped`, or `SelectionChanged` |
| `ChoosingItemContainer`, `ContainerContentChanging` | inherited | **removed** | Use `ConditionalCellStyles`, or `CellStyle`, for per-row and per-cell appearance |
| `Header`, `HeaderTemplate`, `Footer`, `FooterTemplate` | inherited | **removed** | Put them outside the `TableView` |
| `SingleSelectionFollowsFocus`, `IsSwipeEnabled`, `ShowsScrollingPlaceholders` | inherited | **removed** | No replacement needed; selection no longer follows focus in any mode |
| `SelectedValue`, `SelectedValuePath` | inherited | **removed** | Read the value from `SelectedItem` |
| `Columns.VisibleColumns` | a fresh list per read | a cached read-only list | It was never meaningful to mutate it; attempting to now throws instead of silently mutating a copy |
| `TableViewGroupHeaderRow` base class | `ListViewHeaderItem` | `ContentControl` | Restyle it with an implicit style targeting `TableViewGroupHeaderRow`. A template of your own needs two changes: the `HorizontalGridLine` part is now a `Border` whose bottom edge draws the line (it was a `Rectangle`), and the pinned look comes from a `PinnedBackground` part driven by a `PinStates` group (`Pinned`, `Unpinned`) with the `TableViewGroupHeaderRowPinnedBackground` resource; a template without them loses the grid line and the opaque sticky background |
| `TableViewAutomationPeer` and `TableViewRowAutomationPeer` base classes | `ListViewAutomationPeer`, `ListViewItemAutomationPeer` | `FrameworkElementAutomationPeer` | Only matters if you derive from these peers. They implement the same patterns themselves (see [Accessibility](accessibility.md)), and `TableViewGroupHeaderRow` now has a peer of its own, `TableViewGroupHeaderRowAutomationPeer` |
| `WinUI.TableView.Primitives.ListViewItemPresenter` | the row chrome on Windows | **removed** | It existed only to be the `ListViewItem`'s presenter. The row's chrome is now part of the `TableViewRow` template; restyle the row with an implicit style targeting `TableViewRow` |
| `GroupStyle` | inherited from `ListViewBase` | declared on `TableView` | XAML populating `<tv:TableView.GroupStyle>` is unchanged. Entries now apply per nesting level (the last one is reused for deeper levels), `DefaultGroupStyle` is used while the collection is empty instead of being copied into it, and `HeaderContainerStyle` is not applied |
| `TableViewCell` default template | had `BackgroundBorder` and `SelectionBorder` parts, and visual state groups for pointer-over, selection and the current cell | has neither | The cell adds a `Border` of each name, behind its content and drawn with the same resources and visuals, the first time it has a visible `Background`, is selected or is current, and styles its `RootBorder` itself while the pointer is over it. This keeps the default template to the four elements every cell needs, which makes a row cheaper to create. A template of your own keeps working the old way: one that has parts of those names keeps drawing them through its `Selected`, `Current` and other visual states, and one that defines `CommonStates` is driven through `PointerOver` and `Normal` as before; the cell adds nothing to either |
| `[WinRT.GeneratedBindableCustomProperty]` on `TableViewRow`, `TableViewCell`, `TableViewRowHeader`, `TableViewGroupInfo` and the column types | present | **removed** | Only matters under Native AOT, and only for a `{Binding}` of your own that reaches a plain property of one of these types, such as `{Binding TableView.RowHeight, RelativeSource={RelativeSource TemplatedParent}}` in a row or cell template, or a path through `Column`, `Cells` or `IsCurrent`. Use `{x:Bind}` or `TemplateBinding`, or bind to the `TableView`'s own dependency properties. `{Binding Key}` and `{Binding Count}` in a group header template keep working: both are dependency properties now. The row sets the heights of its `HeaderGrid` part itself, so a row template of your own can drop the three height bindings on it |
| `TableViewComboBoxColumn` editor | bound to the column's `ItemsSource`, `DisplayMemberPath`, `SelectedValuePath` and `IsEditable` | reads them when the editor is created | A change to one of these while a cell of the column is being edited shows the next time a cell is edited |
| `TableViewRowPresenter` template | `HorizontalGridLine` was a `Rectangle` | it is a `Border` whose bottom edge draws the line | Only matters for a template of your own: change the part to a `Border`, or the rows lose their horizontal grid lines |
| `TableViewTemplateColumn` cells | the `CellTemplate` was built again for every item a row showed | a reused row keeps the built template and gives it the new item as its `DataContext` | Bind everything the template shows. State kept outside a binding, such as a control the user changed that is bound one-way, text typed into a box, or something set in `Loaded`, stays with the row as it scrolls to other items. This is how item containers behave in every virtualizing list |
| `TableViewCheckBoxColumn` cells | used the implicit `CheckBox` style | use a style of the library's that draws the check mark without animating it, since the same check box shows row after row while scrolling | An implicit `CheckBox` style in your app no longer reaches these cells. Set the column's `ElementStyle` to style them |
| `SelectionChanged` arguments | `AddedItems` and `RemovedItems` were always copies | copies when the change covers up to 1,024 items; larger changes report read-only projections over the changed index ranges | Nothing for clicks and keyboard selection. A handler that changes the source, or keeps the lists for later, after a large change such as select all should copy them first |
| `SelectedIndex` and `SelectedItem` | — | follow `SelectionMode` and `SelectionUnit` | Setting either selects nothing while `SelectionMode="None"` or `SelectionUnit="Cell"`, and the property goes back to `-1`/`null`. `SelectedItem` set to an item that is not in the view also goes back to `null` |
| Selection across a view rebuild | cleared by some rebuilds (header sorting, grouping), left on shifted indexes by others | always cleared | Anything that rebuilds the view (sorting, grouping, expanding or collapsing a group, a source reset) clears row and cell selection first, with `SelectionChanged` reporting the removed items. Unloading and reloading the control is not a rebuild: the row selection is kept, and if the source changed in the meantime the items still in it stay selected |

### Row drag-reorder

Row drag and drop was entirely `ListView`'s, with no code in this library — which is why it disappeared with the
base class rather than being ported. Reimplementing it on top of the new row host is tracked as follow-up work.

Reordering items in your own collection and letting the control follow the change still works.

### Grouping

Grouping keeps its public API — `GroupDescriptions`, `GroupStyle`, `DefaultGroupStyle`, `DefaultGroupState`,
`AreStickyGroupHeadersEnabled`, `ShowGroupExpandCollapseButton`, `UngroupAll`, `RefreshGrouping`, the `Grouping` event
and the column header's **Group** commands — but group headers are now rows of the table's own row host rather than
`ListView` group containers. That is what makes them virtualized like any other row, and what lets grouping run on
**every Uno Platform target** instead of only on Windows. A sticky group header is no longer `ItemsStackPanel`
pinning a group's own container: it is a separate header drawn over the top of the rows inside the scroll viewer,
showing the group the first visible row is in once that group's header has scrolled away. It draws an opaque
background (`TableViewGroupHeaderRowPinnedBackground`), and it is left out of the automation tree and the tab
order, so the header among the rows stays the one keyboard and screen reader users reach.

## Column virtualization

Columns are virtualized as well as rows: a row has cells, and the header row has headers, only for the frozen
columns and the scrollable columns in or near the viewport. A table whose columns all fit the viewport is not
affected by any of the following. See [Column virtualization](performance.md#column-virtualization).

| Member | Was | Now | What to do |
|---|---|---|---|
| `TableViewRow.Cells`, `TableViewRowPresenter.Cells` | a cell for every visible column, so `Cells[n]` was the cell of column `n` | the cells that exist, in column order | Do not index it by column. Compare `cell.Column`, or call `ScrollCellIntoView` for the cell of a given column, which also creates it |
| `TableViewHeaderRow.Headers` | a header for every visible column | the headers that exist, in column order | Same |
| `TableViewColumn.HeaderControl` | set for every visible column once the table was loaded | `null` while the column is out of view; the header of the first place for a column that is in `Columns` more than once | Check for `null`. Sort and filter state live on the column, not on the header |
| `TableViewCell` and `TableViewColumnHeader` instances | belonged to one column for their lifetime | are reused for another column when the table scrolls sideways | Do not keep per-column state on the element, or a reference to it across a scroll. A column type of your own has its `GenerateElement` called for a cell that takes over one of its columns |
| `Width="Auto"` columns | measured as soon as the table loaded | measured when they are in view | A column out of view takes a provisional width until it is first scrolled into view. Use fixed widths where a wide table must not shift |
| `ScrollableCellsPanel` part of `TableViewRowPresenter`, `ScrollableHeadersPanel` part of `TableViewHeaderRow` | a horizontal `StackPanel` | a `TableViewCellsPanel` | Only matters for a template of your own. One that keeps a `StackPanel` works as before, with a cell or header for every column; change the part to `TableViewCellsPanel` to virtualize |
| `TableViewRowPresenter.InsertCell`, `TableViewHeaderRow.InsertHeader` | inserted at the column's position among the others | place the element by its column, replace the one the column already has, and the element is removed again when its column is out of view | Nothing for a column in view. Do not use them to keep a cell or header for a column that is out of view |
| Automation: `IGridProvider.GetItem` | reached a cell of any column of a realized row | also creates the cell of a column that is out of view | Nothing. `GetColumnHeaders` returns the headers that exist |

## Fixed along the way

- **Selecting all cells did nothing.** With `SelectionUnit="Cell"` and a multi-select mode, <kbd>Ctrl</kbd>+<kbd>A</kbd>
  built the selection and then silently discarded it. It now selects every cell, as one rectangle.
- **An empty incrementally loading source was never asked for its first page** in some cases; it is now.
- **A lost pointer capture** (alt-tab or a cancelled touch mid-drag) left the table stuck in drag selection and the
  row stuck looking pressed.
- **A row recycled mid-edit** silently dropped the edit and left `IsEditing` true with no cell editing, which
  disabled the corner menu until the control was reloaded. The edit is now committed as the row is released,
  whether scrolling released it or the view was rebuilt by sorting, grouping or a new source.
- **`IGridProvider.GetItem` returned nothing for off-screen rows**, making large tables unreadable to assistive
  technology. It now realizes the row on demand.
- **Row `SelectionItemPattern` bypassed the control's selection logic**, so selecting a row through automation
  ignored `SelectionUnit` and the selection anchor.

## Related articles

- [Overview](overview.md)
- [Performance guidance](performance.md)
- [Grouping](grouping.md)
- [Accessibility](accessibility.md)
