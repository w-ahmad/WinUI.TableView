; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Release%20Tracking.md

## Release 1.0.0
### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|------
TV0001 | WinUI.TableView.SourceGenerators | Error | TableView must define x:Name or Name to enable generated connector code.
TV0002 | WinUI.TableView.SourceGenerators | Error | Unable to resolve TableView item type from x:Bind ItemsSource.
TV0003 | WinUI.TableView.SourceGenerators | Error | Window roots with generated TableView connectors must call ConnectTableViews() manually after InitializeComponent().
TV0004 | WinUI.TableView.SourceGenerators | Error | TableView member path could not be resolved for the inferred item type.
