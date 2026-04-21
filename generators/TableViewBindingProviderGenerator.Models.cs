using System.Collections.Immutable;

namespace WinUI.TableView.SourceGenerators;

public sealed partial class TableViewBindingProviderGenerator
{
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
            BindingEditorKind editorKind,
            BindingMode bindingMode,
            PathSpanRaw? sortMemberPathSpan,
            PathSpanRaw? displayMemberPathSpan,
            PathSpanRaw? bindingPathSpan,
            PathSpanRaw? clipboardBindingPathSpan,
            PathSpanRaw? contentBindingPathSpan)
        {
            StartOffset = startOffset;
            StartLine = startLine;
            EditorKind = editorKind;
            BindingMode = bindingMode;
            SortMemberPathSpan = sortMemberPathSpan;
            DisplayMemberPathSpan = displayMemberPathSpan;
            BindingPathSpan = bindingPathSpan;
            ClipboardPathSpan = clipboardBindingPathSpan;
            ContentPathSpan = contentBindingPathSpan;
        }

        public int StartOffset { get; }

        public int StartLine { get; }

        public BindingEditorKind EditorKind { get; }

        public BindingMode BindingMode { get; }

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
            BindingEditorKind editorKind,
            BindingMode bindingMode,
            PathDiagnosticLocation? sortMemberPathLocation,
            PathDiagnosticLocation? displayMemberPathLocation,
            PathDiagnosticLocation? cellBindingPathLocation,
            PathDiagnosticLocation? clipboardBindingPathLocation,
            PathDiagnosticLocation? contentBindingPathLocation)
        {
            StartLine = startLine;
            EditorKind = editorKind;
            BindingMode = bindingMode;
            SortMemberPathLocation = sortMemberPathLocation;
            DisplayMemberPathLocation = displayMemberPathLocation;
            BindingPathLocation = cellBindingPathLocation;
            ClipboardBindingPathLocation = clipboardBindingPathLocation;
            ContentBindingPathLocation = contentBindingPathLocation;
        }

        public int StartLine { get; }

        public BindingEditorKind EditorKind { get; }

        public BindingMode BindingMode { get; }

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
            ImmutableArray<GeneratedSetValueCase> bindingSetCases,
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
            BindingSetCases = bindingSetCases;
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
        /// Generated set cases for two-way binding paths.
        /// </summary>
        public ImmutableArray<GeneratedSetValueCase> BindingSetCases { get; }

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

    private readonly struct GeneratedSetValueCase
    {
        public GeneratedSetValueCase(
            string memberPath,
            string localMethodName,
            CaseSourceInfo sourceInfo,
            ImmutableArray<SetNavigationSegment> navigationSegments,
            string leafMemberName,
            string leafValueTypeDisplay,
            bool canAssignNull,
            bool isLeafNullableValueType,
            bool hasNumericConversion,
            string numericConversionHelperMethodName,
            string numericConvertMethodName,
            bool hasDateConversion,
            string dateConversionHelperMethodName,
            bool hasTimeConversion,
            string timeConversionHelperMethodName)
        {
            MemberPath = memberPath;
            LocalMethodName = localMethodName;
            SourceInfo = sourceInfo;
            NavigationSegments = navigationSegments;
            LeafMemberName = leafMemberName;
            LeafValueTypeDisplay = leafValueTypeDisplay;
            CanAssignNull = canAssignNull;
            IsLeafNullableValueType = isLeafNullableValueType;
            HasNumericConversion = hasNumericConversion;
            NumericConversionHelperMethodName = numericConversionHelperMethodName;
            NumericConvertMethodName = numericConvertMethodName;
            HasDateConversion = hasDateConversion;
            DateConversionHelperMethodName = dateConversionHelperMethodName;
            HasTimeConversion = hasTimeConversion;
            TimeConversionHelperMethodName = timeConversionHelperMethodName;
        }

        public string MemberPath { get; }

        public string LocalMethodName { get; }

        public CaseSourceInfo SourceInfo { get; }

        public ImmutableArray<SetNavigationSegment> NavigationSegments { get; }

        public string LeafMemberName { get; }

        public string LeafValueTypeDisplay { get; }

        public bool CanAssignNull { get; }

        public bool IsLeafNullableValueType { get; }

        public bool HasNumericConversion { get; }

        public string NumericConversionHelperMethodName { get; }

        public string NumericConvertMethodName { get; }

        public bool HasDateConversion { get; }

        public string DateConversionHelperMethodName { get; }

        public bool HasTimeConversion { get; }

        public string TimeConversionHelperMethodName { get; }
    }

    private readonly struct SetNavigationSegment
    {
        public SetNavigationSegment(string memberName, string memberTypeDisplay)
        {
            MemberName = memberName;
            MemberTypeDisplay = memberTypeDisplay;
        }

        public string MemberName { get; }

        public string MemberTypeDisplay { get; }
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

    private enum BindingEditorKind
    {
        Unknown,
        Other,
        Date,
        Time
    }

    private enum BindingMode
    {
        OneWay,
        OneTime,
        TwoWay,
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

