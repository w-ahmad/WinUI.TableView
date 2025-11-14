# GitHub Copilot Instructions for WinUI.TableView

## Project Overview

**WinUI.TableView** is a lightweight and fast data grid control for WinUI 3 apps with support for the Uno Platform. It's derived from `ListView` and provides an Excel-like data table experience with features like column filtering, sorting, editing, and data export.

- **Repository**: https://github.com/w-ahmad/WinUI.TableView
- **Documentation**: https://w-ahmad.github.io/WinUI.TableView/
- **NuGet Package**: WinUI.TableView
- **License**: MIT

## Technology Stack

- **Primary Platform**: WinUI 3 (Windows App SDK)
- **Cross-Platform Support**: Uno Platform (WASM, Desktop)
- **Target Frameworks**: 
  - .NET 8.0 and .NET 9.0 (Uno Platform targets)
  - .NET 8.0-windows10.0.19041.0 and .NET 9.0-windows10.0.19041.0 (WinUI 3 targets)
- **Languages**: C# with XAML
- **Dependencies**:
  - Microsoft.WindowsAppSDK (for WinUI targets)
  - Uno.WinUI (for non-Windows targets)
  - CommunityToolkit.WinUI.Behaviors

## Repository Structure

```
/
├── src/                          # Main library source code
│   ├── Columns/                  # Column type implementations
│   ├── Controls/                 # Custom control implementations
│   ├── Converters/               # XAML value converters
│   ├── EventArgs/                # Custom event argument classes
│   ├── Extensions/               # Extension methods
│   ├── Helpers/                  # Helper classes and utilities
│   ├── ItemsSource/              # Data source handling
│   ├── Primitives/               # Base/primitive control classes
│   ├── Strings/                  # Localization resources
│   ├── Themes/                   # XAML styles and control templates
│   ├── TableView.cs              # Main TableView control
│   └── WinUI.TableView.csproj    # Project file
├── tests/                        # Unit tests
│   └── WinUI.TableView.Tests.csproj
├── docs/                         # Documentation (DocFX)
├── .github/                      # GitHub configuration
│   ├── workflows/                # CI/CD workflows
│   └── ISSUE_TEMPLATE/           # Issue templates
├── README.md                     # Project readme
├── CONTRIBUTING.md               # Contribution guidelines
└── WinUI.TableView.slnx          # Solution file
```

## Build and Test Instructions

### Building the Project

**Prerequisites**:
- Visual Studio 2022 or later
- Windows 10 SDK (10.0.19041.0 or later)
- .NET 8.0 and/or .NET 9.0 SDK

**Build Command**:
```bash
msbuild /restore /t:Build,Pack src/WinUI.TableView.csproj /p:Configuration=Release
```

**Build Outputs**:
- NuGet packages: `artifacts/NuGet/Release/`
- Documentation is built automatically on commits to docs/ folder

### Running Tests

**Build Tests**:
```bash
msbuild /restore /t:Build tests/WinUI.TableView.Tests.csproj /p:Platform=x64 /p:Configuration=Release /p:OutputPath=build
```

**Run Tests**:
```bash
vstest.console.exe tests\build\WinUI.TableView.Tests.build.appxrecipe --logger:"console;verbosity=normal" /InIsolation
```

**Note**: Tests are WinUI 3 app tests and require Windows with visual UI. They run on x64 platform only.

## Coding Standards and Conventions

### Code Style

This project uses `.editorconfig` for consistent code formatting. Key conventions:

- **Indentation**: 4 spaces
- **Line Endings**: CRLF (Windows style)
- **Namespace Style**: File-scoped namespaces preferred
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Private Fields**: Use underscore prefix (e.g., `_fieldName`)
- **Interfaces**: Prefix with `I` (e.g., `ITableViewColumn`)
- **Naming**: PascalCase for public members, camelCase with underscore for private fields
- **Using Directives**: Place outside namespace, no separation of System directives
- **File Header**: Not required

### XML Documentation

- **All public types and members must have XML documentation comments** (enforced as error via CS1591)
- Use `<summary>`, `<param>`, `<returns>`, and other standard XML doc tags
- Generate documentation file is enabled for the library project

### Platform-Specific Code

The project supports both WinUI 3 and Uno Platform. When writing platform-specific code:

- Use conditional compilation based on target framework
- WinUI-specific code: Use `#if WINDOWS` or check `$([MSBuild]::GetTargetPlatformIdentifier($(TargetFramework))) == 'windows'`
- Uno Platform specific: Check for non-Windows targets
- Test changes on both WinUI 3 and Uno Platform targets when possible

