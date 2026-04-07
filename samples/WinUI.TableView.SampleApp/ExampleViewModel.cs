using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace WinUI.TableView.SampleApp;

public partial class ExampleViewModel : ObservableObject
{
    public ExampleViewModel()
    {
        foreach (var item in ItemsList)
        {
            Items.Add(item);
            Genders.Add(item.Gender);
            Departments.Add(item.Department);
            Designations.Add(item.Designation);
        }
    }

    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            var startId = 1;
            var startDate = new DateOnly(1970, 1, 1);

            ItemsList.Clear();

            for (var i = 0; i < 1_000; i++)
            {
                var firstName = DataFaker.FirstName();
                var lastName = DataFaker.LastName();
                ItemsList.Add(new ExampleModel
                {
                    Id = startId++,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = DataFaker.Email(firstName, lastName),
                    Gender = DataFaker.Gender(),
                    Dob = DataFaker.PastDate(50, startDate),
                    IsActive = DataFaker.Boolean(),
                    ActiveAt = DataFaker.TimeOfDay(),
                    Department = DataFaker.Department(),
                    Designation = DataFaker.JobTitle(),
                    Address = DataFaker.Address(),
                    Avatar = DataFaker.Avatar()
                });
            }
        });
    }

    public static List<ExampleModel> ItemsList { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ExampleModel> Items { get; set; } = [];

    public SortedSet<string?> Genders { get; set; } = [];

    public SortedSet<string?> Departments { get; set; } = [];

    public SortedSet<string?> Designations { get; set; } = [];

    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }
}
