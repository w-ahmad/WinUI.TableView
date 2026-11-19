// Records and merges the WASM profile-guided-AOT profiles for the Uno sample.
//
//   node tools/record-aot-profile.cjs                    publish in profile-gen mode, record 3 scenarios, merge, verify
//   node tools/record-aot-profile.cjs --no-build         reuse the last profile-gen publish as it is
//   node tools/record-aot-profile.cjs --merge-only       merge the recordings already on disk, no browser
//   node tools/record-aot-profile.cjs --only controls    re-record a single scenario (scrolling | controls | grouping)
//   node tools/record-aot-profile.cjs --out <file>       write the merged profile somewhere else
//   node tools/record-aot-profile.cjs --setup            install missing browser dependencies without asking
//
// On a fresh checkout the browser test dependencies are not installed yet; the script checks for them
// and offers to run the installs (npm ci for the Uno test project, then Playwright's Chromium) itself.
//
// Re-record after every Uno upgrade: a stale profile leaves hot paths interpreted and scrolling janky.
// The app only offers Uno's Alt+Shift+P profile download when built with WasmShellGenerateAOTProfile=true,
// which turns AOT and the IL linker off. That app is published with its own obj/bin under
// artifacts/aot-record, never through the checkout's: the WASM build caches processed assemblies there,
// and a profiling app that picks up a normal publish's stripped assemblies fails to start.
// The three recordings cover what the table JITs most - scrolling every sample page, operating the
// editing/sorting/filter controls, and grouping - and are merged with Mono.Profiler.Aot from the
// Uno.Wasm.Bootstrap tools package, deduplicated by method identity. Every recording must contain
// TableView methods (a recording of an app that never started holds only startup code), and the merged
// file is read back to check no method was lost; only then does it replace the output file. Afterwards
// run the normal publish (tools/publish-and-serve-wasm.ps1) so the new profile is applied.
//
// Pages are found by name: the recording switches on Uno's accessibility layer, which mirrors the
// navigation menu into the DOM with live positions, so the menu's order and expanded groups don't
// matter, and a page this branch's sample doesn't have is skipped. The window is 1280 wide and starts
// 900 high; it grows until the whole menu fits, because the positions are only right while the menu is
// unscrolled. Clicks inside a page are still coordinates from the page's top-left corner - check the
// screenshots in the work folder if a page's layout changes. The accessibility layer's own code is
// recorded into the profile as well.
const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const http = require("node:http");
const { spawnSync } = require("node:child_process");

const repoRoot = path.resolve(__dirname, "..");
const args = process.argv.slice(2);
const option = name => (index => index >= 0 ? args[index + 1] : undefined)(args.indexOf(name));
const project = path.join(repoRoot, "samples", "WinUI.TableView.SampleApp.Uno", "WinUI.TableView.SampleApp.Uno.csproj");
const netVersion = fs.readFileSync(path.join(repoRoot, "Directory.Build.props"), "utf8").match(/<NetVersion>(.+?)<\/NetVersion>/)?.[1] ?? "net10.0";
const tfm = `${netVersion}-browserwasm`;
const workDir = path.join(repoRoot, "artifacts", "aot-record");
const buildDir = path.join(workDir, "build");
const appDir = path.join(workDir, "app");
const wwwroot = path.join(appDir, "wwwroot");
const recordingsDir = path.join(workDir, "recordings");
const mergedOut = path.resolve(repoRoot, option("--out")
    ?? path.join("samples", "WinUI.TableView.SampleApp.Uno", "Platforms", "WebAssembly", "aot.profile"));
const only = option("--only");
// A recording of the sample that really ran holds thousands of TableView methods; one of an app that
// failed to start holds none.
const minimumTableViewMethods = 200;
const warnings = [];

function run(command, commandArgs, failure) {
    const result = spawnSync(command, commandArgs, { cwd: repoRoot, stdio: ["ignore", "pipe", "inherit"], shell: false, encoding: "utf8" });
    if (result.status !== 0) {
        throw new Error(`${failure} (exit code ${result.status})`);
    }
    return result.stdout;
}

