using System.Collections.ObjectModel;

namespace TableViewNuGetSample.Views
{
    /// <summary>
    /// A simple page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        public ObservableCollection<PersonRow> Rows { get; } =
        [
            new PersonRow
            {
                Id = 1,
                Name = "Ada",
                Department = "Engineering",
                IsActive = true,
                IsExpanded = false,
                Score = 98.5,
                Children =
                [
                    new PersonRow { Id = 11, Name = "Ada - Intern", Department = "Engineering", IsActive = true, Score = 82.2 },
                    new PersonRow { Id = 12, Name = "Ada - Contractor", Department = "Engineering", IsActive = true, Score = 89.4 },
                ]
            },
            new PersonRow { Id = 2, Name = "Grace", Department = "Engineering", IsActive = true, Score = 94.2 },
            new PersonRow
            {
                Id = 3,
                Name = "Linus",
                Department = "Research",
                IsActive = false,
                IsExpanded = false,
                Score = 88.0,
                Children =
                [
                    new PersonRow { Id = 31, Name = "Linus - Fellow", Department = "Research", IsActive = true, Score = 91.3 },
                    new PersonRow
                    {
                        Id = 32,
                        Name = "Linus - Team",
                        Department = "Research",
                        IsActive = true,
                        Score = 87.7,
                        Children =
                        [
                            new PersonRow { Id = 321, Name = "Linus - Team Sub", Department = "Research", IsActive = true, Score = 80.1 },
                        ]
                    },
                ]
            },
            new PersonRow { Id = 4, Name = "Margaret", Department = "Research", IsActive = true, Score = 96.8 },
        ];

        public MainPage()
        {
            this.InitializeComponent();
        }

        public sealed class PersonRow
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public string Department { get; set; } = string.Empty;

            public bool IsActive { get; set; }

            public bool IsExpanded { get; set; } = false;

            public double Score { get; set; }

            public List<PersonRow> Children { get; set; } = [];
        }
    }
}
