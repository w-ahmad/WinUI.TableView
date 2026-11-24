#if WINDOWS
using ColorCode;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp.Controls;

/// <summary>
/// Hosts a sample: its title, the running example with its options, and a switch in the toolbar that
/// swaps the example for its code. The code is the page's <see cref="Xaml"/> and <see cref="CSharp"/>
/// snippets when it has them, otherwise the page's real source files, which the project embeds as resources.
/// </summary>
public sealed partial class SamplePresenter : UserControl
{
    private const string RepositoryUri = "https://github.com/w-ahmad/WinUI.TableView/tree/main/samples/WinUI.TableView.SampleApp/";
    private const string XamlView = "XAML";
    private const string CSharpView = "C#";
    private const string OptionsView = "Options";
    private const int TrivialCodeBehindLines = 14;
    private const double NarrowWidth = 720;
    private static readonly Regex _substitutionPattern = SubstitutionPattern();
    private readonly Dictionary<string, FrameworkElement> _codeViews = [];
    private SelectorBarItem? _optionsItem;
    private bool _isLoaded;
    private bool _isNarrow;

    public SamplePresenter()
    {
        InitializeComponent();

        Substitutions = [];
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        var pageType = FindParentPage()?.GetType();
        var pageFolder = pageType is null ? "Pages" : SampleCatalog.Find(pageType)?.Folder ?? "Pages";

        AddView(XamlView, $"{pageType?.Name}.xaml", pageFolder, !string.IsNullOrWhiteSpace(Xaml));
        AddView(CSharpView, $"{pageType?.Name}.xaml.cs", pageFolder, !string.IsNullOrWhiteSpace(CSharp));

        foreach (var file in (Sources ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            AddView(file, file, "Showcases", false);
        }

        foreach (var substitution in Substitutions)
        {
            substitution.ValueChanged += (_, _) => RefreshCode();
        }

        ApplyLayout();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isNarrow != e.NewSize.Width < NarrowWidth)
        {
            _isNarrow = !_isNarrow;
            ApplyLayout();
        }
    }

    /// <summary>
    /// On a narrow window the toolbar moves under the title and the options become a tab next to the code,
    /// so the example keeps the whole width.
    /// </summary>
    private void ApplyLayout()
    {
        if (!_isLoaded) return;

        root.Padding = _isNarrow ? new Thickness(12, 8, 12, 12) : new Thickness(24, 16, 24, 20);
        root.RowSpacing = _isNarrow ? 8 : 14;
        headerText.Style = (Style)Application.Current.Resources[_isNarrow ? "SubtitleTextBlockStyle" : "TitleTextBlockStyle"];
        descriptionText.MaxLines = _isNarrow ? 3 : 0;

        Grid.SetRow(toolbar, _isNarrow ? 1 : 0);
        Grid.SetColumn(toolbar, _isNarrow ? 0 : 1);
        Grid.SetColumnSpan(toolbar, _isNarrow ? 2 : 1);
        toolbar.HorizontalAlignment = _isNarrow ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;

        Grid.SetColumn(optionsContainer, _isNarrow ? 0 : 1);
        Grid.SetColumnSpan(optionsContainer, _isNarrow ? 2 : 1);
        optionsContainer.Margin = _isNarrow ? default : new Thickness(12, 0, 0, 0);

        var needsOptionsItem = _isNarrow && Options is not null;

        if (needsOptionsItem && _optionsItem is null)
        {
            _optionsItem = new SelectorBarItem { Text = OptionsView, Icon = new FontIcon { Glyph = "" } };
            viewSelector.Items.Insert(1, _optionsItem);
        }
        else if (!needsOptionsItem && _optionsItem is not null)
        {
            if (viewSelector.SelectedItem == _optionsItem)
            {
                viewSelector.SelectedItem = viewSelector.Items[0];
            }

            viewSelector.Items.Remove(_optionsItem);
            _optionsItem = null;
        }

        ShowSelectedView();
    }