function publishProfileGenApp() {
    console.log(`Publishing the sample (${tfm}) in AOT-profile-generation mode (interpreted, linker off) ...`);
    fs.rmSync(appDir, { recursive: true, force: true });
    // The profiling runtime only interprets, so nothing is AOT-compiled, and IL is kept: with stripping on,
    // a publish leaves framework methods without bodies and the runtime fails to start. The build gets its
    // own obj/bin under the work folder because the WASM publish caches processed assemblies and reuses
    // them when only properties change: sharing the checkout's obj would serve a normal publish's stripped
    // assemblies to the profiling app, and the profiling app's to the next normal publish.
    const result = spawnSync("dotnet", ["publish", project, "-c", "Release", "-f", tfm, "-o", appDir, "-v", "m",
        "--artifacts-path", buildDir,
        "-p:WasmShellGenerateAOTProfile=true", "-p:WasmShellILLinkerEnabled=false",
        "-p:UnoXamlResourcesTrimming=false", "-p:WasmShellEnableJiterpreter=false",
        "-p:WasmShellMonoRuntimeExecutionMode=Interpreter", "-p:WasmStripILAfterAOT=false"],
        { cwd: repoRoot, stdio: "inherit", shell: false });
    if (result.status !== 0) {
        throw new Error(`dotnet publish failed with exit code ${result.status}.`);
    }
}

function serve(root) {
    const server = http.createServer((request, response) => {
        let target = path.resolve(root, "." + decodeURIComponent(new URL(request.url, "http://localhost").pathname));
        if (target !== root && !target.startsWith(root + path.sep)) {
            response.writeHead(403).end();
            return;
        }
        if (fs.existsSync(target) && fs.statSync(target).isDirectory()) target = path.join(target, "index.html");
        fs.readFile(target, (error, content) => {
            if (error) { response.writeHead(404).end(); return; }
            const types = { ".html": "text/html", ".js": "text/javascript", ".mjs": "text/javascript", ".wasm": "application/wasm", ".json": "application/json", ".css": "text/css" };
            response.setHeader("Content-Type", types[path.extname(target).toLowerCase()] || "application/octet-stream");
            response.setHeader("Cache-Control", "no-store");
            response.end(content);
        });
    });
    return new Promise(resolve => server.listen(0, "127.0.0.1", () => resolve(server)));
}

// Waits for the Uno window, then switches on the accessibility layer and waits for the menu to show up in it.
async function startApp(page, url) {
    const deadline = Date.now() + 300000; // the profiling build only interprets, so it starts slowly
    await page.goto(url);
    try {
        await page.waitForSelector("#uno-semantics-root", { state: "attached", timeout: 300000 });
    } catch {
        throw new Error("The app did not start within 5 minutes (no Uno window) - see the console log and startup screenshot.");
    }
    // The bootstrapper's loading screen goes once the app has rendered; go on anyway if it lingers.
    await page.waitForFunction(() => !document.querySelector(".uno-persistent-loader"), null, { timeout: 120000 }).catch(() => { });
    // The enable-accessibility toggle removes itself before asking .NET to switch the layer on, and on
    // Uno 6 that request is lost if the app's root isn't up yet. Repeat it through the same entry point
    // until the semantic tree starts to fill.
    for (; ;) {
        await page.evaluate(() => {
            const toggle = document.getElementById("uno-enable-accessibility");
            if (toggle) {
                toggle.click();
                return;
            }
            const runtime = globalThis.Uno?.UI?.Runtime;
            (runtime?.Accessibility ?? runtime?.Skia?.Accessibility)?.managedEnableAccessibility?.();
        });
        const filling = await page.waitForFunction(() => document.getElementById("uno-semantics-root")?.childElementCount > 0,
            null, { timeout: 30000 }).then(() => true, () => false);
        if (filling) break;
        if (Date.now() > deadline) {
            throw new Error("The app started but its accessibility layer could not be switched on - see the console log and startup screenshot.");
        }
    }
    try {
        await page.waitForFunction(() => [...document.querySelectorAll("#uno-semantics-root [aria-label]")]
            .some(element => element.getAttribute("aria-label") === "Overview"), null, { timeout: 180000 });
    } catch {
        throw new Error("The app started but its navigation menu never appeared - see the console log and startup screenshot.");
    }
    await page.waitForTimeout(3000); // let the Overview table finish loading its data
}

// The live center of a menu item, or null when the menu has no item of that name.
async function findMenuItem(page, name) {
    return page.evaluate(label => {
        for (const element of document.querySelectorAll("#uno-semantics-root [aria-label]")) {
            if (element.getAttribute("aria-label") !== label || element.getAttribute("role") === "label") continue;
            const rect = element.getBoundingClientRect();
            if (rect.width > 100 && rect.x < 320) return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 };
        }
        return null;
    }, name);
}

