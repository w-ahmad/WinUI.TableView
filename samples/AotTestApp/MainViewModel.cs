using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AotTestApp;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        foreach (var item in ItemsList)
        {
            Items.Add(item);
        }
    }

    public static void InitializeItems()
    {
        var startId = 1;
        var startDate = new DateOnly(1970, 1, 1);

        ItemsList.Clear();

        for (var i = 0; i < 1_000; i++)
        {
            var firstName = DataFaker.FirstName();
            var lastName = DataFaker.LastName();
            var email = DataFaker.Email(firstName, lastName);
            var gender = DataFaker.Gender();
            var dob = DataFaker.PastDate(50, startDate);
            var item = new ExampleModel
            {
                Id = startId++,
                IsActive = DataFaker.Boolean(),
                ActiveAt = DataFaker.TimeOfDay(),
                Department = DataFaker.Department(),
                Designation = DataFaker.JobTitle(),
                Address = DataFaker.Address(),
                Avatar = DataFaker.Avatar(),
                User = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Gender = gender,
                    Dob = dob
                },
                DateTimeModel = new DateTimeModel
                {
                    TimeSpan1 = DataFaker.TimeOfDay().ToTimeSpan(),
                    TimeOnly1 = DataFaker.TimeOfDay(),
                    DateTime1 = new DateTime(DataFaker.PastDate(50, dob), DataFaker.TimeOfDay()),
                    DateTimeOffset1 = new DateTimeOffset(DataFaker.PastDate(50, dob), DataFaker.TimeOfDay(), TimeSpan.Zero),
                    DateOnly1 = DataFaker.PastDate(50, dob)
                }
            };
            ItemsList.Add(item);
        }
    }

    public static List<ExampleModel> ItemsList { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ExampleModel> Items { get; set; } = [];

    public List<string> Genders => [.. DataFaker.Genders];

    public List<string> Departments => [.. DataFaker.Departments];

    public List<string> Designations => [.. DataFaker.JobTitles];
    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }
}

