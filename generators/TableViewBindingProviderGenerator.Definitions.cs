using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
namespace WinUI.TableView.SourceGenerators;
public sealed partial class TableViewBindingProviderGenerator
{
    private static readonly DiagnosticDescriptor TableViewNameRequiredDescriptor =
        new(
            id: "TV0001",
            title: "TableView requires a name for source generation",
            messageFormat: "TableView in '{0}' must define x:Name or Name to enable generated code",
            category: "WinUI.TableView.SourceGenerators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnableToResolveItemsTypeDescriptor =
        new(
            id: "TV0002",
            title: "Unable to resolve TableView item type",
            messageFormat: "TableView in '{0}' uses x:Bind ItemsSource '{1}', but the item type could not be resolved to a typed generic collection",
            category: "WinUI.TableView.SourceGenerators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor WindowRequiresManualConnectDescriptor =
        new(
            id: "TV0003",
            title: "Window containing TableView must call ConnectTableViews",
            messageFormat: "Class '{0}' contains TableView in a Window. Call ConnectTableViews() in code-behind after InitializeComponent().",
            category: "WinUI.TableView.SourceGenerators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnableToResolveMemberPathDescriptor =
        new(
            id: "TV0004",
            title: "Unable to resolve TableView member path",
            messageFormat: "TableView in '{0}' could not resolve member path '{1}' for item type '{2}'",
            category: "WinUI.TableView.SourceGenerators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    private static readonly Regex XamlClassRegex =
        new(
            @"x:Class\s*=\s*[""'](?<className>[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TableViewTagRegex =
        new(
            @"<\s*(?:(?<prefix>[A-Za-z_][A-Za-z0-9_]*)\:)?TableView(?=\s|/|>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex XmlnsPrefixRegex =
        new(
            @"xmlns\:(?<prefix>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*[""']using:WinUI\.TableView(?:;[^""']*)?[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex XmlnsDefaultRegex =
        new(
            @"xmlns\s*=\s*[""']using:WinUI\.TableView(?:;[^""']*)?[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TableViewNameRegex =
        new(
            @"(?:x:Name|Name)\s*=\s*[""'](?<name>[A-Za-z_][A-Za-z0-9_]*)[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ItemsSourceBindingRegex =
        new(
            @"ItemsSource\s*=\s*[""']\{(?<kind>x:Bind|Binding)\s*(?<body>[^}]*)\}[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SortMemberPathRegex =
        new(
            @"SortMemberPath\s*=\s*[""'](?<path>[^""']+)[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ClipboardBindingRegex =
        new(
            @"ClipboardContentBinding\s*=\s*[""']\{Binding\s*(?<body>[^}]*)\}[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ContentBindingRegex =
        new(
            @"(?<![A-Za-z0-9_])ContentBinding\s*=\s*[""']\{Binding\s*(?<body>[^}]*)\}[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DisplayMemberPathRegex =
        new(
            @"DisplayMemberPath\s*=\s*[""'](?<path>[^""']+)[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);


    private static readonly Regex CellBindingRegex =
        new(
            @"\bBinding\s*=\s*[""']\{Binding\s*(?<body>[^}]*)\}[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BindingPathTokenRegex =
        new(
            @"(?:^|,)\s*Path\s*=\s*(?<path>[^,\s]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BindingModeTokenRegex =
        new(
            @"(?:^|,)\s*Mode\s*=\s*(?<mode>[^,\s]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NonSimpleBindingTokenRegex =
        new(
            @"(?:^|,)\s*(?:Source|RelativeSource|ElementName)\s*=",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ColumnTagRegex =
        new(
            @"<\s*(?:(?<prefix>[A-Za-z_][A-Za-z0-9_]*)\:)?(?<columnType>[A-Za-z_][A-Za-z0-9_]*Column)\b(?<attrs>[^>]*)>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CSharpMemberPathRegex =
        new(
            @"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$",
            RegexOptions.Compiled);

    private static readonly SymbolDisplayFormat FullyQualifiedNonAliasedTypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            & ~SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

}