// Makes the window tall enough to show the whole menu. The menu must never scroll: once it has, Uno 7's
// accessibility layer reports its items away from where they are drawn, and a click opens another page.
async function fitMenu(page) {
    for (let attempt = 0; attempt < 5; attempt++) {
        const overflow = await page.evaluate(() => {
            let last = 0, footer = Infinity;
            for (const element of document.querySelectorAll("#uno-semantics-root [aria-label]")) {
                if (element.getAttribute("role") === "label") continue;
                const rect = element.getBoundingClientRect();
                if (rect.width <= 100 || rect.height > 60 || rect.x >= 320) continue; // menu items only
                if (element.getAttribute("aria-label") === "Settings") footer = rect.top;
                else last = Math.max(last, rect.bottom);
            }
            return last - (footer - 12);
        });
        if (!(overflow > 0)) return;
        const size = page.viewportSize();
        await page.setViewportSize({ width: size.width, height: Math.ceil(size.height + overflow + 40) });
        await page.waitForTimeout(1500);
    }
}

const titleClip = { x: 340, y: 15, width: 600, height: 50 };

function createNavigator(page) {
    let current = "Overview";
    // Opens a page from the menu; false when this sample has no such page.
    return async function navigate(name) {
        if (name === current) return true;
        await page.keyboard.press("Escape"); // close any flyout an earlier step left open
        for (let attempt = 0; attempt < 3; attempt++) {
            await fitMenu(page); // opening a page can expand its group, which lengthens the menu
            const item = await findMenuItem(page, name);
            if (!item) {
                console.log(`  (no '${name}' page in this sample - skipped)`);
                return false;
            }
            const before = await page.screenshot({ clip: titleClip });
            await page.mouse.click(item.x, item.y);
            await page.waitForTimeout(2000);
            if (!before.equals(await page.screenshot({ clip: titleClip }))) {
                current = name;
                return true;
            }
        }
        warnings.push(`could not open the '${name}' page; its steps ran on whatever page was showing`);
        return false;
    };
}

async function saveProfile(page, file) {
    const downloadPromise = page.waitForEvent("download");
    await page.keyboard.press("Alt+Shift+KeyP"); // Uno.Wasm.Bootstrap's profile-save hotkey
    await (await downloadPromise).saveAs(file);
    console.log(`  saved ${path.relative(repoRoot, file)} (${fs.statSync(file).size} bytes)`);
}

async function wheel(page, steps, delta = 80) {
    await page.mouse.move(600, 450);
    for (let step = 0; step < steps; step++) {
        await page.mouse.wheel(0, delta);
        await new Promise(resolve => setTimeout(resolve, 16));
    }
}

// Scrolls every common page's templates, jumps deep into the 10,000-row table, groups and expands a group,
// and toggles the theme, so row create/recycle paths dominate the recording.
async function recordScrolling(page, navigate) {
    await wheel(page, 20); // Overview, the landing page
    for (const name of ["Grid Lines", "Grouping", "Virtualization", "Selection", "Corner Button", "Alternate Row Color",
        "Context Flyouts", "Row Reorder", "Column Reorder", "Pagination", "Incremental Loading", "Editing", "Clipboard Actions", "Data Export",
        "Conditional Cell Styling", "Column Sizing", "Star Columns", "Headers Visibility"]) {
        if (await navigate(name)) await wheel(page, 20);
    }
    if (await navigate("Virtualization")) { // jump to row 5000 through the page's own controls
        await page.mouse.click(920, 283);
        await page.mouse.click(1020, 283);
        await page.mouse.click(940, 359);
        await page.keyboard.press("Control+A");
        await page.keyboard.type("5000");
        await page.keyboard.press("Enter");
        await page.mouse.click(1068, 359);
        await page.waitForTimeout(1000);
    }
    if (await navigate("Grouping")) {
        await page.mouse.click(1000, 500); // "Group by last name"
        await page.waitForTimeout(1000);
        await page.mouse.click(358, 238); // expand or collapse the first group
        await page.waitForTimeout(500);
    }
    await page.mouse.click(1120, 38); // "Toggle Theme", there and back
    await page.waitForTimeout(500);
    await page.mouse.click(1120, 38);
}

