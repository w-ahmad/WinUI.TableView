using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace WinUI.TableView.SourceGenerators;

[Generator]
public sealed partial class TableViewBindingProviderGenerator : IIncrementalGenerator
{
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

                    var itemTypeDisplay = GetGlobalTypeDisplay(itemType);

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
                    var bindingSetCases = ImmutableArray.CreateBuilder<GeneratedSetValueCase>();
                    var usedBindingSetterMethodNames = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var memberPath in bindingPathCandidates)
                    {
                        if (TryBuildValueAccessExpression(itemType, memberPath, out var accessExpression, out var memberType))
                        {
                            var sourceInfo = GetCaseSourceInfo(tableView.ColumnDefinitions, memberPath, ColumnPathKind.BindingPath);
                            bindingPathCases.Add(new GeneratedValueCase(
                                memberPath,
                                accessExpression,
                                sourceInfo,
                                GetGlobalTypeDisplay(memberType)));

                            if (!IsBindingPathSettable(tableView.ColumnDefinitions, memberPath))
                            {
                                continue;
                            }

                            var editorKind = GetBindingEditorKind(tableView.ColumnDefinitions, sourceInfo.ColumnStartLine);
                            if (TryBuildSetValueCase(itemType, memberPath, sourceInfo, editorKind, out var setCase))
                            {
                                var localMethodName = GetUniqueIdentifier(setCase.LocalMethodName, usedBindingSetterMethodNames);
                                bindingSetCases.Add(new GeneratedSetValueCase(
                                    setCase.MemberPath,
                                    localMethodName,
                                    setCase.SourceInfo,
                                    setCase.NavigationSegments,
                                    setCase.LeafMemberName,
                                    setCase.LeafValueTypeDisplay,
                                    setCase.CanAssignNull,
                                    setCase.IsLeafNullableValueType,
                                    setCase.HasNumericConversion,
                                    setCase.NumericConversionHelperMethodName,
                                    setCase.NumericConvertMethodName,
                                    setCase.HasDateConversion,
                                    setCase.DateConversionHelperMethodName,
                                    setCase.HasTimeConversion,
                                    setCase.TimeConversionHelperMethodName));
                            }

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
                        itemTypeDisplay!,
                        sortCases.ToImmutable(),
                        displayMemberPathCases.ToImmutable(),
                        bindingPathCases.ToImmutable(),
                        bindingSetCases.ToImmutable(),
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
                    column.EditorKind,
                    column.BindingMode,
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
            var columnTypeText = columnMatch.Groups["columnType"].Success
                ? columnMatch.Groups["columnType"].Value
                : string.Empty;
            var attrsStart = tableViewSegmentStart + attrsGroup.Index;
            var absoluteColumnStart = tableViewSegmentStart + columnMatch.Index;
            var columnStartLine = GetLineNumber(fullXamlContent, absoluteColumnStart);
            var editorKind = GetBindingEditorKind(columnTypeText);

            var sortMemberPathSpan = TryExtractPathSpan(attrsText, attrsStart, SortMemberPathRegex);
            var displayMemberPathSpan = TryExtractPathSpan(attrsText, attrsStart, DisplayMemberPathRegex);
            var bindingExtraction = TryExtractBindingPathSpanWithMode(attrsText, attrsStart, CellBindingRegex);
            var bindingPathSpan = bindingExtraction.PathSpan;
            var clipboardBindingPathSpan = TryExtractBindingPathSpan(attrsText, attrsStart, ClipboardBindingRegex);
            var contentBindingPathSpan = TryExtractBindingPathSpan(attrsText, attrsStart, ContentBindingRegex);

            builder.Add(new ColumnDefinitionRaw(
                absoluteColumnStart,
                columnStartLine,
                editorKind,
                bindingExtraction.BindingMode,
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
        return TryExtractBindingPathSpanWithMode(attrsText, attrsStart, bindingRegex).PathSpan;
    }

    private static (PathSpanRaw? PathSpan, BindingMode BindingMode) TryExtractBindingPathSpanWithMode(string attrsText, int attrsStart, Regex bindingRegex)
    {
        if (string.IsNullOrWhiteSpace(attrsText))
        {
            return (null, BindingMode.TwoWay);
        }

        var bindingMatch = bindingRegex.Match(attrsText);
        if (!bindingMatch.Success)
        {
            return (null, BindingMode.TwoWay);
        }

        var bodyGroup = bindingMatch.Groups["body"];
        var bodyText = bodyGroup.Value;
        var path = TryExtractBindingPath(bodyText);
        if (string.IsNullOrWhiteSpace(path))
        {
            return (null, BindingMode.TwoWay);
        }

        var bindingMode = TryExtractBindingMode(bodyText);
        var body = bodyText;
        var pathOffsetInBody = body.IndexOf(path, StringComparison.Ordinal);
        if (pathOffsetInBody < 0)
        {
            pathOffsetInBody = 0;
        }

        var resolvedPath = path!;
        var pathStart = attrsStart + bodyGroup.Index + pathOffsetInBody;
        return (new PathSpanRaw(resolvedPath, pathStart, Math.Max(1, resolvedPath.Length)), bindingMode);
    }

    private static BindingMode TryExtractBindingMode(string bindingBody)
    {
        if (string.IsNullOrWhiteSpace(bindingBody))
        {
            return BindingMode.TwoWay;
        }

        var modeMatch = BindingModeTokenRegex.Match(bindingBody.Trim());
        if (!modeMatch.Success)
        {
            return BindingMode.TwoWay;
        }

        var modeText = modeMatch.Groups["mode"].Value.Trim();
        modeText = modeText.Trim('\"', '\'');
        if (modeText.Equals("TwoWay", StringComparison.OrdinalIgnoreCase))
        {
            return BindingMode.TwoWay;
        }

        if (modeText.Equals("OneWay", StringComparison.OrdinalIgnoreCase))
        {
            return BindingMode.OneWay;
        }

        if (modeText.Equals("OneTime", StringComparison.OrdinalIgnoreCase))
        {
            return BindingMode.OneTime;
        }

        return BindingMode.TwoWay;
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

    private static BindingEditorKind GetBindingEditorKind(string columnTypeName)
    {
        if (columnTypeName.Equals("TableViewDateColumn", StringComparison.OrdinalIgnoreCase))
        {
            return BindingEditorKind.Date;
        }

        if (columnTypeName.Equals("TableViewTimeColumn", StringComparison.OrdinalIgnoreCase))
        {
            return BindingEditorKind.Time;
        }

        return BindingEditorKind.Other;
    }

    private static BindingEditorKind GetBindingEditorKind(ImmutableArray<ColumnDefinitionInfo> columns, int columnStartLine)
    {
        if (columnStartLine <= 0)
        {
            return BindingEditorKind.Unknown;
        }

        foreach (var column in columns)
        {
            if (column.StartLine == columnStartLine)
            {
                return column.EditorKind;
            }
        }

        return BindingEditorKind.Unknown;
    }

    private static bool IsBindingPathSettable(ImmutableArray<ColumnDefinitionInfo> columns, string memberPath)
    {
        foreach (var column in columns)
        {
            if (column.BindingPathLocation.HasValue
                && column.BindingPathLocation.Value.Path.Equals(memberPath, StringComparison.Ordinal)
                && column.BindingMode == BindingMode.TwoWay)
            {
                return true;
            }
        }

        return false;
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
        if (TryGetInstanceMember(type, memberName, out _, out memberType))
        {
            return true;
        }

        memberType = null!;
        return false;
    }

    private static bool TryGetInstanceMember(ITypeSymbol type, string memberName, out ISymbol memberSymbol, out ITypeSymbol memberType)
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
                        memberSymbol = property;
                        memberType = property.Type;
                        return true;
                    case IFieldSymbol field:
                        memberSymbol = field;
                        memberType = field.Type;
                        return true;
                }
            }
        }

        memberSymbol = null!;
        memberType = null!;
        return false;
    }

    private static bool TryBuildSetValueCase(
        ITypeSymbol itemType,
        string memberPath,
        CaseSourceInfo sourceInfo,
        BindingEditorKind editorKind,
        out GeneratedSetValueCase setValueCase)
    {
        setValueCase = default;
        if (string.IsNullOrWhiteSpace(memberPath))
        {
            return false;
        }

        var rawSegments = memberPath.Split('.');
        if (rawSegments.Length == 0)
        {
            return false;
        }

        var segments = new string[rawSegments.Length];
        for (var i = 0; i < rawSegments.Length; i++)
        {
            var segment = rawSegments[i].Trim();
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            segments[i] = segment;
        }

        var currentType = itemType;
        var navBuilder = ImmutableArray.CreateBuilder<SetNavigationSegment>();
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (!TryGetInstanceMember(currentType, segment, out var memberSymbol, out var memberType))
            {
                return false;
            }

            var unwrappedType = UnwrapNullable(memberType);
            var memberTypeDisplay = GetGlobalTypeDisplay(unwrappedType);

            if (i == segments.Length - 1)
            {
                if (!IsWritableMember(memberSymbol))
                {
                    return false;
                }

                var leafType = memberType;
                var leafUnderlyingType = UnwrapNullable(leafType);
                var hasNumericConversion = TryGetNumericConversionInfo(leafUnderlyingType, out var numericConversionHelperName, out var numericConvertMethodName);
                var hasDateConversion = TryGetDateConversionInfo(editorKind, leafUnderlyingType, out var dateConversionHelperMethodName);
                var hasTimeConversion = TryGetTimeConversionInfo(editorKind, leafUnderlyingType, out var timeConversionHelperMethodName);
                setValueCase = new GeneratedSetValueCase(
                    memberPath,
                    localMethodName: "TrySet_" + SanitizeIdentifier(memberPath.Replace('.', '_')),
                    sourceInfo,
                    navBuilder.ToImmutable(),
                    segment,
                    GetGlobalTypeDisplay(leafUnderlyingType),
                    CanBeNull(leafType),
                    IsNullableValueType(leafType),
                    hasNumericConversion,
                    numericConversionHelperName,
                    numericConvertMethodName,
                    hasDateConversion,
                    dateConversionHelperMethodName,
                    hasTimeConversion,
                    timeConversionHelperMethodName);
                return true;
            }

            navBuilder.Add(new SetNavigationSegment(segment, memberTypeDisplay));
            currentType = unwrappedType;
        }

        return false;
    }

    private static bool IsWritableMember(ISymbol memberSymbol)
    {
        return memberSymbol switch
        {
            IPropertySymbol property => property.SetMethod is not null && !property.SetMethod.IsStatic,
            IFieldSymbol field => !field.IsReadOnly && !field.IsConst && !field.IsStatic,
            _ => false
        };
    }

    private static bool TryGetNumericConversionInfo(ITypeSymbol type, out string helperMethodName, out string convertMethodName)
    {
        helperMethodName = string.Empty;
        convertMethodName = string.Empty;

        switch (type.SpecialType)
        {
            case SpecialType.System_Byte:
                helperMethodName = "TryGetByteFromDouble";
                convertMethodName = "ToByte";
                return true;
            case SpecialType.System_SByte:
                helperMethodName = "TryGetSByteFromDouble";
                convertMethodName = "ToSByte";
                return true;
            case SpecialType.System_Int16:
                helperMethodName = "TryGetInt16FromDouble";
                convertMethodName = "ToInt16";
                return true;
            case SpecialType.System_UInt16:
                helperMethodName = "TryGetUInt16FromDouble";
                convertMethodName = "ToUInt16";
                return true;
            case SpecialType.System_Int32:
                helperMethodName = "TryGetInt32FromDouble";
                convertMethodName = "ToInt32";
                return true;
            case SpecialType.System_UInt32:
                helperMethodName = "TryGetUInt32FromDouble";
                convertMethodName = "ToUInt32";
                return true;
            case SpecialType.System_Int64:
                helperMethodName = "TryGetInt64FromDouble";
                convertMethodName = "ToInt64";
                return true;
            case SpecialType.System_UInt64:
                helperMethodName = "TryGetUInt64FromDouble";
                convertMethodName = "ToUInt64";
                return true;
            case SpecialType.System_Single:
                helperMethodName = "TryGetSingleFromDouble";
                convertMethodName = "ToSingle";
                return true;
            case SpecialType.System_Double:
                helperMethodName = "TryGetDoubleFromDouble";
                convertMethodName = "ToDouble";
                return true;
            case SpecialType.System_Decimal:
                helperMethodName = "TryGetDecimalFromDouble";
                convertMethodName = "ToDecimal";
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetDateConversionInfo(BindingEditorKind editorKind, ITypeSymbol type, out string helperMethodName)
    {
        helperMethodName = string.Empty;
        if (editorKind != BindingEditorKind.Date)
        {
            return false;
        }

        if (type.SpecialType == SpecialType.System_DateTime)
        {
            helperMethodName = "TryGetDateTimeFromDateTimeOffset";
            return true;
        }

        if (type is INamedTypeSymbol namedType
            && namedType.ContainingNamespace?.ToDisplayString() == "System")
        {
            if (namedType.Name == "DateOnly")
            {
                helperMethodName = "TryGetDateOnlyFromDateTimeOffset";
                return true;
            }

            if (namedType.Name == "DateTimeOffset")
            {
                helperMethodName = "TryGetDateTimeOffsetFromDateTimeOffset";
                return true;
            }
        }

        return false;
    }

    private static bool TryGetTimeConversionInfo(BindingEditorKind editorKind, ITypeSymbol type, out string helperMethodName)
    {
        helperMethodName = string.Empty;
        if (editorKind != BindingEditorKind.Time)
        {
            return false;
        }

        if (type is INamedTypeSymbol namedType
            && namedType.ContainingNamespace?.ToDisplayString() == "System")
        {
            if (namedType.Name == "TimeSpan")
            {
                helperMethodName = "TryGetTimeSpanFromTimeSpan";
                return true;
            }

            if (namedType.Name == "TimeOnly")
            {
                helperMethodName = "TryGetTimeOnlyFromTimeSpan";
                return true;
            }

            if (namedType.Name == "DateTime")
            {
                helperMethodName = "TryGetDateTimeFromTimeSpan";
                return true;
            }

            if (namedType.Name == "DateTimeOffset")
            {
                helperMethodName = "TryGetDateTimeOffsetFromTimeSpan";
                return true;
            }
        }

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

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
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
            BuildSetterMethodSource(builder, provider);
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

    private static void BuildSetterMethodSource(StringBuilder builder, GeneratedProviderInfo provider)
    {
        builder.AppendLine("        bool global::WinUI.TableView.ICellValueProvider.TrySetBindingValue(global::System.String? path, global::System.Object? item, global::System.Object? value)");
        builder.AppendLine("        {");
        builder.Append("            if (item is not ")
               .Append(provider.ItemTypeDisplay)
               .AppendLine(" typedItem)");
        builder.AppendLine("            {");
        builder.AppendLine("                return false;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            switch (path)");
        builder.AppendLine("            {");

        var generatedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var setCase in provider.BindingSetCases)
        {
            if (!generatedPaths.Add(setCase.MemberPath))
            {
                continue;
            }

            if (setCase.SourceInfo.HasValue)
            {
                builder.Append("                // ")
                       .Append("column line ")
                       .Append(setCase.SourceInfo.ColumnStartLine)
                       .Append(", property line ")
                       .Append(setCase.SourceInfo.PathLine)
                       .AppendLine();
            }

            builder.Append("                case \"")
                   .Append(setCase.MemberPath.Replace("\"", "\\\""))
                   .AppendLine("\":");
            builder.Append("                    return ")
                   .Append(setCase.LocalMethodName)
                   .AppendLine("(typedItem, value);");
        }

        builder.AppendLine("                default:");
        builder.AppendLine("                    return false;");
        builder.AppendLine("            }");
        builder.AppendLine();

        foreach (var setCase in provider.BindingSetCases)
        {
            var isDateTimeTarget =
                string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTime", StringComparison.Ordinal)
                || string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTime?", StringComparison.Ordinal);

            var isDateTimeOffsetTarget =
                string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTimeOffset", StringComparison.Ordinal)
                || string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTimeOffset?", StringComparison.Ordinal);

            builder.Append("            static bool ")
                   .Append(setCase.LocalMethodName)
                   .Append("(")
                   .Append(provider.ItemTypeDisplay)
                   .AppendLine(" typedItem, global::System.Object? value)");
            builder.AppendLine("            {");

            var currentVariable = "typedItem";
            var segmentIndex = 0;
            foreach (var navigationSegment in setCase.NavigationSegments)
            {
                var nextVariable = "target" + segmentIndex;
                builder.Append("                if (")
                       .Append(currentVariable)
                       .Append('.')
                       .Append(navigationSegment.MemberName)
                       .Append(" is not ")
                       .Append(navigationSegment.MemberTypeDisplay)
                       .Append(' ')
                       .Append(nextVariable)
                       .AppendLine(")");
                builder.AppendLine("                {");
                builder.AppendLine("                    return false;");
                builder.AppendLine("                }");
                builder.AppendLine();
                currentVariable = nextVariable;
                segmentIndex++;
            }

            if (setCase.IsLeafNullableValueType)
            {
                builder.AppendLine("                if (value is null)");
                builder.AppendLine("                {");
                builder.Append("                    ")
                       .Append(currentVariable)
                       .Append('.')
                       .Append(setCase.LeafMemberName)
                       .AppendLine(" = null;");
                builder.AppendLine("                    return true;");
                builder.AppendLine("                }");
                builder.AppendLine();

                if (isDateTimeOffsetTarget)
                {
                    builder.Append("                var existingValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                if (value is global::System.DateTimeOffset dateInput)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    var baseValue = existingValue ?? dateInput;");
                    builder.AppendLine("                    var mergedDateTime = dateInput.Date + baseValue.TimeOfDay;");
                    builder.AppendLine("                    var mergedValue = new global::System.DateTimeOffset(mergedDateTime, baseValue.Offset);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                if (value is global::System.TimeSpan timeInput)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    if (timeInput < global::System.TimeSpan.Zero || timeInput >= global::System.TimeSpan.FromDays(1))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var baseValue = existingValue ?? global::System.DateTimeOffset.Now;");
                    builder.AppendLine("                    var mergedDateTime = baseValue.Date + timeInput;");
                    builder.AppendLine("                    var mergedValue = new global::System.DateTimeOffset(mergedDateTime, baseValue.Offset);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (isDateTimeTarget)
                {
                    builder.Append("                var existingValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                if (value is global::System.DateTimeOffset dateInput)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    var baseValue = existingValue ?? dateInput.DateTime;");
                    builder.AppendLine("                    var dateTicks = new global::System.DateTime(dateInput.Year, dateInput.Month, dateInput.Day).Ticks;");
                    builder.AppendLine("                    var mergedTicks = dateTicks + baseValue.TimeOfDay.Ticks;");
                    builder.AppendLine("                    if (mergedTicks < global::System.DateTime.MinValue.Ticks || mergedTicks > global::System.DateTime.MaxValue.Ticks)");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var mergedValue = new global::System.DateTime(mergedTicks, baseValue.Kind);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                if (value is global::System.TimeSpan timeInput)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    if (timeInput < global::System.TimeSpan.Zero || timeInput >= global::System.TimeSpan.FromDays(1))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var baseValue = existingValue ?? global::System.DateTime.Now;");
                    builder.AppendLine("                    var mergedTicks = baseValue.Date.Ticks + timeInput.Ticks;");
                    builder.AppendLine("                    if (mergedTicks < global::System.DateTime.MinValue.Ticks || mergedTicks > global::System.DateTime.MaxValue.Ticks)");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var mergedValue = new global::System.DateTime(mergedTicks, baseValue.Kind);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasNumericConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.NumericConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasDateConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.DateConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasTimeConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.TimeConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else
                {
                    builder.Append("                if (value is ")
                           .Append(setCase.LeafValueTypeDisplay)
                           .AppendLine(" typedValue)");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = typedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }

                builder.AppendLine("                return false;");
            }
            else
            {
                if (isDateTimeOffsetTarget)
                {
                    builder.Append("                if (value is global::System.DateTimeOffset dateInput)");
                    builder.AppendLine();
                    builder.AppendLine("                {");
                    builder.Append("                    var baseValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                    var mergedDateTime = dateInput.Date + baseValue.TimeOfDay;");
                    builder.AppendLine("                    var mergedValue = new global::System.DateTimeOffset(mergedDateTime, baseValue.Offset);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.Append("                if (value is global::System.TimeSpan timeInput)");
                    builder.AppendLine();
                    builder.AppendLine("                {");
                    builder.AppendLine("                    if (timeInput < global::System.TimeSpan.Zero || timeInput >= global::System.TimeSpan.FromDays(1))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.Append("                    var baseValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                    var mergedDateTime = baseValue.Date + timeInput;");
                    builder.AppendLine("                    var mergedValue = new global::System.DateTimeOffset(mergedDateTime, baseValue.Offset);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (isDateTimeTarget)
                {
                    builder.AppendLine("                if (value is global::System.DateTimeOffset dateInput)");
                    builder.AppendLine("                {");
                    builder.Append("                    var baseValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                    var dateTicks = new global::System.DateTime(dateInput.Year, dateInput.Month, dateInput.Day).Ticks;");
                    builder.AppendLine("                    var mergedTicks = dateTicks + baseValue.TimeOfDay.Ticks;");
                    builder.AppendLine("                    if (mergedTicks < global::System.DateTime.MinValue.Ticks || mergedTicks > global::System.DateTime.MaxValue.Ticks)");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var mergedValue = new global::System.DateTime(mergedTicks, baseValue.Kind);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                if (value is global::System.TimeSpan timeInput)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    if (timeInput < global::System.TimeSpan.Zero || timeInput >= global::System.TimeSpan.FromDays(1))");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.Append("                    var baseValue = ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(";");
                    builder.AppendLine("                    var mergedTicks = baseValue.Date.Ticks + timeInput.Ticks;");
                    builder.AppendLine("                    if (mergedTicks < global::System.DateTime.MinValue.Ticks || mergedTicks > global::System.DateTime.MaxValue.Ticks)");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                        return false;");
                    builder.AppendLine("                    }");
                    builder.AppendLine();
                    builder.AppendLine("                    var mergedValue = new global::System.DateTime(mergedTicks, baseValue.Kind);");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = mergedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasNumericConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.NumericConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasDateConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.DateConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else if (setCase.HasTimeConversion)
                {
                    builder.Append("                if (")
                           .Append(setCase.TimeConversionHelperMethodName)
                           .AppendLine("(value, out var convertedValue))");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = convertedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }
                else
                {
                    builder.Append("                if (value is ")
                           .Append(setCase.LeafValueTypeDisplay)
                           .AppendLine(" typedValue)");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = typedValue;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }

                if (setCase.CanAssignNull)
                {
                    builder.AppendLine("                if (value is null)");
                    builder.AppendLine("                {");
                    builder.Append("                    ")
                           .Append(currentVariable)
                           .Append('.')
                           .Append(setCase.LeafMemberName)
                           .AppendLine(" = null;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                }

                builder.AppendLine("                return false;");
            }

            builder.AppendLine("            }");
            builder.AppendLine();
        }

        var conversionHelpers = new Dictionary<string, GeneratedSetValueCase>(StringComparer.Ordinal);
        foreach (var setCase in provider.BindingSetCases)
        {
            var isDateTimeTarget =
                string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTime", StringComparison.Ordinal)
                || string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTime?", StringComparison.Ordinal);

            var isDateTimeOffsetTarget =
                string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTimeOffset", StringComparison.Ordinal)
                || string.Equals(setCase.LeafValueTypeDisplay, "global::System.DateTimeOffset?", StringComparison.Ordinal);

            if (setCase.HasNumericConversion
                && !conversionHelpers.ContainsKey(setCase.NumericConversionHelperMethodName))
            {
                conversionHelpers.Add(setCase.NumericConversionHelperMethodName, setCase);
            }

            if (!isDateTimeTarget
                && !isDateTimeOffsetTarget
                && setCase.HasDateConversion
                && !conversionHelpers.ContainsKey(setCase.DateConversionHelperMethodName))
            {
                conversionHelpers.Add(setCase.DateConversionHelperMethodName, setCase);
            }

            if (!isDateTimeTarget
                && !isDateTimeOffsetTarget
                && setCase.HasTimeConversion
                && !conversionHelpers.ContainsKey(setCase.TimeConversionHelperMethodName))
            {
                conversionHelpers.Add(setCase.TimeConversionHelperMethodName, setCase);
            }
        }

        foreach (var helper in conversionHelpers.Values)
        {
            if (helper.HasNumericConversion)
            {
                builder.Append("            static bool ")
                       .Append(helper.NumericConversionHelperMethodName)
                       .Append("(global::System.Object? input, out ")
                       .Append(helper.LeafValueTypeDisplay)
                       .AppendLine(" converted)");
                builder.AppendLine("            {");
                builder.AppendLine("                if (input is global::System.Double number)");
                builder.AppendLine("                {");
                if (helper.NumericConvertMethodName == "ToDouble")
                {
                    builder.AppendLine("                    converted = number;");
                    builder.AppendLine("                    return true;");
                }
                else
                {
                    builder.AppendLine("                    try");
                    builder.AppendLine("                    {");
                    builder.Append("                        converted = global::System.Convert.")
                           .Append(helper.NumericConvertMethodName)
                           .AppendLine("(number);");
                    builder.AppendLine("                        return true;");
                    builder.AppendLine("                    }");
                    builder.AppendLine("                    catch (global::System.Exception)");
                    builder.AppendLine("                    {");
                    builder.AppendLine("                    }");
                }
                builder.AppendLine("                }");
                builder.AppendLine();
                builder.AppendLine("                converted = default;");
                builder.AppendLine("                return false;");
                builder.AppendLine("            }");
                builder.AppendLine();
            }
            else if (helper.HasDateConversion)
            {
                if (helper.DateConversionHelperMethodName == "TryGetDateOnlyFromDateTimeOffset")
                {
                    builder.AppendLine("            static bool TryGetDateOnlyFromDateTimeOffset(global::System.Object? input, out global::System.DateOnly converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.DateTimeOffset dateTimeOffset)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = global::System.DateOnly.FromDateTime(dateTimeOffset.DateTime);");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
                else if (helper.DateConversionHelperMethodName == "TryGetDateTimeFromDateTimeOffset")
                {
                    builder.AppendLine("            static bool TryGetDateTimeFromDateTimeOffset(global::System.Object? input, out global::System.DateTime converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.DateTimeOffset dateTimeOffset)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = dateTimeOffset.DateTime;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
                else if (helper.DateConversionHelperMethodName == "TryGetDateTimeOffsetFromDateTimeOffset")
                {
                    builder.AppendLine("            static bool TryGetDateTimeOffsetFromDateTimeOffset(global::System.Object? input, out global::System.DateTimeOffset converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.DateTimeOffset dateTimeOffset)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = dateTimeOffset;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
            }
            else if (helper.HasTimeConversion)
            {
                if (helper.TimeConversionHelperMethodName == "TryGetTimeOnlyFromTimeSpan")
                {
                    builder.AppendLine("            static bool TryGetTimeOnlyFromTimeSpan(global::System.Object? input, out global::System.TimeOnly converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.TimeSpan timeSpan)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = global::System.TimeOnly.FromTimeSpan(timeSpan);");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
                else if (helper.TimeConversionHelperMethodName == "TryGetTimeSpanFromTimeSpan")
                {
                    builder.AppendLine("            static bool TryGetTimeSpanFromTimeSpan(global::System.Object? input, out global::System.TimeSpan converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.TimeSpan timeSpan)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = timeSpan;");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
                else if (helper.TimeConversionHelperMethodName == "TryGetDateTimeFromTimeSpan")
                {
                    builder.AppendLine("            static bool TryGetDateTimeFromTimeSpan(global::System.Object? input, out global::System.DateTime converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.TimeSpan timeSpan)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = global::System.DateTime.MinValue.Add(timeSpan);");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
                else if (helper.TimeConversionHelperMethodName == "TryGetDateTimeOffsetFromTimeSpan")
                {
                    builder.AppendLine("            static bool TryGetDateTimeOffsetFromTimeSpan(global::System.Object? input, out global::System.DateTimeOffset converted)");
                    builder.AppendLine("            {");
                    builder.AppendLine("                if (input is global::System.TimeSpan timeSpan)");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    converted = new global::System.DateTimeOffset(global::System.DateTime.MinValue.Add(timeSpan), global::System.TimeSpan.Zero);");
                    builder.AppendLine("                    return true;");
                    builder.AppendLine("                }");
                    builder.AppendLine();
                    builder.AppendLine("                converted = default;");
                    builder.AppendLine("                return false;");
                    builder.AppendLine("            }");
                    builder.AppendLine();
                }
            }
        }

        builder.AppendLine("        }");
        builder.AppendLine();
    }

    private static string EnsureGlobalQualified(string typeDisplay)
    {
        var trimmed = NormalizeSpecialTypeAlias(typeDisplay.Trim());
        return trimmed.StartsWith("global::", StringComparison.Ordinal)
            ? trimmed
            : "global::" + trimmed;
    }

    private static string GetGlobalTypeDisplay(ITypeSymbol type)
    {
        return EnsureGlobalQualified(type.ToDisplayString(FullyQualifiedNonAliasedTypeFormat));
    }

    private static string NormalizeSpecialTypeAlias(string typeDisplay)
    {
        return typeDisplay switch
        {
            "bool" => "System.Boolean",
            "byte" => "System.Byte",
            "sbyte" => "System.SByte",
            "short" => "System.Int16",
            "ushort" => "System.UInt16",
            "int" => "System.Int32",
            "uint" => "System.UInt32",
            "long" => "System.Int64",
            "ulong" => "System.UInt64",
            "char" => "System.Char",
            "float" => "System.Single",
            "double" => "System.Double",
            "decimal" => "System.Decimal",
            "string" => "System.String",
            "object" => "System.Object",
            "nint" => "System.IntPtr",
            "nuint" => "System.UIntPtr",
            "bool?" => "System.Boolean?",
            "byte?" => "System.Byte?",
            "sbyte?" => "System.SByte?",
            "short?" => "System.Int16?",
            "ushort?" => "System.UInt16?",
            "int?" => "System.Int32?",
            "uint?" => "System.UInt32?",
            "long?" => "System.Int64?",
            "ulong?" => "System.UInt64?",
            "char?" => "System.Char?",
            "float?" => "System.Single?",
            "double?" => "System.Double?",
            "decimal?" => "System.Decimal?",
            "nint?" => "System.IntPtr?",
            "nuint?" => "System.UIntPtr?",
            _ => typeDisplay
        };
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


}