    private void AddView(string label, string file, string folder, bool hasSnippet)
    {
        var source = ReadSource(file);

        if (source is not null)
        {
            sourceLinks.Children.Add(new HyperlinkButton
            {
                Content = file,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                NavigateUri = new Uri($"{RepositoryUri}{folder}/{file}")
            });
        }

        // A code-behind that only calls InitializeComponent is not worth a tab.
        var isWorthShowing = hasSnippet || (source is not null && (label != CSharpView || source.Count(c => c == '\n') > TrivialCodeBehindLines));

        if (isWorthShowing)
        {
            viewSelector.Items.Add(new SelectorBarItem { Text = label, Tag = file });
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
        ShowSelectedView();
    }

    private void ShowSelectedView()
    {
        var item = viewSelector.SelectedItem;
        var isCode = item?.Tag is string;
        var isOptions = item is not null && item == _optionsItem;
        var showsOptions = Options is not null && !isCode && (isOptions || !_isNarrow);

        exampleContainer.Visibility = isCode || isOptions ? Visibility.Collapsed : Visibility.Visible;
        optionsContainer.Visibility = showsOptions ? Visibility.Visible : Visibility.Collapsed;
        codeContainer.Visibility = isCode ? Visibility.Visible : Visibility.Collapsed;

        if (item?.Tag is not string file) return;

        if (!_codeViews.TryGetValue(item.Text, out var view))
        {
            var snippet = item.Text switch
            {
                XamlView => Xaml,
                CSharpView => CSharp,
                _ => null
            };
            var code = string.IsNullOrWhiteSpace(snippet) ? ReadSource(file) ?? string.Empty : ApplySubstitutions(snippet);

            _codeViews[item.Text] = view = CreateCodeView(code, item.Text == XamlView);
        }

        codeScroller.Content = view;
    }

    /// <summary>
    /// Rebuilds the code after a snippet or one of its substitutions changed.
    /// </summary>
    private void RefreshCode()
    {
        _codeViews.Clear();

        if (_isLoaded)
        {
            ShowSelectedView();
        }
    }

    private static string? ReadSource(string file)
    {
        using var stream = typeof(SamplePresenter).Assembly.GetManifestResourceStream($"Source/{file}");

        if (stream is null) return null;

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

    private string ApplySubstitutions(string code)
    {
        code = string.Join('\n', code.Replace("\r\n", "\n").Trim('\n').TrimEnd().Split('\n').Select(line => line.TrimEnd()));

        return _substitutionPattern.Replace(code, match =>
        {
            var substitution = Substitutions.FirstOrDefault(substitution => substitution.Key == match.Groups[1].Value)
                ?? throw new KeyNotFoundException(match.Groups[1].Value);

            return substitution.ValueAsString()!;
        });
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

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as SamplePresenter)?.RefreshCode();
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

    public object? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    /// <summary>
    /// A XAML snippet to show instead of the page's whole .xaml file. $(Key) is replaced by the substitution of that key.
    /// </summary>
    public string? Xaml
    {
        get => (string?)GetValue(XamlCodeProperty);
        set => SetValue(XamlCodeProperty, value);
    }

    /// <summary>
    /// A C# snippet to show instead of the page's whole code-behind file.
    /// </summary>
    public string? CSharp
    {
        get => (string?)GetValue(CSharpCodeProperty);
        set => SetValue(CSharpCodeProperty, value);
    }

    /// <summary>
    /// Semicolon separated names of further source files to show, e.g. "Deal.cs".
    /// </summary>
    public string? Sources { get; set; }

    public IList<CodeSubstitution> Substitutions
    {
        get => (IList<CodeSubstitution>)GetValue(SubstitutionsProperty);
        set => SetValue(SubstitutionsProperty, value);
    }

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty ExampleProperty = DependencyProperty.Register(nameof(Example), typeof(object), typeof(SamplePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(object), typeof(SamplePresenter), new PropertyMetadata(null));
    public static readonly DependencyProperty XamlCodeProperty = DependencyProperty.Register(nameof(Xaml), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null, OnCodeChanged));
    public static readonly DependencyProperty CSharpCodeProperty = DependencyProperty.Register(nameof(CSharp), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null, OnCodeChanged));
    public static readonly DependencyProperty SubstitutionsProperty = DependencyProperty.Register(nameof(Substitutions), typeof(IList<CodeSubstitution>), typeof(SamplePresenter), new PropertyMetadata(null));

    [GeneratedRegex(@"\$\(([^\)]+)\)")]
    private static partial Regex SubstitutionPattern();
}
