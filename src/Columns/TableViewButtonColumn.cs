using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Windows.Input;

using WinUI.TableView.Converters;

namespace WinUI.TableView
{
    /// <summary>
    /// Represents a column in a TableView that displays a clickable Button
    /// </summary>
    [StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(Button))]
    public class TableViewButtonColumn : TableViewBoundColumn
    {
        public TableViewButtonColumn()
        {
            // No editing mode
            UseSingleElement = true;
            CanSort = false;
            CanFilter = false;
        }

        /// <summary>
        /// Generates a Button element for the cell.
        /// </summary>
        /// <param name="cell">The cell for which the element is generated.</param>
        /// <param name="dataItem">The data item associated with the cell.</param>
        /// <returns>A TextBlock element.</returns>
        public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
        {
            var button = new Button();

            if (ButtonStyle != null)
                button.Style = ButtonStyle;
            
            if (!string.IsNullOrEmpty(IconGlyph))
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                // Add icon
                var fontIcon = new FontIcon
                {
                    Glyph = IconGlyph,
                    FontSize = IconFontSize is > 0 ? IconFontSize.Value : 16  // Default to 16 if not specified
                };

                stackPanel.Children.Add(fontIcon);

                // Add text if present
                if (Content != null)
                {
                    stackPanel.Children.Add(new TextBlock { Text = Content.ToString() });
                }

                button.Content = stackPanel;
            }
            else
            {
                button.Content = Content;
            }

            // Option 1: Bind to a command on the data item
            if (!string.IsNullOrEmpty(CommandPath))
            {
                button.SetBinding(Button.CommandProperty, new Binding { Path = new PropertyPath(CommandPath) });

                // Optional: bind command parameter to the data item or specific property
                if (!string.IsNullOrEmpty(CommandParameterPath))
                {
                    button.SetBinding(Button.CommandParameterProperty, new Binding { Path = new PropertyPath(CommandParameterPath) });
                }
                else
                {
                    // Default: pass the entire data item as parameter
                    button.CommandParameter = dataItem;
                }
            }
            // Option 2: Use column-level command (fallback for backward compatibility)
            else if (Command != null)
            {
                button.Command = Command;
                button.CommandParameter = dataItem;
            }
            // Option 3: Legacy Action support (for backward compatibility)
            else if (OnClick != null)
            {
                button.Click += (sender, e) =>
                {
                    var buttonDataContext = ((Button)sender).DataContext;
                    OnClick.Invoke(buttonDataContext);
                };
            }

            SetupBindings(button);
            return button;
        }

        private void SetupBindings(Button button)
        {
            // Set up visibility binding
            if (!string.IsNullOrEmpty(VisibilityPath))
            {
                var binding = new Binding { Path = new PropertyPath(VisibilityPath),  };

                var converter = ConverterFactory.CreateVisibilityConverter(VisibilityConverter, VisibilityValue);
                if (converter != null)
                {
                    binding.Converter = converter;
                }

                button.SetBinding(UIElement.VisibilityProperty, binding);
            }

            // Set up enabled binding
            if (!string.IsNullOrEmpty(EnabledPath))
            {
                var binding = new Binding { Path = new PropertyPath(EnabledPath) };

                var converter = ConverterFactory.CreateBooleanConverter(EnabledConverter, EnabledValue);
                if (converter != null)
                {
                    binding.Converter = converter;
                }

                button.SetBinding(Control.IsEnabledProperty, binding);
            }
        }

        public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
        {
            // Because of UseSingleElement = true, GenerateEditingElement should not be called.
            throw new NotImplementedException();
        }

        public int? IconFontSize
        {
            get => (int?)GetValue(IconFontSizeProperty);
            set => SetValue(IconFontSizeProperty, value);
        }

        public string? CommandPath
        {
            get => (string?)GetValue(CommandPathProperty);
            set => SetValue(CommandPathProperty, value);
        }

        public string? CommandParameterPath
        {
            get => (string?)GetValue(CommandParameterPathProperty);
            set => SetValue(CommandParameterPathProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public string? Content
        {
            get => (string?)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public Action<object>? OnClick
        {
            get => (Action<object>?)GetValue(OnClickProperty);
            set => SetValue(OnClickProperty, value);
        }

        public string? VisibilityPath
        {
            get => (string?)GetValue(VisibilityPathProperty);
            set => SetValue(VisibilityPathProperty, value);
        }

        public object? VisibilityValue
        {
            get => GetValue(VisibilityValueProperty);
            set => SetValue(VisibilityValueProperty, value);
        }

        public IValueConverter? VisibilityConverter
        {
            get => (IValueConverter?)GetValue(VisibilityConverterProperty);
            set => SetValue(VisibilityConverterProperty, value);
        }

        public string? EnabledPath
        {
            get => (string?)GetValue(EnabledPathProperty);
            set => SetValue(EnabledPathProperty, value);
        }

        public object? EnabledValue
        {
            get => GetValue(EnabledValueProperty);
            set => SetValue(EnabledValueProperty, value);
        }

        public IValueConverter? EnabledConverter
        {
            get => (IValueConverter?)GetValue(EnabledConverterProperty);
            set => SetValue(EnabledConverterProperty, value);
        }

        public string? IconGlyph
        {
            get => (string?)GetValue(IconGlyphProperty);
            set => SetValue(IconGlyphProperty, value);
        }

        public Style? ButtonStyle
        {
            get => (Style?)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        // Dependency Properties
        public static readonly DependencyProperty IconFontSizeProperty =
            DependencyProperty.Register(nameof(IconFontSize), typeof(int), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty CommandPathProperty =
            DependencyProperty.Register(nameof(CommandPath), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty CommandParameterPathProperty =
            DependencyProperty.Register(nameof(CommandParameterPath), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty OnClickProperty =
            DependencyProperty.Register(nameof(OnClick), typeof(Action<object>), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty VisibilityPathProperty =
            DependencyProperty.Register(nameof(VisibilityPath), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty VisibilityValueProperty =
            DependencyProperty.Register(nameof(VisibilityValue), typeof(object), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty VisibilityConverterProperty =
            DependencyProperty.Register(nameof(VisibilityConverter), typeof(IValueConverter), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty EnabledPathProperty =
            DependencyProperty.Register(nameof(EnabledPath), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty EnabledValueProperty =
            DependencyProperty.Register(nameof(EnabledValue), typeof(object), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty EnabledConverterProperty =
            DependencyProperty.Register(nameof(EnabledConverter), typeof(IValueConverter), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty IconGlyphProperty =
            DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(TableViewButtonColumn), new PropertyMetadata(default));

        public static readonly DependencyProperty ButtonStyleProperty = 
            DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(TableViewButtonColumn), new PropertyMetadata(default));
    }
}
