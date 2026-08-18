# Breaking changes: the row hosting rewrite

`TableView` used to derive from `ListView`, which is where its row virtualization, container generation, row
selection storage and the Windows row visual came from. It now derives from `Control` and hosts its rows in an
`ItemsRepeater` with a virtualizing layout it owns.

This is a deliberate, semver-major change. It exists because the base class had become the limit: hit testing during
drag selection scanned every item on every pointer-move frame, row positions were maintained with a visual-tree
transform walk per row per scroll frame, cell selection materialised one entry per selected cell, and grouping was
impossible because the control assumed the row you see and the item you have are the same index.

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
| `TableViewRow` base class | `ListViewItem` | `ContentControl` | Same; `Content`, `ContentTemplate` and `IsSelected` are all still there |
| `Items` | `ItemCollection` | `IList<object>` | `Count`, indexing and `IndexOf` are unchanged. For change notifications use `TableView.CollectionView.VectorChanged` instead of `Items.VectorChanged` |
| `CanDragItems`, `CanReorderItems`, `ReorderMode`, `AllowDrop`-driven row reorder, `DragItemsStarting`, `DragItemsCompleted` | inherited from `ListViewBase` | **removed** | Row drag-reorder is not currently supported — see below |
| `ItemTemplate`, `ItemTemplateSelector`, `ItemsPanel`, `ItemContainerStyle`, `ItemContainerStyleSelector`, `ItemContainerTransitions`, `DisplayMemberPath` | inherited | **removed** | The row host builds rows itself. Use `CellStyle`, `ConditionalCellStyles`, `RowDetailsTemplate` or a `TableViewTemplateColumn` |
| `IsItemClickEnabled`, `ItemClick` | inherited | **removed** | Use `RowDoubleTapped`, `CellDoubleTapped`, or `SelectionChanged` |
| `ChoosingItemContainer`, `ContainerContentChanging` | inherited | **removed** | Use `ConditionalCellStyles`, or `CellStyle`, for per-row and per-cell appearance |
| `Header`, `HeaderTemplate`, `Footer`, `FooterTemplate` | inherited | **removed** | Put them outside the `TableView` |
| `SingleSelectionFollowsFocus`, `IsSwipeEnabled`, `ShowsScrollingPlaceholders` | inherited | **removed** | No replacement needed; selection no longer follows focus in any mode |
| `SelectedValue`, `SelectedValuePath` | inherited | **removed** | Read the value from `SelectedItem` |
| `Columns.VisibleColumns` | a fresh list per read | a cached read-only list | It was never meaningful to mutate it; attempting to now throws instead of silently mutating a copy |

### Row drag-reorder

Row drag and drop was entirely `ListView`'s, with no code in this library — which is why it disappeared with the
base class rather than being ported. Reimplementing it on top of the new row host is tracked as follow-up work. The
Row Reorder sample page documents the gap.

Reordering items in your own collection and letting the control follow the change still works.

## Fixed along the way

- **Selecting all cells did nothing.** With `SelectionUnit="Cell"` and a multi-select mode, <kbd>Ctrl</kbd>+<kbd>A</kbd>
  built the selection and then silently discarded it. It now selects every cell, as one rectangle.
- **An empty incrementally loading source was never asked for its first page** in some cases; it is now.
- **A lost pointer capture** (alt-tab or a cancelled touch mid-drag) left the table stuck in drag selection and the
  row stuck looking pressed.
- **A row recycled mid-edit** silently dropped the edit and left `IsEditing` true with no cell editing, which
  disabled the corner menu until the control was reloaded. The edit is now committed as the row is released.
- **`IGridProvider.GetItem` returned nothing for off-screen rows**, making large tables unreadable to assistive
  technology. It now realizes the row on demand.
- **Row `SelectionItemPattern` bypassed the control's selection logic**, so selecting a row through automation
  ignored `SelectionUnit` and the selection anchor.

## Related articles

- [Overview](overview.md)
- [Performance guidance](performance.md)
- [Grouping](grouping.md)
- [Accessibility](accessibility.md)
