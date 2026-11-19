using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class InboxPage : Page
{
    private string _searchText = string.Empty;

    public InboxPage()
    {
        InitializeComponent();

        tableView.FilterDescriptions.Add(new FilterDescription(nameof(MailMessage.IsRead), IsMatch));
        UpdateUnreadCount();
    }

    public ObservableCollection<MailMessage> Messages { get; } = MailData.Create();

    /// <summary>
    /// The message in the reading pane. The pane's x:Bind bindings are refreshed when it changes.
    /// </summary>
    public MailMessage? Selected { get; private set; }

    private bool IsMatch(object? item)
    {
        if (item is not MailMessage message) return false;

        var inView = viewFilter.SelectedItem?.Text switch
        {
            "Flagged" => message.IsFlagged,
            "All" or null => true,
            // Keep the open message in the unread view after reading has marked it as read.
            _ => !message.IsRead || message == Selected
        };

        return inView
            && (_searchText.Length == 0
                || message.Subject.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || message.Sender.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || message.Preview.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Show(tableView.SelectedItem as MailMessage);
    }

    private void Show(MailMessage? message)
    {
        Selected = message;

        if (message is not null)
        {
            message.IsRead = true;
            receivedText.Text = MailVisuals.LongTime(message.Received);
            flagText.Text = message.IsFlagged ? "Unflag" : "Flag";
            attachmentCard.Visibility = MailVisuals.AttachmentVisibility(message.Attachment);
        }

        reader.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
        emptyState.Visibility = message is null ? Visibility.Visible : Visibility.Collapsed;

        Bindings.Update();
        UpdateUnreadCount();
    }

    private void UpdateUnreadCount()
    {
        var unread = Messages.Count(message => !message.IsRead);

        unreadFilter.Text = unread == 0 ? "Unread" : $"Unread ({unread})";
    }

    private void OnViewFilterChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        tableView?.RefreshFilter();
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchText = sender.Text.Trim();
        tableView.RefreshFilter();
    }

    private void OnFlagClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is MailMessage message)
        {
            ToggleFlag(message);
        }
    }

    private void OnToggleFlagClicked(object sender, RoutedEventArgs e)
    {
        if (Selected is not null)
        {
            ToggleFlag(Selected);
        }
    }

    private void ToggleFlag(MailMessage message)
    {
        message.IsFlagged = !message.IsFlagged;

        if (message == Selected)
        {
            flagText.Text = message.IsFlagged ? "Unflag" : "Flag";
        }
    }

    private void OnMarkUnreadClicked(object sender, RoutedEventArgs e)
    {
        if (Selected is { } message)
        {
            tableView.SelectedItem = null;
            message.IsRead = false;
            UpdateUnreadCount();
        }
    }

    private void OnMarkAllReadClicked(object sender, RoutedEventArgs e)
    {
        foreach (var message in Messages)
        {
            message.IsRead = true;
        }

        UpdateUnreadCount();
        tableView.RefreshFilter();
    }

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (Selected is { } message)
        {
            Messages.Remove(message);
            Show(null);
        }
    }
}
