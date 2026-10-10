using Microsoft.UI.Xaml.Markup;

namespace WinUI.TableView.SampleApp.Helpers;

/// <summary>
/// Provides the values of an enum type, e.g. <c>ItemsSource="{helpers:EnumValues Type=tv:TableViewGridLinesVisibility}"</c>.
/// </summary>
/// <remarks>
/// Used instead of the CommunityToolkit <c>EnumValues</c> extension, which calls <see cref="Enum.GetValues(Type)"/>;
/// that needs runtime code generation and fails when the app is published with Native AOT.
/// </remarks>
[MarkupExtensionReturnType(ReturnType = typeof(List<object>))]
public sealed partial class EnumValuesExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the enum type whose values are provided.
    /// </summary>
    public Type? Type { get; set; }

    /// <inheritdoc/>
    protected override object ProvideValue()
    {
        if (Type is not { IsEnum: true } type) return new List<object>();

        return Enum.GetValuesAsUnderlyingType(type)
                   .Cast<object>()
                   .Select(value => Enum.ToObject(type, value))
                   .ToList();
    }
}
