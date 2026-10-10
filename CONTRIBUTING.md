# ✨ Contributing to WinUI.TableView

Thank you for your interest in contributing to **WinUI.TableView**! We welcome all contributions and appreciate your help in making this project better for everyone.

---

## ❔ Questions

Please search existing [Discussions](../../discussions) first to see if your question has already been answered. If not, feel free to start a new discussion for general questions. Keep GitHub issues focused on actionable bug reports and enhancements.

---

## 🐛 Reporting Bugs

Before reporting a bug, please search existing [Issues](../../issues) to see if it has already been reported. If you find a similar issue, add any additional details as a comment. If not, please [open a new issue](../../issues/new?template=bug_report.md) and provide as much detail as possible to help us reproduce and fix the problem.

---

## 💡 Suggesting Features

Please search existing [Issues](../../issues) and [Discussions](../../discussions) to see if your feature has already been suggested. If you find a similar request, consider adding your thoughts to the existing conversation. If not, please [open a feature request](../../issues/new?template=feature_request.md) and describe your idea.

---

## 🚀 Pull Requests

Before creating a Pull Request, please start a Discussion or open an Issue to describe your planned changes. This helps the maintainers and community provide feedback early and ensures your contribution aligns with the project goals. You can skip this step for minor fixes like typos or small documentation updates.

- Do NOT open pull requests from your `main` branch. Always create a new feature ( for example `add-cell-tests` ) in your fork before submitting a PR.
- Ensure your changes are tested with both WinUI 3 and Uno Platform targets.
- Add or update unit tests and integration tests to cover your changes.
- Complete the PR checklist, and ensure your code is tested with both targets.
- Update documentation as needed to reflect your changes.

[How to create a pull request from a fork (GitHub Docs)](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork)

---

## 🧪 Running Tests

The test projects live in `tests/WinUI.TableView.Tests` and `tests/WinUI.TableView.Tests.Uno`, mirroring the sample project layout. The Uno test app links the same test source files as the WinUI test host. Run the suite on Uno Desktop with:

```powershell
dotnet run --project tests\WinUI.TableView.Tests.Uno\WinUI.TableView.Tests.Uno.csproj --configuration Debug --framework net10.0-desktop -- --exit-after-tests
```

To build the shared tests for another Uno target, run the following command with `net10.0-browserwasm`, `net10.0-android`, or `net10.0-ios` as the framework:

```powershell
dotnet build tests\WinUI.TableView.Tests.Uno\WinUI.TableView.Tests.Uno.csproj --configuration Debug --framework net10.0-browserwasm
```

Both `[TestMethod]` and `[UITestMethod]` are discoverable as MSTest methods on Uno. The Uno app runs both kinds of tests on its UI thread; use the app command above to execute the suite.

Tests for features not implemented by Uno are excluded with `#if WINDOWS`. The current Uno suite includes 336 tests versus 378 on WinUI (including the shared test-discovery regression test). The 42 Windows-only tests cover grouping, `SelectRange`, and row-realization-dependent resizing.

### CI execution

`ci-build.yml` first builds and packs the library for all its targets in an independent build job. WinUI, Uno Desktop, WebAssembly, Android, and iOS test jobs depend on that build and then run in parallel. NuGet publishing waits for the build and all five test jobs to pass. Each Uno job fails if the test host reports failures, executes no tests, or does not finish.

WebAssembly tests use Uno's browser DOM renderer and run in headless Chromium. The Skia browser renderer in the pinned Uno version throws during startup before a window is available; Desktop, Android, and iOS retain Skia. After building the Release WASM target, run:

```powershell
cd tests\WinUI.TableView.Tests.Uno
npm ci
npx playwright install chromium
npm run test:wasm
```

Android and iOS jobs build Debug test apps and execute them on a tablet emulator/simulator. With the matching app built and Android's `adb` or macOS's `xcrun` available, run `python tests/WinUI.TableView.Tests.Uno/ci/run_mobile_tests.py android` or `python3 tests/WinUI.TableView.Tests.Uno/ci/run_mobile_tests.py ios`. The iOS job pins the .NET 10.0.100 workload set and Xcode 26.0.1 and uses an iOS 26.0 iPad simulator.

CI passes `-p:UnoTestTargetFramework=<framework>` to limit restore and build to that Uno target without changing the library's target frameworks. Console logs and WASM JUnit results are uploaded as job artifacts.

---

## 🧪 Testing Dev Packages

When you open a pull request, our CI pipeline automatically builds and publishes a dev package to NuGet.org after a successful build. This allows you to test your changes before they are merged.

**Version Format:** `0.0.{buildnumber}-dev` (e.g., `0.0.1234-dev`)

---

## 📝 Code Style

- Follow the existing coding conventions and structure.
- Write clear, concise commit messages.
- Include comments where necessary.

---

## 💙 Thank You

Thank you for being a part of the WinUI.TableView community. Every contribution counts!
