# Incremental loading

`TableView` supports incremental loading (also known as infinite scrolling or lazy loading). When the [`ItemsSource`](xref:WinUI.TableView.TableView.ItemsSource) implements [`ISupportIncrementalLoading`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.isupportincrementalloading), the control automatically requests more items as the user scrolls toward the end of the list. The internal collection view forwards `HasMoreItems` and `LoadMoreItemsAsync` to your source collection.

This is a good fit for data coming from a remote service or database where loading everything upfront would be slow or wasteful.

## Implementing an incremental source

Implement `ISupportIncrementalLoading` on a collection type — typically by deriving from `ObservableCollection<T>` so the view also updates when items are added:

```csharp
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

public class IncrementalProductSource : ObservableCollection<Product>, ISupportIncrementalLoading
{
    private const uint PageSize = 50;
    private const uint MaxItemsCount = 10_000;
    private const int SimulatedDelayMs = 750;
    private int _nextId = 1;

    public event EventHandler<bool>? LoadingStateChanged;

    public bool HasMoreItems => Count < MaxItemsCount;

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return AsyncInfo.Run(async cancellationToken =>
        {
            LoadingStateChanged?.Invoke(this, true);

            try
            {
                // Simulate fetching a page of data from a remote service.
                await Task.Delay(SimulatedDelayMs, cancellationToken);

                for (var i = 0; i < PageSize; i++)
                {
                    Add(CreateItem());
                }

                return new LoadMoreItemsResult { Count = PageSize };
            }
            finally
            {
                LoadingStateChanged?.Invoke(this, false);
            }
        });
    }
}
```

Then assign it as the items source:

```csharp
tableView.ItemsSource = new IncrementalProductSource();
```

The first page is loaded automatically when the control is displayed; subsequent pages are requested as the user scrolls.

## Controlling when items are loaded

The familiar incremental loading properties are declared on `TableView` itself:

```xml
<tv:TableView ItemsSource="{x:Bind Products}"
              IncrementalLoadingTrigger="Edge"
              IncrementalLoadingThreshold="2"
              DataFetchSize="3" />
```

| Property | Description |
|---|---|
| `IncrementalLoadingTrigger` | `Edge` (default) triggers loading when scrolling near the end; `None` disables automatic loading |
| `IncrementalLoadingThreshold` | How close to the end (in pages of visible items) the user must scroll before loading begins |
| `DataFetchSize` | The amount of data to request, in pages of visible items; determines the `count` passed to `LoadMoreItemsAsync` |

## Loading items manually

You can also trigger a load from code, for example behind a "Load more" button:

```csharp
if (tableView.ItemsSource is ISupportIncrementalLoading source && source.HasMoreItems)
{
    await source.LoadMoreItemsAsync(50);
}
```

## Notes

- Sorting and filtering operate on the items loaded so far, not on the full remote dataset. If the user sorts or filters while more pages remain, the result reflects only the loaded items. For server-side data, consider applying sort/filter criteria in your service query instead. See [Sorting](sorting.md) and [Filtering](filtering.md).
- Clipboard copy, select all, and CSV export also include only the loaded items.
- `LoadMoreItemsAsync` is invoked on the UI thread. Perform the actual data fetch asynchronously (as in the example above) to keep the UI responsive.
- Items added by `LoadMoreItemsAsync` must be added on the UI thread since the collection is bound to the view.

## Example

The [sample app](https://github.com/w-ahmad/WinUI.TableView/tree/main/samples/WinUI.TableView.SampleApp) includes an interactive **Incremental Loading** page (`Pages/IncrementalLoadingPage.xaml`) that simulates fetching pages of data from a remote service.

## Related articles

- [Binding data](binding-data.md)
- [Performance guidance](performance.md)
- [Sorting](sorting.md)
- [Filtering](filtering.md)