// Edits a cell, sorts, opens the filter flyout, and scrolls the row-header, row-details and compact pages.
async function recordControls(page, navigate) {
    if (await navigate("Editing")) {
        await page.mouse.click(1105, 177);
        await page.mouse.dblclick(460, 165);
        await page.keyboard.press("Control+A");
        await page.keyboard.type("ProfileName");
        await page.keyboard.press("Tab");
        await page.waitForTimeout(500);
    }
    if (await navigate("Sorting")) {
        await page.mouse.click(500, 128); // sort by the first column header
        await page.waitForTimeout(2000);
    }
    if (await navigate("Filtering")) {
        await page.mouse.click(506, 128); // open the first column's filter flyout
        await page.waitForTimeout(1000);
        await page.keyboard.press("Escape");
    }
    for (const name of ["Row Header Template", "Row Details", "Compact Sizing"]) {
        if (!await navigate(name)) continue;
        if (name === "Row Details") await page.mouse.click(350, 185); // expand a details row
        await wheel(page, 10);
    }
}

// Groups the 100-row sample, expands a group, and scrolls the grouped table.
async function recordGrouping(page, navigate) {
    if (!await navigate("Grouping")) return;
    await page.waitForTimeout(3000);
    await page.mouse.click(1000, 500);
    await page.waitForTimeout(5000);
    await page.mouse.click(358, 238);
    await page.waitForTimeout(2000);
    await wheel(page, 10);
}

const playwrightDir = path.join(repoRoot, "tests", "WinUI.TableView.Tests.Uno", "node_modules", "playwright");

// Asks before running a setup command; --setup runs it without asking, and a declined or
// promptless run fails with the command to run by hand.
async function offerSetup(problem, command) {
    if (args.includes("--setup")) {
        console.log(`${problem} Running: ${command}`);
        return;
    }
    if (!process.stdin.isTTY) {
        throw new Error(`${problem} Run this first (or pass --setup to let the script run it):\n  ${command}`);
    }
    const readline = require("node:readline");
    const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
    const answer = await new Promise(resolve => rl.question(`${problem}\n  ${command}\nRun it now? [Y/n] `, resolve));
    rl.close();
    if (/^n/i.test(answer.trim())) {
        throw new Error(`Setup declined - run it yourself, then try again:\n  ${command}`);
    }
}

// Recording drives a real browser, so the Uno test project's npm packages and Playwright's
// Chromium build must be installed in this checkout; offer to set up whatever is missing.
async function ensureBrowserDependencies() {
    if (!fs.existsSync(playwrightDir)) {
        await offerSetup("Playwright is not installed in this checkout.", "npm ci --prefix tests/WinUI.TableView.Tests.Uno");
        const npm = spawnSync("npm ci --prefix tests/WinUI.TableView.Tests.Uno", { cwd: repoRoot, stdio: "inherit", shell: true });
        if (npm.status !== 0) throw new Error(`npm ci failed with exit code ${npm.status}.`);
    }
    const { chromium } = require(playwrightDir);
    let executable;
    try { executable = chromium.executablePath(); } catch { executable = undefined; }
    if (!executable || !fs.existsSync(executable)) {
        await offerSetup("Playwright's Chromium build is not installed.",
            "node tests/WinUI.TableView.Tests.Uno/node_modules/playwright/cli.js install chromium");
        const install = spawnSync(process.execPath, [path.join(playwrightDir, "cli.js"), "install", "chromium"],
            { cwd: repoRoot, stdio: "inherit", shell: false });
        if (install.status !== 0) throw new Error(`Chromium install failed with exit code ${install.status}.`);
    }
    return chromium;
}

