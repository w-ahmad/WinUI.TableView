using System;
using System.Collections.Generic;

namespace WinUI.TableView.SampleApp;

/// <summary>
/// A native AOT-compatible data faker for generating sample data.
/// </summary>
public static class DataFaker
{
    private static readonly Random _random = new();

    // First names
    private static readonly string[] FirstNames =
    [
        "James", "Mary", "Robert", "Patricia", "Michael", "Jennifer", "William", "Linda", "David", "Barbara",
        "Richard", "Elizabeth", "Joseph", "Susan", "Thomas", "Jessica", "Charles", "Sarah", "Christopher", "Karen",
        "Daniel", "Nancy", "Matthew", "Lisa", "Anthony", "Betty", "Mark", "Margaret", "Donald", "Sandra",
        "Steven", "Ashley", "Paul", "Kimberly", "Andrew", "Donna", "Joshua", "Carol", "Kenneth", "Michelle",
        "Kevin", "Emily", "Brian", "Melissa", "George", "Deborah", "Edward", "Stephanie", "Ronald", "Rebecca",
        "Timothy", "Sharon", "Jason", "Brenda", "Jeffrey", "Amber", "Ryan", "Anna", "Jacob", "Pamela",
        "Gary", "Nicole", "Nicholas", "Emma", "Eric", "Helen", "Jonathan", "Samantha", "Stephen", "Katherine"
    ];

    // Last names
    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Peterson", "Phillips", "Campbell",
        "Parker", "Evans", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy", "Cook",
        "Rogers", "Morgan", "Peterson", "Cooper", "Reed", "Bell", "Gomez", "Murray", "Freeman", "Wells",
        "Webb", "Simpson", "Stevens", "Tucker", "Porter", "Hunter", "Hicks", "Crawford", "Henry", "Boyd"
    ];

    // Job titles
    private static readonly string[] JobTitles =
    [
        "Software Developer", "Manager", "Sales Representative", "Accountant", "Analyst",
        "Engineer", "Designer", "Teacher", "Consultant", "Executive",
        "Administrator", "Coordinator", "Director", "Supervisor", "Specialist",
        "Technician", "Operator", "Clerk", "Assistant", "Officer",
        "Agent", "Associate", "Architect", "Planner", "Scientist",
        "Researcher", "Programmer", "Nurse", "Doctor", "Lawyer",
        "Marketing Manager", "Product Manager", "Business Analyst", "Data Scientist", "DevOps Engineer"
    ];

    // Department names
    private static readonly string[] Departments =
    [
        "Sales", "Marketing", "Engineering", "Finance", "Human Resources",
        "Operations", "IT", "Legal", "Research", "Development",
        "Quality Assurance", "Customer Service", "Production", "Logistics", "Planning",
        "Maintenance", "Administration", "Strategic Planning", "Corporate Communications", "Treasury"
    ];

    // Street suffixes
    private static readonly string[] StreetSuffixes =
    [
        "Street", "Avenue", "Road", "Boulevard", "Drive", "Lane", "Court", "Circle",
        "Way", "Parkway", "Plaza", "Square", "Trail", "Ridge", "Hill", "Oak"
    ];

    // Cities
    private static readonly string[] Cities =
    [
        "New York", "Los Angeles", "Chicago", "Houston", "Phoenix",
        "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose",
        "Austin", "Jacksonville", "Seattle", "Denver", "Boston",
        "Portland", "Miami", "Atlanta", "Las Vegas", "Detroit"
    ];

    // States
    private static readonly string[] States =
    [
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY"
    ];

    private static readonly string[] Regions = [
        "East", "West", "North", "South"
    ];

    // Genders
    private static readonly string[] Genders = ["Male", "Female", "Non-binary", "Genderfluid", "Agender", "Bigender", "Genderqueer", "Two-Spirit", "Prefer not to say"];

    // Avatar images (using placeholder service URLs)
    private static readonly string[] AvatarSeeds =
    [
        "avatar1", "avatar2", "avatar3", "avatar4", "avatar5",
        "avatar6", "avatar7", "avatar8", "avatar9", "avatar10"
    ];

    public static string FirstName()
    {
        return FirstNames[_random.Next(FirstNames.Length)];
    }

    public static string LastName()
    {
        return LastNames[_random.Next(LastNames.Length)];
    }

    public static string FullName()
    {
        return $"{FirstName()} {LastName()}";
    }

    public static string Email(string? firstName = null, string? lastName = null)
    {
        firstName ??= FirstName();
        lastName ??= LastName();
        var domains = new[] { "example.com", "test.com", "sample.com", "data.com" };
        var domain = domains[_random.Next(domains.Length)];
        return $"{firstName.ToLower()}.{lastName.ToLower()}@{domain}";
    }

    public static string Gender()
    {
        return Genders[_random.Next(Genders.Length)];
    }

    public static DateOnly PastDate(int yearsBack = 50, DateOnly? maxDate = null)
    {
        maxDate ??= DateOnly.FromDateTime(DateTime.Now);
        var startDate = maxDate.Value.AddYears(-yearsBack);
        var daysRange = maxDate.Value.DayNumber - startDate.DayNumber;
        var randomDays = _random.Next(daysRange);
        return startDate.AddDays(randomDays);
    }

    public static TimeOnly TimeOfDay()
    {
        var hours = _random.Next(0, 24);
        var minutes = _random.Next(0, 60);
        var seconds = _random.Next(0, 60);
        return new TimeOnly(hours, minutes, seconds);
    }

    public static bool Boolean(float truePercentage = 0.5f)
    {
        return _random.NextSingle() < truePercentage;
    }

    public static int Integer(int min = 0, int max = int.MaxValue)
    {
        return _random.Next(min, max);
    }

    public static decimal Decimal(decimal min = 0, decimal max = 1000)
    {
        return ((decimal)_random.NextDouble() * (max - min)) + min;
    }

    public static string Department()
    {
        return Departments[_random.Next(Departments.Length)];
    }

    public static string JobTitle()
    {
        return JobTitles[_random.Next(JobTitles.Length)];
    }

    public static string Address()
    {
        var streetNumber = _random.Next(1, 9999);
        var streetName = FirstName();
        var suffix = StreetSuffixes[_random.Next(StreetSuffixes.Length)];
        var city = Cities[_random.Next(Cities.Length)];
        var state = States[_random.Next(States.Length)];
        var zip = _random.Next(10000, 99999);
        return $"{streetNumber} {streetName} {suffix}, {city}, {state} {zip}";
    }

    public static string Avatar()
    {
        var seed = AvatarSeeds[_random.Next(AvatarSeeds.Length)];
        var id = _random.Next(1, 100);
        return $"https://api.dicebear.com/7.x/avataaars/svg?seed={seed}{id}";
    }

    public static string ZipCode()
    {
        return _random.Next(10000, 99999).ToString();
    }

    public static string State()
    {
        return States[_random.Next(States.Length)];
    }

    public static string Region()
    {
        return Regions[_random.Next(Regions.Length)];
    }

    public static string City()
    {
        return Cities[_random.Next(Cities.Length)];
    }
}
