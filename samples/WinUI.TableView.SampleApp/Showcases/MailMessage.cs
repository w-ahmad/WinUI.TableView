using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Globalization;
using Windows.UI.Text;

namespace WinUI.TableView.SampleApp.Showcases;

#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class MailMessage : ObservableObject
{
    public string Sender { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Preview { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Attachment { get; set; }

    public DateTime Received { get; set; }

    [ObservableProperty]
    public partial bool IsRead { get; set; }

    [ObservableProperty]
    public partial bool IsFlagged { get; set; }

    /// <summary>
    /// Has a filtered view re-evaluate the message, as when its read state changes.
    /// </summary>
    public void Reevaluate() => OnPropertyChanged(nameof(IsRead));
}

/// <summary>
/// How a message looks in the list: weights, colors, glyphs and the short received time.
/// </summary>
public static class MailVisuals
{
    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    public static FontWeight Weight(bool isRead) => isRead ? FontWeights.Normal : FontWeights.SemiBold;

    public static Visibility UnreadVisibility(bool isRead) => isRead ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility AttachmentVisibility(string? attachment) => attachment is null ? Visibility.Collapsed : Visibility.Visible;

    public static Brush FlagBrush(bool isFlagged) => Palette.Solid(isFlagged ? Palette.Red : Palette.Gray);

    public static double FlagOpacity(bool isFlagged) => isFlagged ? 1 : 0.45;

    public static Brush AvatarBrush(string sender) => Palette.Avatar(sender.Length * 5 + sender.FirstOrDefault());

    public static Brush CategoryBrush(string category) => Palette.Solid(CategoryColor(category));

    public static Brush CategoryTint(string category) => Palette.Tint(CategoryColor(category));

    private static Windows.UI.Color CategoryColor(string category) => category switch
    {
        "Customers" => Palette.Blue,
        "Team" => Palette.Purple,
        "Finance" => Palette.Green,
        "Travel" => Palette.Teal,
        _ => Palette.Amber
    };

    /// <summary>
    /// Time for today, weekday for the past week, date before that - the way mail clients do it.
    /// </summary>
    public static string ShortTime(DateTime received)
    {
        var days = (MailData.Now.Date - received.Date).Days;

        return days switch
        {
            0 => received.ToString("h:mm tt", _culture),
            < 7 => received.ToString("ddd h:mm tt", _culture),
            _ => received.ToString("MMM d", _culture)
        };
    }

    public static string LongTime(DateTime received) => received.ToString("dddd, MMMM d, yyyy 'at' h:mm tt", _culture);
}

public static class MailData
{
    public static readonly DateTime Now = new(2026, 11, 19, 10, 45, 0);

    private static readonly (string Sender, string Category, string Subject, string Preview, string? Attachment)[] _templates =
    [
        ("Emma Walsh", "Customers", "Fourth Coffee renewal: revised quote", "Attached is the revised quote with the three-year discount we discussed on Tuesday.", "Fourth Coffee quote v3.pdf"),
        ("Diego Morales", "Team", "Sprint review moved to Thursday", "The room was double-booked, so the review now starts at 2 PM in Cascade. Agenda is unchanged.", null),
        ("Woodgrove Bank", "Finance", "Your October statement is ready", "Your statement for the period ending October 31 is now available in online banking.", "Statement-2026-10.pdf"),
        ("Aisha Khan", "Customers", "Litware landing zone: go-live checklist", "Two items left before we can sign off: the firewall change window and the DNS cutover plan.", "Go-live checklist.xlsx"),
        ("Blue Yonder Airlines", "Travel", "Your trip to Seattle is confirmed", "Flight BY 482 departs Monday at 7:15 AM. Check-in opens 24 hours before departure.", "Itinerary.pdf"),
        ("Ravi Iyer", "Team", "Design review notes: grouping headers", "Summary of today's review. Main decision: sticky headers stay on by default.", null),
        ("Priya Raman", "Customers", "Munson's Pickles wants a pilot", "They would like to start with 40 seats in the Portland office before the wider rollout.", null),
        ("GitHub", "Updates", "[WinUI.TableView] Pull request merged", "Fix header row padding was merged into main. 3 checks passed.", null),
        ("Hannah Schultz", "Team", "Lunch on Friday?", "A few of us are heading to the new place on 5th. Want to join?", null),
        ("Northwind Traders", "Finance", "Invoice INV-20418 is due in 7 days", "This is a reminder that invoice INV-20418 for $4,250.00 is due on November 26.", "INV-20418.pdf"),
        ("Kwame Mensah", "Customers", "Proseware: security questionnaire", "Their security team sent 212 questions. I filled in what I could; the rest needs engineering.", "Questionnaire.xlsx"),
        ("Sofia Rossi", "Team", "Out of office next week", "I'm away Monday through Wednesday. Jun is covering the Tailspin account.", null),
        ("Alpine Ski House", "Travel", "Booking confirmation #AS-77104", "Thanks for booking. Two nights, mountain view, breakfast included.", null),
        ("Jun Bae", "Customers", "Contoso seat expansion signed", "Signed order form is in. 610 seats, starting December 1. Great work, everyone.", "Order form (signed).pdf"),
        ("NuGet Gallery", "Updates", "WinUI.TableView 1.5.2 was published", "Your package has been listed and is now available for download.", null),
    ];

    public static ObservableCollection<MailMessage> Create()
    {
        var random = new Random(715);
        var messages = new ObservableCollection<MailMessage>();
        var received = Now.AddMinutes(-9);

        for (var i = 0; i < 140; i++)
        {
            var (sender, category, subject, preview, attachment) = _templates[random.Next(_templates.Length)];

            messages.Add(new MailMessage
            {
                Sender = sender,
                SenderEmail = $"{sender.ToLowerInvariant().Replace(' ', '.').Replace("'", string.Empty)}@example.com",
                Category = category,
                Subject = i < _templates.Length ? subject : (random.Next(3) == 0 ? $"RE: {subject}" : subject),
                Preview = preview,
                Body = $"Hi,\n\n{preview}\n\nLet me know if anything looks off or if you would like to walk through it together. I'm around most of this week.\n\nThanks,\n{sender.Split(' ')[0]}",
                Attachment = attachment,
                Received = received,
                IsRead = i > 2 && random.NextDouble() < 0.72,
                IsFlagged = random.NextDouble() < 0.12
            });

            received = received.AddMinutes(-random.Next(12, 60 * (1 + i / 6)));
        }

        return messages;
    }
}