async function record(chromium) {
    if (!fs.existsSync(path.join(wwwroot, "index.html"))) {
        throw new Error(`No index.html under ${wwwroot} - run without --no-build first.`);
    }
    const scenarios = { scrolling: recordScrolling, controls: recordControls, grouping: recordGrouping };
    if (only && !scenarios[only]) throw new Error(`--only takes one of: ${Object.keys(scenarios).join(", ")}`);
    if (!only) fs.rmSync(recordingsDir, { recursive: true, force: true }); // never merge with another build's recordings
    fs.mkdirSync(recordingsDir, { recursive: true });
    const server = await serve(wwwroot);
    let browser;
    try {
        browser = await chromium.launch({
            headless: true, channel: "chromium",
            args: process.platform === "win32" ? ["--use-angle=d3d11", "--enable-gpu"] : ["--enable-gpu"]
        });
        for (const [name, scenario] of Object.entries(scenarios)) {
            if (only && name !== only) continue;
            console.log(`Recording ${name} ...`);
            const page = await browser.newPage({ viewport: { width: 1280, height: 900 }, serviceWorkers: "block", acceptDownloads: true });
            page.setDefaultTimeout(300000); // the interpreted app can keep a screenshot waiting past the 30 s default
            const logs = [];
            page.on("console", message => logs.push(message.text()));
            page.on("pageerror", error => logs.push(String(error.stack || error)));
            try {
                await startApp(page, `http://127.0.0.1:${server.address().port}`);
                await page.screenshot({ path: path.join(recordingsDir, `${name}-startup.png`) });
                await scenario(page, createNavigator(page));
                await page.screenshot({ path: path.join(recordingsDir, `${name}-end.png`) });
                await saveProfile(page, path.join(recordingsDir, `${name}.profile`));
            } catch (error) {
                await page.screenshot({ path: path.join(recordingsDir, `${name}-startup.png`) }).catch(() => { });
                throw new Error(`Recording '${name}' failed: ${error.message}\n  (logs: ${path.relative(repoRoot, recordingsDir)})`);
            } finally {
                fs.writeFileSync(path.join(recordingsDir, `${name}.console.txt`), logs.join("\n"));
                await page.close();
            }
        }
    } finally {
        await browser?.close();
        server.close();
    }
}

// Merging runs in PowerShell: the profile format's reader and writer ship in the Uno.Wasm.Bootstrap
// tools assembly (Mono.Profiler.Aot), which needs the SDK's MSBuild assemblies loadable beside it.
const mergeScript = [
    "param([string]$Output, [string]$InputsJoined, [string]$BootstrapDll, [string]$SdkDir, [int]$MinimumTableViewMethods)",
    "$ErrorActionPreference = 'Stop'",
    "[void][System.Reflection.Assembly]::LoadFrom((Join-Path $SdkDir 'Microsoft.Build.Framework.dll'))",
    "[void][System.Reflection.Assembly]::LoadFrom((Join-Path $SdkDir 'Microsoft.Build.Utilities.Core.dll'))",
    "$assembly = [System.Reflection.Assembly]::LoadFrom($BootstrapDll)",
    "function Read-Profile([string]$ProfilePath) {",
    "    $stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $ProfilePath))",
    "    try {",
    "        $reader = [Activator]::CreateInstance($assembly.GetType('Mono.Profiler.Aot.ProfileReader'))",
    "        return $reader.ReadAllData($stream)",
    "    } finally { $stream.Dispose() }",
    "}",
    "function Get-MethodKey($method) {",
    "    return $method.Type.Module.Name + ':' + $method.Type.FullName + ':' + $method.Type.GenericInst + ':' + $method.Name + ':' + $method.Signature + ':' + $method.ParamCount + ':' + $method.GenericInst",
    "}",
    "function Get-TableViewCount($methods) {",
    "    return @($methods | Where-Object { $_.Type.Module.Name -match '^WinUI\\.TableView(\\.dll)?$' }).Count",
    "}",
    "$seen = [System.Collections.Generic.HashSet[string]]::new()",
    "$methods = [System.Collections.Generic.List[object]]::new()",
    "$modules = [System.Collections.Generic.List[object]]::new()",
    "$types = [System.Collections.Generic.List[object]]::new()",
    "$empty = @()",
    "foreach ($file in $InputsJoined.Split(';')) {",
    "    $data = Read-Profile $file",
    "    $tableView = Get-TableViewCount $data.Methods",
    "    Write-Host ((Split-Path $file -Leaf) + ': ' + $data.Methods.Count + ' methods, ' + $tableView + ' from WinUI.TableView')",
    "    if ($tableView -lt $MinimumTableViewMethods) { $empty += (Split-Path $file -Leaf) }",
    "    foreach ($method in $data.Methods) {",
    "        if ($seen.Add((Get-MethodKey $method))) { $methods.Add($method) }",
    "    }",
    "    $modules.AddRange(@($data.Modules))",
    "    $types.AddRange(@($data.Types))",
    "}",
    "if ($empty.Count -gt 0) {",
    "    Write-Host ('Not merging: ' + ($empty -join ', ') + ' holds almost no WinUI.TableView methods, so the app did not really run while it was recorded.')",
    "    exit 2",
    "}",
    "$merged = [Activator]::CreateInstance($assembly.GetType('Mono.Profiler.Aot.ProfileData'))",
    "$merged.Modules = $modules.ToArray()",
    "$merged.Types = $types.ToArray()",
    "$merged.Methods = $methods.ToArray()",
    "$stream = [System.IO.File]::Create($Output)",
    "try {",
    "    $writer = [Activator]::CreateInstance($assembly.GetType('Mono.Profiler.Aot.ProfileWriter'))",
    "    $writer.WriteAllData($stream, $merged)",
    "} finally { $stream.Dispose() }",
    "# Read the file back and require every recorded method to have survived the merge.",
    "$written = Read-Profile $Output",
    "$writtenKeys = [System.Collections.Generic.HashSet[string]]::new()",
    "foreach ($method in $written.Methods) { [void]$writtenKeys.Add((Get-MethodKey $method)) }",
    "$missing = 0",
    "foreach ($key in $seen) { if (!$writtenKeys.Contains($key)) { $missing++ } }",
    "Write-Host ('merged: ' + $written.Methods.Count + ' methods, ' + (Get-TableViewCount $written.Methods) + ' from WinUI.TableView, ' + $missing + ' lost, ' + (Get-Item $Output).Length + ' bytes')",
    "if ($missing -gt 0) { exit 1 }",
].join("\n");