### XAML Conventions

- Use WinUI 3 XAML syntax
- Control templates and styles are in `src/Themes/` folder
- Generic.xaml serves as the main resource dictionary
- Use resource keys for reusable styles and templates

## Key Features and Components

### Core Components

1. **TableView**: Main control derived from ListView
2. **TableViewColumn Types**:
   - TableViewTextColumn
   - TableViewCheckBoxColumn
   - TableViewComboBoxColumn
   - TableViewNumberColumn
   - TableViewToggleSwitchColumn
   - TableViewTemplateColumn
   - TableViewTimeColumn
   - TableViewDateColumn

3. **Supporting Classes**:
   - TableViewCell: Individual cell control
   - TableViewRow: Row container
   - TableViewColumnHeader: Column header with filtering
   - TableViewHeaderRow: Header row container
   - TableViewColumnsCollection: Column collection management

### Important Features

- Auto-generating columns from data source
- Individual cell selection and editing
- Excel-like column filtering
- Built-in sorting
- Copy to clipboard functionality
- CSV export
- Grid lines customization
- Row details support
- Localization support
- Alternate row colors

## Common Development Tasks

### Adding a New Column Type

1. Create a new class in `src/Columns/` inheriting from `TableViewBoundColumn` or `TableViewColumn`
2. Implement required abstract members
3. Add XAML style if needed in `src/Themes/`
4. Update documentation
5. Add unit tests if applicable

### Adding New Features

1. Check existing issues and discussions first
2. Start a discussion or open an issue to describe the feature
3. Implement the feature with minimal changes
4. Ensure compatibility with both WinUI 3 and Uno Platform
5. Add XML documentation
6. Update README.md and docs/ if needed
7. Add unit tests where feasible
8. Test on both platforms

### Modifying Existing Code

- Make surgical, minimal changes
- Preserve existing behavior unless fixing a bug
- Update XML documentation if changing public API
- Run tests before submitting PR
- Follow the existing code style

## Testing Guidelines

- Unit tests are located in `tests/` folder
- Tests use MSTest framework
- Focus on testing extension methods and collection operations
- UI testing is limited due to WinUI 3 constraints
- Test coverage includes:
  - Extension methods (CollectionExtensions, DateTimeExtensions, etc.)
  - Type utilities
  - Collection operations

## Pull Request Guidelines

From CONTRIBUTING.md:

- **Do NOT** open pull requests from your `main` branch
- Always create a new feature branch (e.g., `add-feature-name`)
- Test changes with both WinUI 3 and Uno Platform targets
- Add or update unit tests to cover changes
- Complete the PR checklist
- Update documentation as needed
- Ensure CI build passes

## Localization

- Localized strings are in `src/Strings/` folder as `.resw` files
- Use `TableViewLocalizedStrings` class to access localized strings
- Support for multiple languages

## Documentation

- Project uses DocFX for documentation generation
- Documentation source is in `docs/` folder
- Auto-generated API reference
- CI workflow builds and publishes docs automatically
- Documentation site: https://w-ahmad.github.io/WinUI.TableView/

## CI/CD Workflows

- **ci-build.yml**: Runs on PRs and main branch commits (non-docs)
  - Builds the library
  - Runs unit tests
  - Creates NuGet package artifacts
  
- **cd-build.yml**: Continuous deployment for releases
  - Publishes to NuGet on version tags

- **ci-docs.yml**: Documentation build
  - Triggered by changes to docs/ folder
  - Publishes to GitHub Pages

## Dependencies and Package Management

- Use NuGet for package management
- Pin specific versions in .csproj for stability
- CommunityToolkit.WinUI.Behaviors version differs between .NET 8 and 9
- Microsoft.WindowsAppSDK and Uno.WinUI versions are carefully selected for compatibility

## Important Notes for Contributors

1. **Nullable Reference Types**: The project uses nullable reference types. Be mindful of null checks and use appropriate nullable annotations.

2. **Internal Visibility**: Tests have access to internal members via `InternalsVisibleTo` attribute.

3. **Performance**: This is a performance-focused control designed to handle large datasets efficiently.

4. **Breaking Changes**: Avoid breaking changes to public API. Discuss in issues first.

5. **Samples**: A separate repository (WinUI.TableView.SampleApp) contains interactive samples and demos.

## Getting Help

- **Issues**: Report bugs via GitHub Issues with bug_report.md template
- **Discussions**: Ask questions in GitHub Discussions
- **Feature Requests**: Use feature_request.md template in Issues

## License

This project is licensed under the MIT License. All contributions will be under the same license.
