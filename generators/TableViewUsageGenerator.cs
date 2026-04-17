using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace WinUI.TableView.SourceGenerators;

[Generator]
public sealed class TableViewBindingProviderGenerator : IIncrementalGenerator
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

    private static readonly Regex NonSimpleBindingTokenRegex =
        new(
            @"(?:^|,)\s*(?:Source|RelativeSource|ElementName)\s*=",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ColumnTagRegex =
        new(
            @"<\s*(?:(?<prefix>[A-Za-z_][A-Za-z0-9_]*)\:)?[A-Za-z_][A-Za-z0-9_]*Column\b(?<attrs>[^>]*)>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CSharpMemberPathRegex =
        new(
            @"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$",
            RegexOptions.Compiled);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var xamlFiles = context.AdditionalTextsProvider
            .Where(static file =>
                file.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                && !IsBuildOutputPath(file.Path));

        var parsedXamlInfos = xamlFiles
            .Select(static (additionalText, cancellationToken) => ParseXamlInfo(additionalText, cancellationToken))
            .Where(static parsed => parsed is not null)
            .Select(static (parsed, _) => parsed!.Value)
            .Collect();

        var combined = context.CompilationProvider.Combine(parsedXamlInfos);

        context.RegisterSourceOutput(combined, static (sourceProductionContext, input) =>
        {
            var compilation = input.Left;
            var parsedXamls = input.Right;

            var seenClasses = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parsed in parsedXamls)
            {
                if (!seenClasses.Add(parsed.FullyQualifiedClassName))
                {
                    continue;
                }

                var providers = ImmutableArray.CreateBuilder<GeneratedProviderInfo>();
                var usedProviderClassNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (var tableView in parsed.TableViews)
                {
                    if (string.IsNullOrWhiteSpace(tableView.TableViewName))
                    {
                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                TableViewNameRequiredDescriptor,
                                CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName));
                        continue;
                    }

                    var itemType = ResolveItemsSourceItemType(
                        compilation,
                        parsed.FullyQualifiedClassName,
                        tableView.ItemsSourceXBindPath);

                    if (itemType is null) continue;

                    var itemTypeDisplay = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (!string.IsNullOrWhiteSpace(tableView.ItemsSourceXBindPath)
                        && string.IsNullOrWhiteSpace(itemTypeDisplay))
                    {
                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveItemsTypeDescriptor,
                                CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                tableView.ItemsSourceXBindPath));
                    }

                    if (itemType is null
                        || string.IsNullOrWhiteSpace(itemTypeDisplay)
                        || (tableView.SortMemberPaths.Length == 0
                            && tableView.ClipboardPaths.Length == 0
                            && tableView.DisplayMemberPaths.Length == 0
                            && tableView.BindingPaths.Length == 0
                            && tableView.ContentPaths.Length == 0))
                    {
                        continue;
                    }

                    var clipboardPathCandidates = ImmutableArray.CreateBuilder<string>();
                    foreach (var memberPath in tableView.ClipboardPaths)
                    {
                        if (CSharpMemberPathRegex.IsMatch(memberPath))
                        {
                            clipboardPathCandidates.Add(memberPath);
                        }
                    }

                    var bindingPathCandidates = ImmutableArray.CreateBuilder<string>();
                    foreach (var memberPath in tableView.BindingPaths)
                    {
                        if (CSharpMemberPathRegex.IsMatch(memberPath))
                        {
                            bindingPathCandidates.Add(memberPath);
                        }
                    }

                    var contentPathCandidates = ImmutableArray.CreateBuilder<string>();
                    foreach (var memberPath in tableView.ContentPaths)
                    {
                        if (CSharpMemberPathRegex.IsMatch(memberPath))
                        {
                            contentPathCandidates.Add(memberPath);
                        }
                    }

                    var displayMemberPathCandidates = ImmutableArray.CreateBuilder<string>();
                    foreach (var memberPath in tableView.DisplayMemberPaths)
                    {
                        if (CSharpMemberPathRegex.IsMatch(memberPath))
                        {
                            displayMemberPathCandidates.Add(memberPath);
                        }
                    }

                    var sortMemberPathCandidates = ImmutableArray.CreateBuilder<string>();
                    foreach (var memberPath in tableView.SortMemberPaths)
                    {
                        if (CSharpMemberPathRegex.IsMatch(memberPath))
                        {
                            sortMemberPathCandidates.Add(memberPath);
                        }
                    }

                    var sortCases = ImmutableArray.CreateBuilder<GeneratedValueCase>();
                    foreach (var memberPath in sortMemberPathCandidates)
                    {
                        if (TryBuildValueAccessExpression(itemType, memberPath, out var accessExpression, out _))
                        {
                            var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.SortMemberPath);
                            sortCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo));

                            continue;
                        }

                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveMemberPathDescriptor,
                                CreatePathDiagnosticLocation(tableView.SortMemberPathLocations, memberPath)
                                    ?? CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                memberPath,
                                itemTypeDisplay));
                    }

                    var clipboardCases = ImmutableArray.CreateBuilder<GeneratedValueCase>();
                    foreach (var memberPath in clipboardPathCandidates)
                    {
                        if (TryBuildValueAccessExpression(itemType, memberPath, out var accessExpression, out _))
                        {
                            var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.ClipboardBindingPath);
                            clipboardCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo));

                            continue;
                        }

                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveMemberPathDescriptor,
                                CreatePathDiagnosticLocation(tableView.ClipboardPathLocations, memberPath)
                                    ?? CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                memberPath,
                                itemTypeDisplay));
                    }

                    var bindingPathCases = ImmutableArray.CreateBuilder<GeneratedValueCase>();
                    foreach (var memberPath in bindingPathCandidates)
                    {
                        if (TryBuildValueAccessExpression(itemType, memberPath, out var accessExpression, out var memberType))
                        {
                            var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.BindingPath);
                            bindingPathCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo));

                            continue;
                        }

                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveMemberPathDescriptor,
                                CreatePathDiagnosticLocation(tableView.BindingPathLocations, memberPath)
                                    ?? CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                memberPath,
                                itemTypeDisplay));
                    }

                    var contentPathCases = ImmutableArray.CreateBuilder<GeneratedValueCase>();
                    foreach (var memberPath in contentPathCandidates)
                    {
                        if (TryBuildValueAccessExpression(itemType, memberPath, out var accessExpression, out var memberType))
                        {
                            var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.ContentBindingPath);
                            contentPathCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo));

                            continue;
                        }

                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveMemberPathDescriptor,
                                CreatePathDiagnosticLocation(tableView.ContentPathLocations, memberPath)
                                    ?? CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                memberPath,
                                itemTypeDisplay));
                    }

                    var displayMemberPathCases = ImmutableArray.CreateBuilder<GeneratedValueCase>();
                    foreach (var memberPath in displayMemberPathCandidates)
                    {
                        var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.DisplayMemberPath);
                        var cellValuePathCase = bindingPathCases.FirstOrDefault(c => c.SourceInfo.ColumnStartLine == sourceInfo.ColumnStartLine);
                        if (string.IsNullOrEmpty(cellValuePathCase.MemberPath))
                        {
                            continue;
                        }

                        if (TryBuildValueAccessExpression(itemType, $"{cellValuePathCase.MemberPath}.{memberPath}", out var accessExpression, out var memberType))
                        {
                            displayMemberPathCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo));

                            continue;
                        }

                        sourceProductionContext.ReportDiagnostic(
                            Diagnostic.Create(
                                UnableToResolveMemberPathDescriptor,
                                CreatePathDiagnosticLocation(tableView.DisplayMemberPathLocations, memberPath)
                                    ?? CreateDiagnosticLocation(tableView),
                                parsed.FullyQualifiedClassName,
                                memberPath,
                                itemTypeDisplay));
                    }

                    if (sortCases.Count == 0 && clipboardCases.Count == 0 && bindingPathCases.Count == 0 && displayMemberPathCases.Count == 0 && contentPathCases.Count == 0)
                    {
                        continue;
                    }

                    var baseProviderClassName = $"TableView_{SanitizeIdentifier(tableView.TableViewName!)}_MemberValueProvider";
                    var providerClassName = GetUniqueIdentifier(baseProviderClassName, usedProviderClassNames);

                    providers.Add(new GeneratedProviderInfo(
                        tableView.TableViewName!,
                        providerClassName,
                        tableView.TableViewLine,
                        EnsureGlobalQualified(itemTypeDisplay!),
                        sortCases.ToImmutable(),
                        displayMemberPathCases.ToImmutable(),
                        bindingPathCases.ToImmutable(),
                        clipboardCases.ToImmutable(),
                        contentPathCases.ToImmutable()));
                }

                if (providers.Count == 0)
                {
                    continue;
                }

                if (parsed.IsWindowRoot
                    && !HasConnectTableViewsCall(compilation, parsed.FullyQualifiedClassName))
                {
                    sourceProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            WindowRequiresManualConnectDescriptor,
                            GetClassLocation(compilation, parsed.FullyQualifiedClassName),
                            parsed.FullyQualifiedClassName));
                }

                var source = BuildSource(parsed.NamespaceName, parsed.ClassName, providers.ToImmutable());
                sourceProductionContext.AddSource(parsed.HintName, SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static bool IsBuildOutputPath(string path)
    {
        var normalizedPath = path.Replace('/', '\\');
        return normalizedPath.IndexOf(@"\bin\", StringComparison.OrdinalIgnoreCase) >= 0
            || normalizedPath.IndexOf(@"\obj\", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static ParsedXamlInfo? ParseXamlInfo(
        AdditionalText additionalText,
        CancellationToken cancellationToken)
    {
        var sourceText = additionalText.GetText(cancellationToken);
        if (sourceText is null)
        {
            return null;
        }

        var xamlContent = sourceText.ToString();
        if (string.IsNullOrWhiteSpace(xamlContent))
        {
            return null;
        }

        var isWindowRoot = IsWindowRootElement(xamlContent);

        if (!TryGetWinUITableViews(xamlContent, out var tableViewsRaw) || tableViewsRaw.Length == 0)
        {
            return null;
        }

        var classMatch = XamlClassRegex.Match(xamlContent);
        if (!classMatch.Success)
        {
            return null;
        }

        var fullyQualifiedClassName = classMatch.Groups["className"].Value.Trim();
        if (string.IsNullOrWhiteSpace(fullyQualifiedClassName))
        {
            return null;
        }

        var (namespaceName, className) = SplitClassName(fullyQualifiedClassName);
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        var tableViews = ImmutableArray.CreateBuilder<TableViewXamlInfo>();
        foreach (var tableViewRaw in tableViewsRaw)
        {
            var hasItemsSourceLocation = tableViewRaw.ItemsSourceSpanStart >= 0
                && tableViewRaw.ItemsSourceSpanLength > 0
                && sourceText.Lines.Count > 0;

            var startLine = 0;
            var startColumn = 0;
            var endLine = 0;
            var endColumn = 0;

            if (hasItemsSourceLocation)
            {
                var span = new TextSpan(tableViewRaw.ItemsSourceSpanStart, tableViewRaw.ItemsSourceSpanLength);
                var lineSpan = sourceText.Lines.GetLinePositionSpan(span);
                startLine = lineSpan.Start.Line;
                startColumn = lineSpan.Start.Character;
                endLine = lineSpan.End.Line;
                endColumn = lineSpan.End.Character;
            }

            tableViews.Add(
                new TableViewXamlInfo(
                    tableViewRaw.TableViewName,
                    tableViewRaw.TableViewLine,
                    tableViewRaw.ItemsSourceXBindPath,
                    tableViewRaw.SortMemberPaths,
                    tableViewRaw.DisplayMemberPaths,
                    tableViewRaw.BindingPaths,
                    tableViewRaw.ClipboardPaths,
                    tableViewRaw.ContentPaths,
                    CreateColumnDefinitions(sourceText, additionalText.Path, tableViewRaw.ColumnDefinitions),
                    CreatePathDiagnosticLocations(sourceText, additionalText.Path, tableViewRaw.SortMemberPathSpans),
                    CreatePathDiagnosticLocations(sourceText, additionalText.Path, tableViewRaw.DisplayMemberPathSpans),
                    CreatePathDiagnosticLocations(sourceText, additionalText.Path, tableViewRaw.BindingPathSpans),
                    CreatePathDiagnosticLocations(sourceText, additionalText.Path, tableViewRaw.ClipboardPathSpans),
                    CreatePathDiagnosticLocations(sourceText, additionalText.Path, tableViewRaw.ContentPathSpans),
                    hasItemsSourceLocation,
                    additionalText.Path,
                    tableViewRaw.ItemsSourceSpanStart,
                    tableViewRaw.ItemsSourceSpanLength,
                    startLine,
                    startColumn,
                    endLine,
                    endColumn));
        }

        var hintName = $"{fullyQualifiedClassName.Replace('.', '_')}.TableViewUsage.g.cs";
        return new ParsedXamlInfo(
            namespaceName,
            className,
            fullyQualifiedClassName,
            hintName,
            isWindowRoot,
            tableViews.ToImmutable());
    }

    private static ImmutableArray<ColumnDefinitionInfo> CreateColumnDefinitions(
        SourceText sourceText,
        string filePath,
        ImmutableArray<ColumnDefinitionRaw> columnDefinitions)
    {
        if (columnDefinitions.Length == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<ColumnDefinitionInfo>(columnDefinitions.Length);
        foreach (var column in columnDefinitions)
        {
            builder.Add(
                new ColumnDefinitionInfo(
                    column.StartLine,
                    CreatePathDiagnosticLocation(sourceText, filePath, column.SortMemberPathSpan),
                    CreatePathDiagnosticLocation(sourceText, filePath, column.DisplayMemberPathSpan),
                    CreatePathDiagnosticLocation(sourceText, filePath, column.BindingPathSpan),
                    CreatePathDiagnosticLocation(sourceText, filePath, column.ClipboardPathSpan),
                CreatePathDiagnosticLocation(sourceText, filePath, column.ContentPathSpan)));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Converts raw absolute path spans into Roslyn-friendly file and line mapped locations.
    /// </summary>
    /// <param name="sourceText">The full XAML source text.</param>
    /// <param name="filePath">The source file path for the XAML file.</param>
    /// <param name="pathSpans">Absolute spans for extracted member paths.</param>
    /// <returns>A list of member-path diagnostic locations.</returns>
    private static ImmutableArray<PathDiagnosticLocation> CreatePathDiagnosticLocations(
        SourceText sourceText,
        string filePath,
        ImmutableArray<PathSpanRaw> pathSpans)
    {
        if (pathSpans.Length == 0 || sourceText.Lines.Count == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<PathDiagnosticLocation>(pathSpans.Length);
        foreach (var pathSpan in pathSpans)
        {
            if (pathSpan.SpanStart < 0 || pathSpan.SpanLength <= 0)
            {
                continue;
            }

            var span = new TextSpan(pathSpan.SpanStart, pathSpan.SpanLength);
            var lineSpan = sourceText.Lines.GetLinePositionSpan(span);
            builder.Add(new PathDiagnosticLocation(
                pathSpan.Path,
                filePath,
                pathSpan.SpanStart,
                pathSpan.SpanLength,
                lineSpan.Start.Line,
                lineSpan.Start.Character,
                lineSpan.End.Line,
                lineSpan.End.Character));
        }

        return builder.ToImmutable();
    }

    private static bool IsWindowRootElement(string xamlContent)
    {
        var index = 0;
        while (index < xamlContent.Length)
        {
            var ltIndex = xamlContent.IndexOf('<', index);
            if (ltIndex < 0 || ltIndex + 1 >= xamlContent.Length)
            {
                return false;
            }

            var nextChar = xamlContent[ltIndex + 1];

            if (nextChar == '?')
            {
                var piEnd = xamlContent.IndexOf("?>", ltIndex + 2, StringComparison.Ordinal);
                if (piEnd < 0)
                {
                    return false;
                }

                index = piEnd + 2;
                continue;
            }

            if (nextChar == '!')
            {
                if (xamlContent.AsSpan(ltIndex).StartsWith("<!--".AsSpan(), StringComparison.Ordinal))
                {
                    var commentEnd = xamlContent.IndexOf("-->", ltIndex + 4, StringComparison.Ordinal);
                    if (commentEnd < 0)
                    {
                        return false;
                    }

                    index = commentEnd + 3;
                    continue;
                }

                var declarationEnd = xamlContent.IndexOf('>', ltIndex + 2);
                if (declarationEnd < 0)
                {
                    return false;
                }

                index = declarationEnd + 1;
                continue;
            }

            var nameStart = ltIndex + 1;
            while (nameStart < xamlContent.Length && char.IsWhiteSpace(xamlContent[nameStart]))
            {
                nameStart++;
            }

            var nameEnd = nameStart;
            while (nameEnd < xamlContent.Length
                && !char.IsWhiteSpace(xamlContent[nameEnd])
                && xamlContent[nameEnd] is not '>' and not '/')
            {
                nameEnd++;
            }

            if (nameEnd <= nameStart)
            {
                return false;
            }

            var qualifiedName = xamlContent.Substring(nameStart, nameEnd - nameStart);
            var colonIndex = qualifiedName.IndexOf(':');
            var localName = colonIndex >= 0 ? qualifiedName.Substring(colonIndex + 1) : qualifiedName;

            return localName.Equals("Window", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool TryGetWinUITableViews(string xamlContent, out ImmutableArray<TableViewInfoRaw> tableViews)
    {
        var commentRanges = GetCommentRanges(xamlContent);
        var allowedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in XmlnsPrefixRegex.Matches(xamlContent))
        {
            var prefix = match.Groups["prefix"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                allowedPrefixes.Add(prefix);
            }
        }

        var defaultNamespaceIsWinUITableView = XmlnsDefaultRegex.IsMatch(xamlContent);
        var builder = ImmutableArray.CreateBuilder<TableViewInfoRaw>();

        for (var match = TableViewTagRegex.Match(xamlContent); match.Success; match = match.NextMatch())
        {
            if (IsIndexInAnyRange(match.Index, commentRanges))
            {
                continue;
            }

            var prefixGroup = match.Groups["prefix"];
            var isValidTag = prefixGroup.Success
                ? allowedPrefixes.Contains(prefixGroup.Value)
                : defaultNamespaceIsWinUITableView;

            if (!isValidTag)
            {
                continue;
            }

            var startTagText = ExtractStartTagText(xamlContent, match.Index);
            if (string.IsNullOrWhiteSpace(startTagText))
            {
                continue;
            }

            var tableViewLine = GetLineAtIndex(xamlContent, match.Index);
            if (string.IsNullOrWhiteSpace(tableViewLine))
            {
                continue;
            }

            var tableViewSegment = ExtractTableViewSegment(xamlContent, match.Index, startTagText, prefixGroup);
            var tableViewName = TryExtractTableViewName(startTagText);
            var itemsSourceXBindPath = TryExtractItemsSourceXBindPath(
                startTagText,
                match.Index,
                out var itemsSourceSpanInText);

            var columnDefinitions = ExtractColumnDefinitions(xamlContent, tableViewSegment, match.Index);
            var sortMemberPathSpans = GetPathSpans(columnDefinitions, static column => column.SortMemberPathSpan);
            var displayMemberPathSpans = GetPathSpans(columnDefinitions, static column => column.DisplayMemberPathSpan);
            var bingingPathSpans = GetPathSpans(columnDefinitions, static column => column.BindingPathSpan);
            var clipboardPathSpans = GetPathSpans(columnDefinitions, static column => column.ClipboardPathSpan);
            var contentPathSpans = GetPathSpans(columnDefinitions, static column => column.ContentPathSpan);
            var sortMemberPaths = ExtractPaths(sortMemberPathSpans);
            var displayMemberPaths = ExtractPaths(displayMemberPathSpans);
            var bindingPaths = ExtractPaths(bingingPathSpans);
            var clipboardPaths = ExtractPaths(clipboardPathSpans);
            var contentPaths = ExtractPaths(contentPathSpans);

            builder.Add(new TableViewInfoRaw(
                tableViewName,
                tableViewLine,
                itemsSourceXBindPath,
                sortMemberPaths,
                displayMemberPaths,
                bindingPaths,
                clipboardPaths,
                contentPaths,
                columnDefinitions,
                sortMemberPathSpans,
                displayMemberPathSpans,
                bingingPathSpans,
                clipboardPathSpans,
                contentPathSpans,
                itemsSourceSpanInText.Start,
                itemsSourceSpanInText.Length));
        }

        tableViews = builder.ToImmutable();
        return tableViews.Length > 0;
    }

    private static ImmutableArray<(int Start, int End)> GetCommentRanges(string xamlContent)
    {
        var ranges = ImmutableArray.CreateBuilder<(int Start, int End)>();
        var searchIndex = 0;

        while (searchIndex < xamlContent.Length)
        {
            var commentStart = xamlContent.IndexOf("<!--", searchIndex, StringComparison.Ordinal);
            if (commentStart < 0)
            {
                break;
            }

            var commentEndMarker = xamlContent.IndexOf("-->", commentStart + 4, StringComparison.Ordinal);
            var commentEnd = commentEndMarker < 0 ? xamlContent.Length : commentEndMarker + 3;
            ranges.Add((commentStart, commentEnd));

            searchIndex = commentEnd;
        }

        return ranges.ToImmutable();
    }

    private static bool IsIndexInAnyRange(int index, ImmutableArray<(int Start, int End)> ranges)
    {
        foreach (var (Start, End) in ranges)
        {
            if (index >= Start && index < End)
            {
                return true;
            }
        }

        return false;
    }

    private static string? TryExtractTableViewName(string startTagText)
    {
        var match = TableViewNameRegex.Match(startTagText);
        if (!match.Success)
        {
            return null;
        }

        var name = match.Groups["name"].Value.Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static ImmutableArray<ColumnDefinitionRaw> ExtractColumnDefinitions(
        string fullXamlContent,
        string tableViewSegment,
        int tableViewSegmentStart)
    {
        if (string.IsNullOrWhiteSpace(tableViewSegment))
        {
            return [];
        }

        var commentRanges = GetCommentRanges(tableViewSegment);
        var builder = ImmutableArray.CreateBuilder<ColumnDefinitionRaw>();
        foreach (Match columnMatch in ColumnTagRegex.Matches(tableViewSegment))
        {
            if (IsIndexInAnyRange(columnMatch.Index, commentRanges))
            {
                continue;
            }

            var attrsGroup = columnMatch.Groups["attrs"];
            var attrsText = attrsGroup.Success ? attrsGroup.Value : string.Empty;
            var attrsStart = tableViewSegmentStart + attrsGroup.Index;
            var absoluteColumnStart = tableViewSegmentStart + columnMatch.Index;
            var columnStartLine = GetLineNumber(fullXamlContent, absoluteColumnStart);

            var sortMemberPathSpan = TryExtractPathSpan(attrsText, attrsStart, SortMemberPathRegex);
            var displayMemberPathSpan = TryExtractPathSpan(attrsText, attrsStart, DisplayMemberPathRegex);
            var bindingPathSpan = TryExtractBindingPathSpan(attrsText, attrsStart, CellBindingRegex);
            var clipboardBindingPathSpan = TryExtractBindingPathSpan(attrsText, attrsStart, ClipboardBindingRegex);
            var contentBindingPathSpan = TryExtractBindingPathSpan(attrsText, attrsStart, ContentBindingRegex);

            builder.Add(new ColumnDefinitionRaw(
                absoluteColumnStart,
                columnStartLine,
                sortMemberPathSpan,
                displayMemberPathSpan,
                bindingPathSpan,
                clipboardBindingPathSpan,
                contentBindingPathSpan));
        }

        return builder.ToImmutable();
    }

    private static PathDiagnosticLocation? CreatePathDiagnosticLocation(
        SourceText sourceText,
        string filePath,
        PathSpanRaw? pathSpan)
    {
        if (!pathSpan.HasValue || sourceText.Lines.Count == 0)
        {
            return null;
        }

        var value = pathSpan.Value;
        if (value.SpanStart < 0 || value.SpanLength <= 0)
        {
            return null;
        }

        var span = new TextSpan(value.SpanStart, value.SpanLength);
        var lineSpan = sourceText.Lines.GetLinePositionSpan(span);
        return new PathDiagnosticLocation(
            value.Path,
            filePath,
            value.SpanStart,
            value.SpanLength,
            lineSpan.Start.Line,
            lineSpan.Start.Character,
            lineSpan.End.Line,
            lineSpan.End.Character);
    }

    private static int GetLineNumber(string content, int absoluteIndex)
    {
        if (absoluteIndex <= 0)
        {
            return 1;
        }

        var line = 1;
        var max = Math.Min(absoluteIndex, content.Length);
        for (var i = 0; i < max; i++)
        {
            if (content[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static PathSpanRaw? TryExtractPathSpan(string attrsText, int attrsStart, Regex regex)
    {
        if (string.IsNullOrWhiteSpace(attrsText))
        {
            return null;
        }

        var match = regex.Match(attrsText);
        if (!match.Success)
        {
            return null;
        }

        var pathGroup = match.Groups["path"];
        var path = pathGroup.Value.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return new PathSpanRaw(path, attrsStart + pathGroup.Index, Math.Max(1, pathGroup.Length));
    }

    private static PathSpanRaw? TryExtractBindingPathSpan(string attrsText, int attrsStart, Regex bindingRegex)
    {
        if (string.IsNullOrWhiteSpace(attrsText))
        {
            return null;
        }

        var bindingMatch = bindingRegex.Match(attrsText);
        if (!bindingMatch.Success)
        {
            return null;
        }

        var bodyGroup = bindingMatch.Groups["body"];
        var path = TryExtractBindingPath(bodyGroup.Value);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var body = bodyGroup.Value;
        var pathOffsetInBody = body.IndexOf(path, StringComparison.Ordinal);
        if (pathOffsetInBody < 0)
        {
            pathOffsetInBody = 0;
        }

        var resolvedPath = path!;
        var pathStart = attrsStart + bodyGroup.Index + pathOffsetInBody;
        return new PathSpanRaw(resolvedPath, pathStart, Math.Max(1, resolvedPath.Length));
    }

    private static string? TryExtractBindingPath(string bindingBody)
    {
        if (string.IsNullOrWhiteSpace(bindingBody))
        {
            return null;
        }

        var trimmedBody = bindingBody.Trim();
        if (NonSimpleBindingTokenRegex.IsMatch(trimmedBody))
        {
            return null;
        }

        // Handles: {Binding Path=Id} and other bodies containing Path=...
        var explicitPathMatch = BindingPathTokenRegex.Match(trimmedBody);
        if (explicitPathMatch.Success)
        {
            var explicitPath = explicitPathMatch.Groups["path"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return explicitPath;
            }
        }

        // Handles: {Binding Id}
        var firstSegment = trimmedBody.Split(',')[0].Trim();
        if (string.IsNullOrWhiteSpace(firstSegment) || firstSegment.Contains('='))
        {
            return null;
        }

        return firstSegment;
    }

    private static ImmutableArray<PathSpanRaw> GetPathSpans(
        ImmutableArray<ColumnDefinitionRaw> columns,
        Func<ColumnDefinitionRaw, PathSpanRaw?> selector)
    {
        if (columns.Length == 0)
        {
            return [];
        }

        var uniquePaths = new HashSet<string>(StringComparer.Ordinal);
        var builder = ImmutableArray.CreateBuilder<PathSpanRaw>();
        foreach (var column in columns)
        {
            var pathSpan = selector(column);
            if (!pathSpan.HasValue || !uniquePaths.Add(pathSpan.Value.Path))
            {
                continue;
            }

            builder.Add(pathSpan.Value);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Projects path span entries to their path text in declaration order.
    /// </summary>
    /// <param name="pathSpans">Path span entries.</param>
    /// <returns>Ordered unique path texts.</returns>
    private static ImmutableArray<string> ExtractPaths(ImmutableArray<PathSpanRaw> pathSpans)
    {
        if (pathSpans.Length == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<string>(pathSpans.Length);
        foreach (var pathSpan in pathSpans)
        {
            builder.Add(pathSpan.Path);
        }

        return builder.ToImmutable();
    }

    private static string ExtractTableViewSegment(
        string xamlContent,
        int tableViewTagStartIndex,
        string tableViewStartTagText,
        Group prefixGroup)
    {
        if (tableViewStartTagText.EndsWith("/>", StringComparison.Ordinal))
        {
            return tableViewStartTagText;
        }

        var tableViewTagName = prefixGroup.Success
            ? $"{prefixGroup.Value}:TableView"
            : "TableView";

        var closingTag = $"</{tableViewTagName}>";
        var contentStart = tableViewTagStartIndex + tableViewStartTagText.Length;
        if (contentStart >= xamlContent.Length)
        {
            return tableViewStartTagText;
        }

        var closingIndex = xamlContent.IndexOf(closingTag, contentStart, StringComparison.OrdinalIgnoreCase);
        if (closingIndex < 0)
        {
            return tableViewStartTagText;
        }

        var totalLength = closingIndex + closingTag.Length - tableViewTagStartIndex;
        if (totalLength <= 0 || tableViewTagStartIndex + totalLength > xamlContent.Length)
        {
            return tableViewStartTagText;
        }

        return xamlContent.Substring(tableViewTagStartIndex, totalLength);
    }

    private static string ExtractStartTagText(string xamlContent, int tagStartIndex)
    {
        if (tagStartIndex < 0 || tagStartIndex >= xamlContent.Length)
        {
            return string.Empty;
        }

        var endIndex = xamlContent.IndexOf('>', tagStartIndex);
        if (endIndex < 0)
        {
            return string.Empty;
        }

        return xamlContent.Substring(tagStartIndex, endIndex - tagStartIndex + 1);
    }

    private static string? TryExtractItemsSourceXBindPath(
        string startTagText,
        int startTagGlobalIndex,
        out (int Start, int Length) itemsSourceSpanInText)
    {
        var bindingMatch = ItemsSourceBindingRegex.Match(startTagText);
        if (!bindingMatch.Success)
        {
            itemsSourceSpanInText = (-1, 0);
            return null;
        }

        itemsSourceSpanInText = (
            startTagGlobalIndex + bindingMatch.Index,
            Math.Max(1, "ItemsSource".Length));

        var kind = bindingMatch.Groups["kind"].Value.Trim();
        if (!kind.Equals("x:Bind", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var body = bindingMatch.Groups["body"].Value.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var firstSegment = body.Split(',')[0].Trim();
        if (string.IsNullOrWhiteSpace(firstSegment))
        {
            return null;
        }

        if (firstSegment.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
        {
            firstSegment = firstSegment.Substring("Path=".Length).Trim();
        }

        if (string.IsNullOrWhiteSpace(firstSegment) || firstSegment.Contains('='))
        {
            return null;
        }

        return firstSegment;
    }

    private static Location? CreateDiagnosticLocation(TableViewXamlInfo tableView)
    {
        if (!tableView.HasItemsSourceLocation
            || string.IsNullOrWhiteSpace(tableView.FilePath)
            || tableView.ItemsSourceSpanStart < 0
            || tableView.ItemsSourceSpanLength <= 0)
        {
            return null;
        }

        var textSpan = new TextSpan(tableView.ItemsSourceSpanStart, tableView.ItemsSourceSpanLength);
        var lineSpan = new LinePositionSpan(
            new LinePosition(tableView.ItemsSourceStartLine, tableView.ItemsSourceStartColumn),
            new LinePosition(tableView.ItemsSourceEndLine, tableView.ItemsSourceEndColumn));

        return Location.Create(tableView.FilePath, textSpan, lineSpan);
    }

    /// <summary>
    /// Finds the diagnostic location for the specified member path.
    /// </summary>
    /// <param name="pathLocations">Known locations keyed by member path text.</param>
    /// <param name="path">The member path to locate.</param>
    /// <returns>The matching source location, or <see langword="null"/> when not found.</returns>
    private static Location? CreatePathDiagnosticLocation(
        ImmutableArray<PathDiagnosticLocation> pathLocations,
        string path)
    {
        foreach (var pathLocation in pathLocations)
        {
            if (!pathLocation.Path.Equals(path, StringComparison.Ordinal))
            {
                continue;
            }

            var textSpan = new TextSpan(pathLocation.SpanStart, pathLocation.SpanLength);
            var lineSpan = new LinePositionSpan(
                new LinePosition(pathLocation.StartLine, pathLocation.StartColumn),
                new LinePosition(pathLocation.EndLine, pathLocation.EndColumn));

            return Location.Create(pathLocation.FilePath, textSpan, lineSpan);
        }

        return null;
    }

    private static CaseSourceInfo GetCaseSourceInfo(
        ImmutableArray<ColumnDefinitionInfo> columns,
        string memberPath,
        ColumnPathKind kind)
    {
        foreach (var column in columns)
        {
            var location = kind switch
            {
                ColumnPathKind.SortMemberPath => column.SortMemberPathLocation,
                ColumnPathKind.DisplayMemberPath => column.DisplayMemberPathLocation,
                ColumnPathKind.BindingPath => column.BindingPathLocation,
                ColumnPathKind.ClipboardBindingPath => column.ClipboardBindingPathLocation,
                ColumnPathKind.ContentBindingPath => column.ContentBindingPathLocation,
                _ => null
            };

            if (!location.HasValue || !location.Value.Path.Equals(memberPath, StringComparison.Ordinal))
            {
                continue;
            }

            return new CaseSourceInfo(
                column.StartLine,
                location.Value.StartLine + 1);
        }

        return CaseSourceInfo.None;
    }

    private static Location? GetClassLocation(Compilation compilation, string fullyQualifiedClassName)
    {
        var type = compilation.GetTypeByMetadataName(fullyQualifiedClassName);
        if (type is null)
        {
            return null;
        }

        foreach (var location in type.Locations)
        {
            if (location.IsInSource)
            {
                return location;
            }
        }

        return null;
    }

    private static bool HasConnectTableViewsCall(Compilation compilation, string fullyQualifiedClassName)
    {
        var type = compilation.GetTypeByMetadataName(fullyQualifiedClassName);
        if (type is null)
        {
            return false;
        }

        foreach (var declaration in type.DeclaringSyntaxReferences)
        {
            var syntax = declaration.GetSyntax();
            if (syntax is not TypeDeclarationSyntax typeDeclaration)
            {
                continue;
            }

            foreach (var node in typeDeclaration.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation)
                {
                    continue;
                }

                if (invocation.Expression is IdentifierNameSyntax identifier
                    && identifier.Identifier.ValueText.Equals("ConnectTableViews", StringComparison.Ordinal))
                {
                    return true;
                }

                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && memberAccess.Name.Identifier.ValueText.Equals("ConnectTableViews", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static ITypeSymbol? ResolveItemsSourceItemType(
        Compilation compilation,
        string fullyQualifiedClassName,
        string? xBindPath)
    {
        if (xBindPath is null)
        {
            return null;
        }

        var pageType = compilation.GetTypeByMetadataName(fullyQualifiedClassName);
        if (pageType is null)
        {
            return null;
        }

        var bindingPath = xBindPath.Trim();
        if (bindingPath.Length == 0)
        {
            return null;
        }

        if (bindingPath.StartsWith("this.", StringComparison.Ordinal))
        {
            bindingPath = bindingPath.Substring("this.".Length);
        }

        if (string.IsNullOrWhiteSpace(bindingPath))
        {
            return null;
        }

        ITypeSymbol currentType = pageType;
        var segments = bindingPath.Split('.');
        foreach (var rawSegment in segments)
        {
            var segment = rawSegment.Trim().TrimEnd('!', '?');
            if (string.IsNullOrWhiteSpace(segment)
                || segment.Contains('(')
                || segment.Contains('[')
                || segment.Contains(']'))
            {
                return null;
            }

            if (!TryGetInstanceMemberType(currentType, segment, out var memberType))
            {
                return null;
            }

            currentType = memberType;
        }

        if (!TryGetCollectionItemType(currentType, out var itemType))
        {
            return null;
        }

        return itemType;
    }

    private static bool TryGetInstanceMemberType(ITypeSymbol type, string memberName, out ITypeSymbol memberType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(memberName))
            {
                if (member.IsStatic)
                {
                    continue;
                }

                switch (member)
                {
                    case IPropertySymbol property:
                        memberType = property.Type;
                        return true;
                    case IFieldSymbol field:
                        memberType = field.Type;
                        return true;
                }
            }
        }

        memberType = null!;
        return false;
    }

    private static bool TryBuildValueAccessExpression(
        ITypeSymbol itemType,
        string memberPath,
        out string accessExpression,
        out ITypeSymbol memberType)
    {
        accessExpression = string.Empty;
        memberType = null!;
        if (string.IsNullOrWhiteSpace(memberPath))
        {
            return false;
        }

        var segments = memberPath.Split('.');
        if (segments.Length == 0)
        {
            return false;
        }

        var currentType = itemType;
        var builder = new StringBuilder();

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i].Trim();
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            if (!TryGetInstanceMemberType(currentType, segment, out memberType))
            {
                return false;
            }

            builder.Append('.').Append(segment);
            if (i < segments.Length - 1 && CanBeNull(memberType))
            {
                builder.Append('?');
            }

            currentType = UnwrapNullable(memberType);
        }

        memberType = currentType;
        accessExpression = builder.ToString();
        return true;
    }

    private static bool CanBeNull(ITypeSymbol type)
    {
        if (type.IsReferenceType)
        {
            return true;
        }

        return type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }

    private static bool TryGetCollectionItemType(ITypeSymbol sourceType, out ITypeSymbol itemType)
    {
        if (sourceType is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
            return true;
        }

        if (sourceType is INamedTypeSymbol namedType
            && namedType.IsGenericType
            && namedType.TypeArguments.Length == 1)
        {
            itemType = namedType.TypeArguments[0];
            return true;
        }

        itemType = null!;
        return false;
    }

    private static (string? NamespaceName, string ClassName) SplitClassName(string fullyQualifiedClassName)
    {
        var lastDotIndex = fullyQualifiedClassName.LastIndexOf('.');
        if (lastDotIndex <= 0 || lastDotIndex == fullyQualifiedClassName.Length - 1)
        {
            return (null, fullyQualifiedClassName);
        }

        return (
            fullyQualifiedClassName.Substring(0, lastDotIndex),
            fullyQualifiedClassName.Substring(lastDotIndex + 1));
    }

    private static string GetLineAtIndex(string content, int index)
    {
        if (index < 0 || index >= content.Length)
        {
            return string.Empty;
        }

        var lineStart = content.LastIndexOf('\n', index);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        var lineEnd = content.IndexOf('\n', index);
        lineEnd = lineEnd < 0 ? content.Length : lineEnd;

        var line = content.Substring(lineStart, lineEnd - lineStart);
        if (line.EndsWith("\r", StringComparison.Ordinal))
        {
            line = line.Substring(0, line.Length - 1);
        }

        return line;
    }

    private static string BuildSource(
        string? namespaceName,
        string className,
        ImmutableArray<GeneratedProviderInfo> providers)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine(";");
            builder.AppendLine();
        }

        builder.Append("partial class ")
               .Append(className)
               .Append(" : global::WinUI.TableView.ITableViewConnector")
               .AppendLine();
        builder.AppendLine("{");
        builder.AppendLine("    void global::WinUI.TableView.ITableViewConnector.ConnectTableView(global::WinUI.TableView.TableView @param_tableView)");
        builder.AppendLine("    {");
        var isFirstProvider = true;
        foreach (var provider in providers)
        {
            builder.Append("        ")
                   .Append(isFirstProvider ? "if" : "else if")
                   .Append(" (global::System.Object.ReferenceEquals(@param_tableView, this.")
                   .Append(provider.TableViewName)
                   .AppendLine("))");
            builder.AppendLine("        {");
            builder.Append("            @param_tableView.MemberValueProvider = new ")
                   .Append(provider.ProviderClassName)
                   .AppendLine("();");
            builder.AppendLine("        }");
            isFirstProvider = false;
        }
        builder.AppendLine("    }");
        builder.AppendLine();

        builder.AppendLine("    void global::WinUI.TableView.ITableViewConnector.ConnectTableViews()");
        builder.AppendLine("    {");
        foreach (var provider in providers)
        {
            builder.Append("        this.")
                   .Append(provider.TableViewName)
                   .Append(".MemberValueProvider = new ")
                   .Append(provider.ProviderClassName)
                   .AppendLine("();");
        }

        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var provider in providers)
        {
            builder.Append("    private sealed class ")
                   .Append(provider.ProviderClassName)
                   .Append(" : global::WinUI.TableView.ICellValueProvider")
                   .AppendLine();
            builder.AppendLine("    {");

            BuildMethodSource(builder, provider, provider.SortMemberCases, "TryGetSortMemberValue", ColumnPathKind.SortMemberPath);
            BuildMethodSource(builder, provider, provider.BindingPathCases, "TryGetBindingValue", ColumnPathKind.BindingPath);
            BuildMethodSource(builder, provider, provider.DisplayMemberCases, "TryGetDisplayMemberValue", ColumnPathKind.DisplayMemberPath);
            BuildMethodSource(builder, provider, provider.ClipboardPathCases, "TryGetClipboardContentBindingValue", ColumnPathKind.ClipboardBindingPath);
            BuildMethodSource(builder, provider, provider.ContentPathCases, "TryGetContentBindingValue", ColumnPathKind.ContentBindingPath);

            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void BuildMethodSource(StringBuilder builder, GeneratedProviderInfo provider, ImmutableArray<GeneratedValueCase> memberCases, string methodName, ColumnPathKind pathKind)
    {
        var itemRootIdentifier = "typedItem";

        builder.Append("        bool global::WinUI.TableView.ICellValueProvider.")
               .Append(methodName)
               .Append("(global::System.String? path, global::System.Object? item, out global::System.Object? value)")
               .AppendLine();
        builder.AppendLine("        {");
        builder.Append("            var ")
               .Append(itemRootIdentifier)
               .Append(" = item as ")
               .Append(provider.ItemTypeDisplay)
               .AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("            switch (path)");
        builder.AppendLine("            {");

        //itemRootIdentifier = pathKind is ColumnPathKind.DisplayMemberPath ? "typedCellValue" : itemRootIdentifier;
        var generatedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var memberCase in memberCases)
        {
            if (!generatedPaths.Add(memberCase.MemberPath))
            {
                continue;
            }


            if (memberCase.SourceInfo.HasValue)
            {
                builder.Append("                // ")
                       .Append("column line ")
                       .Append(memberCase.SourceInfo.ColumnStartLine)
                       .Append(", property line ")
                       .Append(memberCase.SourceInfo.PathLine)
                       .AppendLine();
            }

            builder.Append("                case \"")
                   .Append(memberCase.MemberPath.Replace("\"", "\\\""))
                   .Append("\":");

            if (pathKind is ColumnPathKind.DisplayMemberPath && false)
            {
                var cellValueCase = provider.BindingPathCases.First(c => c.SourceInfo.ColumnStartLine == memberCase.SourceInfo.ColumnStartLine);

                builder.AppendLine();
                builder.Append("                    if (!TryGetCellValue(\"")
                       .Append(cellValueCase.MemberPath)
                       .Append("\", item, out var cellValue) || cellValue is not ")
                       .Append(memberCase.MemberTypeDisplay)
                       .Append(" ")
                       .Append(itemRootIdentifier)
                       .AppendLine(" )");
                builder.AppendLine("                    {");
                builder.AppendLine("                        value = null;");
                builder.AppendLine("                        return false;");
                builder.AppendLine("                    }");
            }

            builder.AppendLine();
            builder.Append("                    value = ")
                   .Append(itemRootIdentifier)
                   .Append("?")
                   .Append(memberCase.AccessExpression)
                   .AppendLine(";");
            builder.AppendLine("                    return true;");
        }

        builder.AppendLine("                default:");
        builder.AppendLine("                    value = null;");
        builder.AppendLine("                    return false;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static string EnsureGlobalQualified(string typeDisplay)
    {
        var trimmed = typeDisplay.Trim();
        return trimmed.StartsWith("global::", StringComparison.Ordinal)
            ? trimmed
            : "global::" + trimmed;
    }

    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unnamed";
        }

        var buffer = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            var valid = i == 0 ? char.IsLetter(ch) || ch == '_' : char.IsLetterOrDigit(ch) || ch == '_';
            buffer.Append(valid ? ch : '_');
        }

        if (buffer.Length == 0 || !(char.IsLetter(buffer[0]) || buffer[0] == '_'))
        {
            buffer.Insert(0, '_');
        }

        return buffer.ToString();
    }

    private static string GetUniqueIdentifier(string baseName, HashSet<string> used)
    {
        var candidate = baseName;
        var suffix = 1;
        while (!used.Add(candidate))
        {
            candidate = baseName + "_" + suffix;
            suffix++;
        }

        return candidate;
    }

    private readonly struct ParsedXamlInfo
    {
        public ParsedXamlInfo(
            string? namespaceName,
            string className,
            string fullyQualifiedClassName,
            string hintName,
            bool isWindowRoot,
            ImmutableArray<TableViewXamlInfo> tableViews)
        {
            NamespaceName = namespaceName;
            ClassName = className;
            FullyQualifiedClassName = fullyQualifiedClassName;
            HintName = hintName;
            IsWindowRoot = isWindowRoot;
            TableViews = tableViews;
        }

        public string? NamespaceName { get; }

        public string ClassName { get; }

        public string FullyQualifiedClassName { get; }

        public string HintName { get; }

        public bool IsWindowRoot { get; }

        public ImmutableArray<TableViewXamlInfo> TableViews { get; }
    }

    /// <summary>
    /// Raw TableView data parsed from XAML text before Roslyn line mapping.
    /// </summary>
    private readonly struct TableViewInfoRaw
    {
        public TableViewInfoRaw(
            string? tableViewName,
            string tableViewLine,
            string? itemsSourceXBindPath,
            ImmutableArray<string> sortMemberPaths,
            ImmutableArray<string> displayMemberPaths,
            ImmutableArray<string> bindingPaths,
            ImmutableArray<string> clipboardPaths,
            ImmutableArray<string> contentPaths,
            ImmutableArray<ColumnDefinitionRaw> columnDefinitions,
            ImmutableArray<PathSpanRaw> sortMemberPathSpans,
            ImmutableArray<PathSpanRaw> displayMemberPathSpans,
            ImmutableArray<PathSpanRaw> bindingPathSpans,
            ImmutableArray<PathSpanRaw> clipboardPathSpans,
            ImmutableArray<PathSpanRaw> contentPathSpans,
            int itemsSourceSpanStart,
            int itemsSourceSpanLength)
        {
            TableViewName = tableViewName;
            TableViewLine = tableViewLine;
            ItemsSourceXBindPath = itemsSourceXBindPath;
            SortMemberPaths = sortMemberPaths;
            DisplayMemberPaths = displayMemberPaths;
            BindingPaths = bindingPaths;
            ClipboardPaths = clipboardPaths;
            ContentPaths = contentPaths;
            ColumnDefinitions = columnDefinitions;
            SortMemberPathSpans = sortMemberPathSpans;
            DisplayMemberPathSpans = displayMemberPathSpans;
            BindingPathSpans = bindingPathSpans;
            ClipboardPathSpans = clipboardPathSpans;
            ContentPathSpans = contentPathSpans;
            ItemsSourceSpanStart = itemsSourceSpanStart;
            ItemsSourceSpanLength = itemsSourceSpanLength;
        }

        public string? TableViewName { get; }

        public string TableViewLine { get; }

        public string? ItemsSourceXBindPath { get; }

        /// <summary>
        /// Sort member paths extracted from column definitions.
        /// </summary>
        public ImmutableArray<string> SortMemberPaths { get; }

        /// <summary>
        /// Display member paths extracted from <c>DisplayMemberPath</c>.
        /// </summary>
        public ImmutableArray<string> DisplayMemberPaths { get; }

        /// <summary>
        /// Cell value paths extracted from <c>CellValuePath</c>.
        /// </summary>
        public ImmutableArray<string> BindingPaths { get; }

        /// <summary>
        /// Clipboard member paths extracted from <c>ClipboardPath</c>.
        /// </summary>
        public ImmutableArray<string> ClipboardPaths { get; }

        /// <summary>
        /// Clipboard member paths extracted from <c>ContentPath</c>.
        /// </summary>
        public ImmutableArray<string> ContentPaths { get; }

        /// <summary>
        /// Parsed column definitions keyed by source line based <c>ColumnId</c>.
        /// </summary>
        public ImmutableArray<ColumnDefinitionRaw> ColumnDefinitions { get; }

        /// <summary>
        /// Absolute source spans for <see cref="SortMemberPaths"/>.
        /// </summary>
        public ImmutableArray<PathSpanRaw> SortMemberPathSpans { get; }

        /// <summary>
        /// Absolute source spans for <see cref="DisplayMemberPaths"/>.
        /// </summary>
        public ImmutableArray<PathSpanRaw> DisplayMemberPathSpans { get; }

        /// <summary>
        /// Absolute source spans for <see cref="BindingPaths"/>.
        /// </summary>
        public ImmutableArray<PathSpanRaw> BindingPathSpans { get; }

        /// <summary>
        /// Absolute source spans for <see cref="ClipboardPaths"/>.
        /// </summary>
        public ImmutableArray<PathSpanRaw> ClipboardPathSpans { get; }

        /// <summary>
        /// Absolute source spans for <see cref="ContentPaths"/>.
        /// </summary>
        public ImmutableArray<PathSpanRaw> ContentPathSpans { get; }

        public int ItemsSourceSpanStart { get; }

        public int ItemsSourceSpanLength { get; }
    }

    /// <summary>
    /// Parsed TableView data enriched with file and line/column diagnostics metadata.
    /// </summary>
    private readonly struct TableViewXamlInfo
    {
        public TableViewXamlInfo(
            string? tableViewName,
            string tableViewLine,
            string? itemsSourceXBindPath,
            ImmutableArray<string> sortMemberPaths,
            ImmutableArray<string> displayMemberPaths,
            ImmutableArray<string> cellValuePaths,
            ImmutableArray<string> clipboardMemberPaths,
            ImmutableArray<string> contentPaths,
            ImmutableArray<ColumnDefinitionInfo> columnDefinitions,
            ImmutableArray<PathDiagnosticLocation> sortMemberPathLocations,
            ImmutableArray<PathDiagnosticLocation> displayMemberPathLocations,
            ImmutableArray<PathDiagnosticLocation> cellValuePathLocations,
            ImmutableArray<PathDiagnosticLocation> clipboardPathLocations,
            ImmutableArray<PathDiagnosticLocation> contentPathLocations,
            bool hasItemsSourceLocation,
            string filePath,
            int itemsSourceSpanStart,
            int itemsSourceSpanLength,
            int itemsSourceStartLine,
            int itemsSourceStartColumn,
            int itemsSourceEndLine,
            int itemsSourceEndColumn)
        {
            TableViewName = tableViewName;
            TableViewLine = tableViewLine;
            ItemsSourceXBindPath = itemsSourceXBindPath;
            SortMemberPaths = sortMemberPaths;
            DisplayMemberPaths = displayMemberPaths;
            BindingPaths = cellValuePaths;
            ClipboardPaths = clipboardMemberPaths;
            ContentPaths = contentPaths;
            ColumnDefinitions = columnDefinitions;
            SortMemberPathLocations = sortMemberPathLocations;
            DisplayMemberPathLocations = displayMemberPathLocations;
            BindingPathLocations = cellValuePathLocations;
            ClipboardPathLocations = clipboardPathLocations;
            ContentPathLocations = contentPathLocations;
            HasItemsSourceLocation = hasItemsSourceLocation;
            FilePath = filePath;
            ItemsSourceSpanStart = itemsSourceSpanStart;
            ItemsSourceSpanLength = itemsSourceSpanLength;
            ItemsSourceStartLine = itemsSourceStartLine;
            ItemsSourceStartColumn = itemsSourceStartColumn;
            ItemsSourceEndLine = itemsSourceEndLine;
            ItemsSourceEndColumn = itemsSourceEndColumn;
        }

        public string? TableViewName { get; }

        public string TableViewLine { get; }

        public string? ItemsSourceXBindPath { get; }

        /// <summary>
        /// Sort member paths extracted from column definitions.
        /// </summary>
        public ImmutableArray<string> SortMemberPaths { get; }

        /// <summary>
        /// Display member paths extracted from <c>DisplayMemberPath</c>.
        /// </summary>
        public ImmutableArray<string> DisplayMemberPaths { get; }

        /// <summary>
        /// Cell value paths extracted from <c>CellValuePath</c>.
        /// </summary>
        public ImmutableArray<string> BindingPaths { get; }

        /// <summary>
        /// Clipboard member paths extracted from <c>ClipboardPath</c>.
        /// </summary>
        public ImmutableArray<string> ClipboardPaths { get; }

        /// <summary>
        /// Content member paths extracted from <c>ContentPath</c>.
        /// </summary>
        public ImmutableArray<string> ContentPaths { get; }

        /// <summary>
        /// Parsed column definitions keyed by source line based <c>ColumnId</c>.
        /// </summary>
        public ImmutableArray<ColumnDefinitionInfo> ColumnDefinitions { get; }

        /// <summary>
        /// Diagnostic locations for <see cref="SortMemberPaths"/>.
        /// </summary>
        public ImmutableArray<PathDiagnosticLocation> SortMemberPathLocations { get; }

        /// <summary>
        /// Diagnostic locations for <see cref="DisplayMemberPaths"/>.
        /// </summary>
        public ImmutableArray<PathDiagnosticLocation> DisplayMemberPathLocations { get; }

        /// <summary>
        /// Diagnostic locations for <see cref="BindingPaths"/>.
        /// </summary>
        public ImmutableArray<PathDiagnosticLocation> BindingPathLocations { get; }

        /// <summary>
        /// Diagnostic locations for <see cref="ClipboardPaths"/>.
        /// </summary>
        public ImmutableArray<PathDiagnosticLocation> ClipboardPathLocations { get; }

        /// <summary>
        /// Diagnostic locations for <see cref="ContentPaths"/>.
        /// </summary>
        public ImmutableArray<PathDiagnosticLocation> ContentPathLocations { get; }

        public bool HasItemsSourceLocation { get; }

        public string FilePath { get; }

        public int ItemsSourceSpanStart { get; }

        public int ItemsSourceSpanLength { get; }

        public int ItemsSourceStartLine { get; }

        public int ItemsSourceStartColumn { get; }

        public int ItemsSourceEndLine { get; }

        public int ItemsSourceEndColumn { get; }
    }

    /// <summary>
    /// Parsed raw column definition with a stable <c>ColumnId</c> derived from start line.
    /// </summary>
    private readonly struct ColumnDefinitionRaw
    {
        public ColumnDefinitionRaw(
            int startOffset,
            int startLine,
            PathSpanRaw? sortMemberPathSpan,
            PathSpanRaw? displayMemberPathSpan,
            PathSpanRaw? bindingPathSpan,
            PathSpanRaw? clipboardBindingPathSpan,
            PathSpanRaw? contentBindingPathSpan)
        {
            StartOffset = startOffset;
            StartLine = startLine;
            SortMemberPathSpan = sortMemberPathSpan;
            DisplayMemberPathSpan = displayMemberPathSpan;
            BindingPathSpan = bindingPathSpan;
            ClipboardPathSpan = clipboardBindingPathSpan;
            ContentPathSpan = contentBindingPathSpan;
        }

        public int StartOffset { get; }

        public int StartLine { get; }

        public PathSpanRaw? SortMemberPathSpan { get; }

        public PathSpanRaw? DisplayMemberPathSpan { get; }

        public PathSpanRaw? BindingPathSpan { get; }

        public PathSpanRaw? ClipboardPathSpan { get; }

        public PathSpanRaw? ContentPathSpan { get; }
    }

    /// <summary>
    /// Column definition enriched with file and line/column diagnostics metadata.
    /// </summary>
    private readonly struct ColumnDefinitionInfo
    {
        public ColumnDefinitionInfo(
            int startLine,
            PathDiagnosticLocation? sortMemberPathLocation,
            PathDiagnosticLocation? displayMemberPathLocation,
            PathDiagnosticLocation? cellBindingPathLocation,
            PathDiagnosticLocation? clipboardBindingPathLocation,
            PathDiagnosticLocation? contentBindingPathLocation)
        {
            StartLine = startLine;
            SortMemberPathLocation = sortMemberPathLocation;
            DisplayMemberPathLocation = displayMemberPathLocation;
            BindingPathLocation = cellBindingPathLocation;
            ClipboardBindingPathLocation = clipboardBindingPathLocation;
            ContentBindingPathLocation = contentBindingPathLocation;
        }

        public int StartLine { get; }

        public PathDiagnosticLocation? SortMemberPathLocation { get; }

        public PathDiagnosticLocation? DisplayMemberPathLocation { get; }

        public PathDiagnosticLocation? BindingPathLocation { get; }

        public PathDiagnosticLocation? ClipboardBindingPathLocation { get; }

        public PathDiagnosticLocation? ContentBindingPathLocation { get; }
    }

    /// <summary>
    /// Represents an extracted member path and its absolute span in the source file.
    /// </summary>
    private readonly struct PathSpanRaw
    {
        public PathSpanRaw(string path, int spanStart, int spanLength)
        {
            Path = path;
            SpanStart = spanStart;
            SpanLength = spanLength;
        }

        /// <summary>
        /// The extracted member path text.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The absolute zero-based start index in the source file.
        /// </summary>
        public int SpanStart { get; }

        /// <summary>
        /// The length of the path span.
        /// </summary>
        public int SpanLength { get; }
    }

    /// <summary>
    /// Stores a member path and the exact source mapping used for diagnostics.
    /// </summary>
    private readonly struct PathDiagnosticLocation
    {
        public PathDiagnosticLocation(
            string path,
            string filePath,
            int spanStart,
            int spanLength,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Path = path;
            FilePath = filePath;
            SpanStart = spanStart;
            SpanLength = spanLength;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        /// <summary>
        /// The extracted member path text.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The source file path containing the path token.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The absolute zero-based start index in the source file.
        /// </summary>
        public int SpanStart { get; }

        /// <summary>
        /// The length of the path span.
        /// </summary>
        public int SpanLength { get; }

        /// <summary>
        /// Zero-based starting line for the path token.
        /// </summary>
        public int StartLine { get; }

        /// <summary>
        /// Zero-based starting column for the path token.
        /// </summary>
        public int StartColumn { get; }

        /// <summary>
        /// Zero-based ending line for the path token.
        /// </summary>
        public int EndLine { get; }

        /// <summary>
        /// Zero-based ending column for the path token.
        /// </summary>
        public int EndColumn { get; }
    }

    /// <summary>
    /// Code-generation model for one generated provider associated with a TableView.
    /// </summary>
    private readonly struct GeneratedProviderInfo
    {
        public GeneratedProviderInfo(
            string tableViewName,
            string providerClassName,
            string tableViewLine,
            string itemTypeDisplay,
            ImmutableArray<GeneratedValueCase> sortMemberCases,
            ImmutableArray<GeneratedValueCase> displayMemberCases,
            ImmutableArray<GeneratedValueCase> bindingPathCases,
            ImmutableArray<GeneratedValueCase> clipboardPathCases,
            ImmutableArray<GeneratedValueCase> contentPathCases)
        {
            TableViewName = tableViewName;
            ProviderClassName = providerClassName;
            TableViewLine = tableViewLine;
            ItemTypeDisplay = itemTypeDisplay;
            SortMemberCases = sortMemberCases;
            DisplayMemberCases = displayMemberCases;
            BindingPathCases = bindingPathCases;
            ClipboardPathCases = clipboardPathCases;
            ContentPathCases = contentPathCases;
        }

        public string TableViewName { get; }

        public string ProviderClassName { get; }

        public string TableViewLine { get; }

        public string ItemTypeDisplay { get; }

        /// <summary>
        /// Generated access cases for sort member paths.
        /// </summary>
        public ImmutableArray<GeneratedValueCase> SortMemberCases { get; }

        /// <summary>
        /// Generated access cases for display member paths.
        /// </summary>
        public ImmutableArray<GeneratedValueCase> DisplayMemberCases { get; }

        /// <summary>
        /// Generated access cases for cell value paths.
        /// </summary>
        public ImmutableArray<GeneratedValueCase> BindingPathCases { get; }

        /// <summary>
        /// Generated access cases for clipboard member paths.
        /// </summary>
        public ImmutableArray<GeneratedValueCase> ClipboardPathCases { get; }

        /// <summary>
        /// Generated access cases for clipboard member paths.
        /// </summary>
        public ImmutableArray<GeneratedValueCase> ContentPathCases { get; }
    }

    private readonly struct GeneratedValueCase
    {
        public GeneratedValueCase(string memberPath, string accessExpression, CaseSourceInfo sourceInfo, string? memberTypeDisplay = null)
        {
            MemberPath = memberPath;
            AccessExpression = accessExpression;
            SourceInfo = sourceInfo;
            MemberTypeDisplay = memberTypeDisplay;
        }

        public string MemberPath { get; }

        public string AccessExpression { get; }

        public CaseSourceInfo SourceInfo { get; }

        public string? MemberTypeDisplay { get; }
    }

    private readonly struct CaseSourceInfo
    {
        public static CaseSourceInfo None => new(0, 0);

        public CaseSourceInfo(int columnStartLine, int pathLine)
        {
            ColumnStartLine = columnStartLine;
            PathLine = pathLine;
        }

        public int ColumnStartLine { get; }

        public int PathLine { get; }

        public bool HasValue => ColumnStartLine > 0 && PathLine > 0;
    }

    private enum ColumnPathKind
    {
        SortMemberPath,
        DisplayMemberPath,
        BindingPath,
        ClipboardBindingPath,
        ContentBindingPath
    }
}


