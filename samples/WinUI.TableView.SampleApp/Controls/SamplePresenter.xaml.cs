#if WINDOWS
using ColorCode; 
#endif
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp.Controls
{
    public sealed partial class SamplePresenter : UserControl
    {
        private const string _baseUri = "https://GitHub.com/w-ahmad/WinUI.TableView.SampleApp/tree/main/src/WinUI.TableView.SampleApp/Pages/";
        private static readonly Regex _substitutionPattern = SubstitutionPattern();

        public SamplePresenter()
        {
            InitializeComponent();

            Substitutions = [];
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.FindParent<Page>() is { } page)
            {
                var pageName = page.GetType().Name;

                PageMarkupGitHubLink.NavigateUri = new Uri($"{_baseUri}{pageName}.xaml", UriKind.Absolute);
                PageCodeGitHubLink.NavigateUri = new Uri($"{_baseUri}{pageName}.xaml.cs", UriKind.Absolute);

            }

            if (Substitutions is not null)
            {
                foreach (var substitution in Substitutions)
                {
                    substitution.ValueChanged += OnSubstitutionValueChanged;
                }
            }

            GenerateSyntaxHighlightedContent();
        }

        private void OnSubstitutionValueChanged(CodeSubstitution sender, object? args)
        {
            GenerateSyntaxHighlightedContent();
        }

        private void GenerateSyntaxHighlightedContent()
        {
            OnXamlChanged();
            OnCSharpChanged();
        }

        private void OnToggleThemeButtonClicked(object sender, RoutedEventArgs e)
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

        private void OnSourceExpanderExpanded(Expander sender, ExpanderExpandingEventArgs args)
        {
            sourceRow.Height = new GridLength(1, GridUnitType.Star);
        }

        private void OnSourceExpanderCollapsed(Expander sender, ExpanderCollapsedEventArgs args)
        {
            sourceRow.Height = GridLength.Auto;
        }

        private void OnXamlChanged()
        {
            if (!IsLoaded) return;

            ToggleSourceExpanderVisibility();

            if (!string.IsNullOrWhiteSpace(Xaml))
            {
                AddFormattedCode(Xaml, "XAML");
            }
        }

        private void OnCSharpChanged()
        {
            if (!IsLoaded) return;

            ToggleSourceExpanderVisibility();

            if (!string.IsNullOrWhiteSpace(CSharp))
            {
                AddFormattedCode(CSharp, "C#");
            }
        }

        private void AddFormattedCode(string code, string lang)
        {
            code = ApplySubstitutions(code);

#if WINDOWS
            var formatter = new RichTextBlockFormatter(ThemeHelper.ActualTheme);
            var textBlock = new RichTextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                FontFamily = new FontFamily("Consolas"),
                IsTextSelectionEnabled = true
            }; 

            formatter.FormatRichTextBlock(code, lang == "XAML" ? Languages.Xml : Languages.CSharp, textBlock);
#else
            var textBlock = new TextBlock
            {
                Text = code,
                Margin = new Thickness(0, 8, 0, 0),
                FontFamily = new FontFamily("Consolas"),
            };
#endif

            var pivotItem = sourcePivot.Items.OfType<PivotItem>().FirstOrDefault(x => x.Header?.Equals(lang) is true);

            if (pivotItem is not null)
            {
                var scrollViewer = (ScrollViewer)pivotItem.Content;
                scrollViewer.Content = textBlock;
            }
            else
            {
                sourcePivot.Items.Insert(lang == "XAML" ? 0 : sourcePivot.Items.Count, new PivotItem
                {
                    Header = lang,
                    Content = new ScrollViewer
                    {
                        VerticalScrollMode = ScrollMode.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = textBlock
                    }
                });
            }
        }

        private string ApplySubstitutions(string code)
        {
            // Trim out stray blank lines at start and end.
            code = code.TrimStart('\n').TrimEnd();

            // Also trim out spaces at the end of each line
            code = string.Join('\n', code.Split('\n').Select(s => s.TrimEnd()));

            if (Substitutions != null)
            {
                // Perform any applicable substitutions.
                code = _substitutionPattern.Replace(code, match =>
                {
                    foreach (var substitution in Substitutions)
                    {
                        if (substitution.Key == match.Groups[1].Value)
                        {
                            return substitution.ValueAsString()!;
                        }
                    }
                    throw new KeyNotFoundException(match.Groups[1].Value);
                });
            }

            return code;
        }

        private void ToggleSourceExpanderVisibility()
        {
            sourceExpander.Visibility = !string.IsNullOrWhiteSpace(Xaml) || !string.IsNullOrWhiteSpace(CSharp)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnXamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SamplePresenter presenter)
            {
                presenter.OnXamlChanged();
            }
        }

        private static void OnCSharpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SamplePresenter presenter)
            {
                presenter.OnCSharpChanged();
            }
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

        public string? Xaml
        {
            get => (string?)GetValue(XamlCodeProperty);
            set => SetValue(XamlCodeProperty, value);
        }

        public string? CSharp
        {
            get => (string?)GetValue(CSharpCodeProperty);
            set => SetValue(CSharpCodeProperty, value);
        }

        public IList<CodeSubstitution> Substitutions
        {
            get => (IList<CodeSubstitution>)GetValue(SubstitutionsProperty);
            set => SetValue(SubstitutionsProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null));
        public static readonly DependencyProperty ExampleProperty = DependencyProperty.Register(nameof(Example), typeof(object), typeof(SamplePresenter), new PropertyMetadata(null));
        public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(object), typeof(SamplePresenter), new PropertyMetadata(null));
        public static readonly DependencyProperty XamlCodeProperty = DependencyProperty.Register(nameof(Xaml), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null, OnXamlChanged));
        public static readonly DependencyProperty CSharpCodeProperty = DependencyProperty.Register(nameof(CSharp), typeof(string), typeof(SamplePresenter), new PropertyMetadata(null, OnCSharpChanged));
        public static readonly DependencyProperty SubstitutionsProperty = DependencyProperty.Register(nameof(Substitutions), typeof(IList<CodeSubstitution>), typeof(SamplePresenter), new PropertyMetadata(null));

        [GeneratedRegex(@"\$\(([^\)]+)\)")]
        private static partial Regex SubstitutionPattern();
    }
}
