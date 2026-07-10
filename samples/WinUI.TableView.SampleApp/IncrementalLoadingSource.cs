using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace WinUI.TableView.SampleApp;

/// <summary>
/// An items source that loads items incrementally as the user scrolls,
/// simulating paged data coming from a remote service.
/// </summary>
public partial class IncrementalLoadingSource : ObservableCollection<ExampleModel>, ISupportIncrementalLoading
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

    private ExampleModel CreateItem()
    {
        var firstName = DataFaker.FirstName();
        var lastName = DataFaker.LastName();

        return new ExampleModel
        {
            Id = _nextId++,
            FirstName = firstName,
            LastName = lastName,
            Email = DataFaker.Email(firstName, lastName),
            Gender = DataFaker.Gender(),
            Dob = DataFaker.PastDate(50, new DateOnly(1970, 1, 1)),
            IsActive = DataFaker.Boolean(),
            ActiveAt = DataFaker.TimeOfDay(),
            Department = DataFaker.Department(),
            Designation = DataFaker.JobTitle(),
            Address = DataFaker.Address(),
            Avatar = DataFaker.Avatar()
        };
    }
}
