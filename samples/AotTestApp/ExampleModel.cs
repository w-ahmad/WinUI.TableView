using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AotTestApp;

[WinRT.GeneratedBindableCustomProperty]
public partial class Designation : ObservableObject
{
    public Designation(int id, string? title)
    {
        Id = id;
        Title = title;
    }

    [ObservableProperty]
    public partial int Id { get; set; }


    [ObservableProperty]
    public partial string? Title { get; set; }
}

[WinRT.GeneratedBindableCustomProperty]
public partial class User : ObservableObject
{
    [ObservableProperty]
    [Display(ShortName = "First Name")]
    public partial string? FirstName { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Last Name")]
    public partial string? LastName { get; set; }

    [ObservableProperty]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Gender { get; set; }

    [ObservableProperty]
    public partial DateOnly Dob { get; set; }
}

[WinRT.GeneratedBindableCustomProperty]
public partial class ExampleModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Active At")]
    public partial TimeOnly ActiveAt { get; set; }

    [ObservableProperty]
    [Display(ShortName = "Is Active")]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string? Department { get; set; }

    [ObservableProperty]
    public partial Designation? Designation { get; set; }

    [ObservableProperty]
    public partial string? Address { get; set; }

    [ObservableProperty]
    [Display(AutoGenerateField = false)]
    public partial string? Avatar { get; set; }

    public Uri AvatarUrl => new(Avatar ?? string.Empty);

    [ObservableProperty]
    [Display(AutoGenerateField = false)]
    public partial User? User { get; set; }
}