function findBootstrapDll() {
    const packagesRoot = run("dotnet", ["nuget", "locals", "global-packages", "--list"], "Could not query the NuGet cache")
        .match(/global-packages:\s*(.+)/)[1].trim();
    const assetsFile = [path.join(buildDir, "obj", path.basename(project, ".csproj"), "project.assets.json"),
        path.join(path.dirname(project), "obj", "project.assets.json")].find(file => fs.existsSync(file));
    let version = assetsFile
        ? fs.readFileSync(assetsFile, "utf8").match(/"Uno\.Wasm\.Bootstrap\/([^"]+)"/)?.[1]
        : undefined;
    version ??= fs.readdirSync(path.join(packagesRoot, "uno.wasm.bootstrap")).sort().at(-1);
    const toolsDir = path.join(packagesRoot, "uno.wasm.bootstrap", version, "tools");
    const dll = fs.readdirSync(toolsDir).find(name => /^Uno\.Wasm\.Bootstrap\.v.*\.dll$/.test(name));
    if (!dll) throw new Error(`No Uno.Wasm.Bootstrap tools assembly under ${toolsDir}`);
    return path.join(toolsDir, dll);
}

function findSdkDir() {
    const sdks = run("dotnet", ["--list-sdks"], "Could not list .NET SDKs").trim().split("\n");
    const [, version, base] = sdks.at(-1).match(/^(\S+)\s+\[(.+)\]$/);
    return path.join(base, version);
}

function merge() {
    const inputs = fs.existsSync(recordingsDir)
        ? fs.readdirSync(recordingsDir).filter(name => name.endsWith(".profile")).map(name => path.join(recordingsDir, name))
        : [];
    if (inputs.length === 0) throw new Error(`No .profile recordings under ${recordingsDir} - record first.`);
    console.log(`Merging ${inputs.length} recording(s) ...`);
    const staging = path.join(workDir, "merged.profile");
    const scriptFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), "aot-merge-")), "merge.ps1");
    fs.writeFileSync(scriptFile, mergeScript);
    const result = spawnSync("pwsh", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptFile,
        "-Output", staging, "-InputsJoined", inputs.join(";"), "-BootstrapDll", findBootstrapDll(), "-SdkDir", findSdkDir(),
        "-MinimumTableViewMethods", String(minimumTableViewMethods)],
        { cwd: repoRoot, stdio: "inherit", shell: false });
    if (result.status !== 0) {
        throw new Error(`Profile merge failed (exit code ${result.status}); ${path.relative(repoRoot, mergedOut)} was left as it was.`);
    }
    fs.copyFileSync(staging, mergedOut);
    console.log(`Wrote ${path.relative(repoRoot, mergedOut)}`);
}

(async () => {
    if (!args.includes("--merge-only")) {
        // Settle the dependency question before the build, so the long part runs unattended.
        const chromium = await ensureBrowserDependencies();
        if (!args.includes("--no-build")) publishProfileGenApp();
        await record(chromium);
    }
    merge();
    for (const warning of warnings) console.warn(`Warning: ${warning}`);
    console.log("Done. Publish again (tools/publish-and-serve-wasm.ps1) to apply the new profile, and spot-check the scroll.");
})().catch(error => {
    console.error(error.message ?? error);
    process.exit(1);
});
