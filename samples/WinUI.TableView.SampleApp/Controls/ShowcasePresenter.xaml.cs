#if WINDOWS
using ColorCode;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp.Controls;

/// <summary>
/// Hosts a showcase and lets the reader flip between the running sample and its real source files,
/// which the project embeds as resources so the code shown is always the code that runs.
/// </summary>
public sealed partial class ShowcasePresenter : UserControl
{
    private const string _repositoryUri = "https://github.com/w-ahmad/WinUI.TableView/tree/main/samples/WinUI.TableView.SampleApp/";
    private readonly Dictionary<string, FrameworkElement> _codeViews = [];
    private bool _isLoaded;

    public ShowcasePresenter()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Semicolon separated source file names, e.g. "SalesPipelinePage.xaml;SalesPipelinePage.xaml.cs;Deal.cs".
    /// </summary>
    public string? Sources { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        var pageFolder = SampleCatalog.Find(FindParentPage()?.GetType() ?? typeof(Page))?.Folder ?? "Pages";

        foreach (var file in (Sources ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var isPageFile = file.Contains(".xaml", StringComparison.OrdinalIgnoreCase);
            var label = file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ? "XAML"
                      : file.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) ? "C#"
                      : file;

            viewSelector.Items.Add(new SelectorBarItem { Text = label, Tag = file });
            sourceLinks.Children.Add(new HyperlinkButton
            {
                Content = file,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                NavigateUri = new Uri($"{_repositoryUri}{(isPageFile ? pageFolder : "Showcases")}/{file}")
            });
        }
    }

    private Page? FindParentPage()
    {
        var parent = VisualTreeHelper.GetParent(this);

        while (parent is not null and not Page)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as Page;
    }

    private void OnViewSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var file = sender.SelectedItem?.Tag as string;

        exampleContainer.Visibility = file is null ? Visibility.Visible : Visibility.Collapsed;
        codeContainer.Visibility = file is null ? Visibility.Collapsed : Visibility.Visible;

        if (file is null) return;

        if (!_codeViews.TryGetValue(file, out var view))
        {
            _codeViews[file] = view = CreateCodeView(ReadSource(file), file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));
        }

        codeScroller.Content = view;
        codeScroller.ChangeView(0, 0, null, true);
    }

    private static string ReadSource(string file)
    {
        using var stream = typeof(ShowcasePresenter).Assembly.GetManifestResourceStream($"Source/{file}");

        if (stream is null) return $"// {file} is not embedded in this build.";

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Replace("\r\n", "\n").TrimEnd();
    }

    private static FrameworkElement CreateCodeView(string code, bool isXaml)
    {
#if WINDOWS
        var textBlock = new RichTextBlock
        {
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 13,
            IsTextSelectionEnabled = true
        };

        new RichTextBlockFormatter(ThemeHelper.ActualTheme).FormatRichTextBlock(code, isXaml ? Languages.Xml : Languages.CSharp, textBlock);
#else
        var textBlock = new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            IsTextSelectionEnabled = true
        };
#endif
        return textBlock;
    }

    private void OnToggleThemeClicked(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        exampleContainer.RequestedTheme = exampleContainer.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
        themeBackground.Visibility = exampleContainer.ActualTheme != ThemeHelper.ActualTheme ? Visibility.Visible : Visibility.Collapsed;
#else
        if (App.Current.MainWindow.Content is FrameworkElement root)
        {
            root.RequestedTheme = root.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
        }
#endif
    }

    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? Example
    {
        get => GetValue(ExampleProperty);
        set => SetValue(ExampleProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(ShowcasePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(ShowcasePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty ExampleProperty = DependencyProperty.Register(nameof(Example), typeof(object), typeof(ShowcasePresenter), new PropertyMetadata(null));
}
