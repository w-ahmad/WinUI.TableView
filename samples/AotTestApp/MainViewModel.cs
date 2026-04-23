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

        Genders = [.. DataFaker.Genders];
        Departments = [.. DataFaker.Departments];
        Designations = [.. DataFaker.JobTitles];
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
            ItemsList.Add(new ExampleModel
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
                }
            });
        }
    }

    public static List<ExampleModel> ItemsList { get; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ExampleModel> Items { get; set; } = [];

    public List<string?> Genders { get; set; } = [];

    public List<string?> Departments { get; set; } = [];

    public List<Designation> Designations { get; set; } = [];

    [ObservableProperty]
    public partial ExampleModel? SelectedItem { get; set; }
}

