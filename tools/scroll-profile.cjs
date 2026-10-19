// Profile the published Uno sample with real browser wheel input.
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");
const { chromium } = require("../tests/WinUI.TableView.Tests.Uno/node_modules/playwright");
const { PNG } = require("../tests/WinUI.TableView.Tests.Uno/node_modules/playwright-core/lib/utilsBundle");

async function main() {
    const root = path.resolve(process.argv[2] || "artifacts/scroll-publish/wwwroot");
    const output = path.resolve(process.argv[3] || "artifacts/scroll-profile");
    const recordControls = process.argv.includes("--record-controls");
    const recordGrouping = process.argv.includes("--record-grouping");
    const record = process.argv.includes("--record-aot") || recordControls || recordGrouping;
    if (!fs.existsSync(path.join(root, "index.html"))) {
        throw new Error(`Publish the WASM sample first; no index.html in ${root}`);
    }
    fs.mkdirSync(output, { recursive: true });
    const types = { ".html": "text/html", ".js": "text/javascript", ".wasm": "application/wasm", ".json": "application/json", ".css": "text/css" };
    const server = http.createServer((request, response) => {
        let target = path.resolve(root, "." + decodeURIComponent(new URL(request.url, "http://localhost").pathname));
        if (target !== root && !target.startsWith(root + path.sep)) {
            response.writeHead(403).end();
            return;
        }
        if (fs.existsSync(target) && fs.statSync(target).isDirectory()) target = path.join(target, "index.html");
        fs.readFile(target, (error, content) => {
            if (error) { response.writeHead(404).end(); return; }
            response.setHeader("Content-Type", types[path.extname(target)] || "application/octet-stream");
            response.end(content);
        });
    });
    await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
    let browser;
    try {
        browser = await chromium.launch({
            headless: true, channel: "chromium",
            args: process.platform === "win32" ? ["--use-angle=d3d11", "--enable-gpu"] : ["--enable-gpu"]
        });
        const browserCdp = await browser.newBrowserCDPSession();
        const system = await browserCdp.send("SystemInfo.getInfo");
        fs.writeFileSync(path.join(output, "graphics.json"), JSON.stringify({ devices: system.gpu.devices, features: system.gpu.featureStatus }, null, 2));
        const page = await browser.newPage({ viewport: { width: 1280, height: 900 }, serviceWorkers: "block" });
        const logs = [];
        const nativeRequests = [];
        page.on("console", message => logs.push(message.text()));
        page.on("pageerror", error => logs.push(error.stack));
        page.on("request", request => {
            if (/dotnet\.native.*\.wasm/.test(request.url())) nativeRequests.push(new URL(request.url()).pathname);
        });
        await page.addInitScript(() => {
            window.addEventListener("unhandledrejection", event => console.error(`Unhandled rejection: ${event.reason?.stack || event.reason}`));
            window.addEventListener("error", event => console.error(`Window error: ${event.error?.stack || event.message}`));
        });
        await page.goto(`http://127.0.0.1:${server.address().port}`);
        await page.waitForTimeout(record ? 20000 : 8000);
        if (process.argv.includes("--inspect")) {
            fs.writeFileSync(path.join(output, "dom.html"), await page.content());
            fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
            await page.screenshot({ path: path.join(output, "startup.png") });
            console.log(JSON.stringify(await page.evaluate(() => ({
                title: document.title, text: document.body.innerText,
                canvas: document.querySelectorAll("canvas").length,
                bootstrap: Object.keys(globalThis.Uno?.WebAssembly?.Bootstrap || {}),
                bootstrapStatics: Object.getOwnPropertyNames(globalThis.Uno?.WebAssembly?.Bootstrap?.Bootstrapper || {}),
                app: typeof globalThis.App
            }))));
            return;
        }
        await page.locator("#uno-canvas").waitFor({ state: "visible", timeout: 60000 });
        if (process.argv.includes("--scrollbar")) {
            const checks = [];
            const cdp = await page.context().newCDPSession(page);
            function deadline(promise, action, milliseconds = 3000) {
                let timer;
                return Promise.race([
                    promise,
                    new Promise((_, reject) => { timer = setTimeout(() => reject(new Error(`${action}: browser stopped responding for ${milliseconds} ms`)), milliseconds); })
                ]).finally(() => clearTimeout(timer));
            }
            async function check(action, input) {
                const start = Date.now();
                await deadline(input(), action);
                await deadline(page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))), action);
                checks.push({ action, elapsedMs: Date.now() - start });
                console.log(JSON.stringify(checks.at(-1)));
                if (logs.some(value => value && /RuntimeError|unhandled|Error:|Window error|NIY encountered|Assertion:/.test(value))) {
                    throw new Error(`${action}: scrollbar hover reported a runtime error; inspect console.txt`);
                }
            }
            await page.screenshot({ path: path.join(output, "scrollbar-before.png") });
            await cdp.send("Profiler.enable");
            await cdp.send("Profiler.start");
            try {
                // Overview's vertical scrollbar sits at the right edge of the table in this viewport.
                const iterations = process.argv.includes("--hover-only") ? 1 : process.argv.includes("--soak") ? 20 : 4;
                for (let iteration = 0; iteration < iterations; iteration++) {
                    if (!process.argv.includes("--hover-only")) {
                        await check(`cell-${iteration}`, () => page.mouse.click(480 + 150 * (iteration % 2), 165 + 41 * (iteration % 12)));
                    }
                    await check(`scrollbar-thumb-${iteration}`, () => page.mouse.move(1254, 165));
                    await check(`scrollbar-track-${iteration}`, () => page.mouse.move(1254, 400));
                    await check(`leave-scrollbar-${iteration}`, () => page.mouse.move(600, 450));
                }
                if (!process.argv.includes("--hover-only")) {
                    await check("add-cell-after-hover", async () => {
                        await page.keyboard.down("Control");
                        try { await page.mouse.click(815, 288); }
                        finally { await page.keyboard.up("Control"); }
                    });
                    await check("range-selection-after-hover", async () => {
                        await page.keyboard.down("Shift");
                        try { await page.mouse.click(630, 533); }
                        finally { await page.keyboard.up("Shift"); }
                    });
                    const clip = { x: 420, y: 190, width: 310, height: 270 };
                    const beforeSelection = await page.screenshot({ clip });
                    await check("select-cell-after-hover", () => page.mouse.click(630, 370));
                    const afterSelection = await page.screenshot({ clip });
                    if (beforeSelection.equals(afterSelection)) throw new Error("Cell selection stopped repainting after scrollbar hover");
                    await page.mouse.move(600, 450);
                    await check("scroll-after-hover", () => page.mouse.wheel(0, 160));
                    const afterScroll = await page.screenshot({ clip });
                    if (afterSelection.equals(afterScroll)) throw new Error("Rows stopped repainting after scrollbar hover");
                }
                await page.screenshot({ path: path.join(output, "scrollbar-after.png") });
                if (logs.some(value => value && /RuntimeError|unhandled|Error:|Window error/.test(value))) {
                    throw new Error("Scrollbar hover reported a runtime error; inspect console.txt");
                }
                console.log("Scrollbar hover and cell-selection responsiveness passed");
            } finally {
                fs.writeFileSync(path.join(output, "scrollbar-results.json"), JSON.stringify(checks, null, 2));
                fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
                try {
                    const { profile } = await deadline(cdp.send("Profiler.stop"), "stop CPU profiler", 5000);
                    fs.writeFileSync(path.join(output, "scrollbar.cpuprofile"), JSON.stringify(profile));
                } catch (error) { console.error(error.message); }
            }
            return;
        }
        if (recordGrouping) {
            await page.mouse.click(120, 550);
            await page.waitForTimeout(5000);
            await page.screenshot({ path: path.join(output, "grouping-before.png") });
            await page.mouse.click(1000, 500);
            await page.waitForTimeout(5000);
            await page.mouse.click(358, 238);
            await page.waitForTimeout(2000);
            await page.screenshot({ path: path.join(output, "grouping-expanded.png") });
            await page.mouse.move(600, 450);
            for (let step = 0; step < 10; step++) await page.mouse.wheel(0, 80);
            const downloadPromise = page.waitForEvent("download");
            await page.keyboard.press("Alt+Shift+KeyP");
            await (await downloadPromise).saveAs(path.join(output, "aot.profile"));
            fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
            console.log("Saved grouping AOT profile");
            return;
        }
        if (recordControls) {
            async function navigate(y) {
                await page.mouse.click(120, y);
                await page.waitForTimeout(2000);
            }
            await navigate(470);
            await page.mouse.click(1105, 177);
            await page.mouse.dblclick(460, 165);
            await page.keyboard.press("Control+A");
            await page.keyboard.type("ProfileName");
            await page.keyboard.press("Tab");
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, "editing.png") });
            await navigate(510);
            await page.mouse.click(500, 128);
            await page.waitForTimeout(2000);
            await page.screenshot({ path: path.join(output, "sorting.png") });
            // Invoking Sorting expands its one child, shifting the later entries down by 40 pixels.
            await navigate(630);
            await page.mouse.click(506, 128);
            await page.waitForTimeout(1000);
            await page.screenshot({ path: path.join(output, "filter-options.png") });
            await page.keyboard.press("Escape");
            await page.mouse.move(120, 700);
            await page.mouse.wheel(0, 800);
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, "navigation-bottom.png") });
            for (const [name, y] of [["row-header", 702], ["row-details", 742], ["compact", 782]]) {
                await navigate(y);
                if (name === "row-details") await page.mouse.click(350, 185);
                await page.mouse.move(600, 450);
                for (let step = 0; step < 10; step++) await page.mouse.wheel(0, 80);
                await page.screenshot({ path: path.join(output, `${name}.png`) });
            }
            const downloadPromise = page.waitForEvent("download");
            await page.keyboard.press("Alt+Shift+KeyP");
            await (await downloadPromise).saveAs(path.join(output, "aot.profile"));
            fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
            console.log("Saved current control-operation AOT profile");
            return;
        }
        const results = [];
        async function scroll(name, navigationY, wheelDelta) {
            if (navigationY) {
                await page.mouse.click(120, navigationY);
                await page.waitForTimeout(2000);
            }
            await page.mouse.move(600, 450);
            await page.screenshot({ path: path.join(output, `${name}-before.png`) });
            const rowClip = { x: 380, y: 240, width: 380, height: 160 };
            const beforeRows = await page.screenshot({ clip: rowClip });
            await page.evaluate(() => {
                window.scrollFrames = [];
                window.scrollMeasuring = true;
                let previous;
                function frame(now) {
                    if (!window.scrollMeasuring) return;
                    if (previous !== undefined) window.scrollFrames.push(now - previous);
                    previous = now;
                    requestAnimationFrame(frame);
                }
                requestAnimationFrame(frame);
            });
            // Stay within the 100-row grouping sample's scroll extent.
            for (let step = 0; step < (record ? 20 : 40); step++) {
                await page.mouse.wheel(0, wheelDelta ? wheelDelta(step) : 80);
                await new Promise(resolve => setTimeout(resolve, 16));
            }
            const frames = await page.evaluate(() => {
                window.scrollMeasuring = false;
                return window.scrollFrames;
            });
            const sorted = [...frames].sort((a, b) => a - b);
            const result = {
                name,
                // The raw intervals, in order: where the slow frames fall tells apart a periodic pause,
                // a cost tied to rows entering, and a uniformly heavy baseline.
                intervals: frames.map(value => Math.round(value * 10) / 10),
                frames: frames.length,
                elapsedMs: Math.round(frames.reduce((sum, value) => sum + value, 0)),
                p50Ms: sorted[Math.floor(sorted.length * .5)],
                p95Ms: sorted[Math.floor(sorted.length * .95)],
                maxMs: sorted.at(-1),
                over34ms: frames.filter(value => value > 34).length,
                slowPercent: Math.round(100 * frames.filter(value => value > 34).length / frames.length)
            };
            await page.screenshot({ path: path.join(output, `${name}-after.png`) });
            const afterRows = await page.screenshot({ clip: rowClip });
            // The hold scroll deliberately ends where it started.
            if (!record && !wheelDelta && beforeRows.equals(afterRows)) {
                throw new Error(`${name}: wheel input did not change the visible rows`);
            }
            results.push(result);
            console.log(JSON.stringify(result));
        }
        await scroll("overview");
        if (process.argv.includes("--warm")) await scroll("overview-warm");
        if (process.argv.includes("--hold")) {
            // Oscillates inside the realized range, so no row is recycled: how a small table feels.
            await scroll("overview-hold", undefined, step => step % 2 ? -160 : 160);
        }
        await scroll("gridlines", 110);
        await scroll("grouping", 550);
        await scroll("virtualization", 310);
        if (process.argv.includes("--fast")) {
            await page.mouse.click(120, 62);
            await page.waitForTimeout(2000);
            await page.mouse.move(600, 450);
            const gaps = [];
            for (let step = 0; step < 20; step++) {
                await page.mouse.wheel(0, 600);
                const screenshot = await page.screenshot({ clip: { x: 380, y: 200, width: 380, height: 420 } });
                fs.writeFileSync(path.join(output, `fast-${step}.png`), screenshot);
                const png = PNG.sync.read(screenshot);
                let gap = 0, maxGap = 0;
                for (let y = 40; y < png.height - 40; y++) {
                    let hasText = false;
                    for (let x = 0; x < png.width; x++) {
                        const offset = (y * png.width + x) * 4;
                        if (png.data[offset] < 140 && png.data[offset + 1] < 140 && png.data[offset + 2] < 140) {
                            hasText = true;
                            break;
                        }
                    }
                    gap = hasText ? 0 : gap + 1;
                    maxGap = Math.max(maxGap, gap);
                }
                gaps.push(maxGap);
                if (maxGap > 40) throw new Error(`Fast scroll ${step}: blank row-sized band (${maxGap} pixels)`);
            }
            fs.writeFileSync(path.join(output, "fast-scroll-gaps.json"), JSON.stringify(gaps));
            console.log(`Fast-scroll rendering passed: ${gaps.length} captures, largest text-free band ${Math.max(...gaps)} px`);
        }
        if (record) {
            // Exercise the other common row/cell templates before saving the recording.
            for (const [name, y] of [["grid-lines", 110], ["selection", 150], ["corner-button", 190],
                ["alternate-colors", 230], ["context-flyouts", 270], ["virtualization", 310],
                ["row-reorder", 350], ["pagination", 390], ["incremental-loading", 430],
                ["editing", 470], ["clipboard", 630],
                ["export", 670], ["conditional-styling", 750], ["column-sizing", 790], ["headers", 830]]) {
                await scroll(name, y);
            }
            await page.mouse.click(120, 310);
            await page.waitForTimeout(2000);
            await page.mouse.click(920, 283);
            await page.mouse.click(1020, 283);
            await page.mouse.click(940, 359);
            await page.keyboard.press("Control+A");
            await page.keyboard.type("5000");
            await page.keyboard.press("Enter");
            await page.mouse.click(1068, 359);
            await page.waitForTimeout(1000);
            await page.screenshot({ path: path.join(output, "record-jump.png") });
            await page.mouse.click(120, 550);
            await page.waitForTimeout(2000);
            await page.mouse.click(1000, 500);
            await page.waitForTimeout(1000);
            await page.mouse.click(358, 238);
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, "record-group-expand.png") });
            await page.mouse.click(1120, 38);
            await page.waitForTimeout(500);
            await page.mouse.click(1120, 38);
            const downloadPromise = page.waitForEvent("download");
            await page.keyboard.press("Alt+Shift+KeyP");
            const download = await downloadPromise;
            await download.saveAs(path.join(output, "aot.profile"));
            fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
            console.log("Saved current scrolling AOT profile");
            return;
        }
        if (process.argv.includes("--cpu")) {
            await page.mouse.click(120, 62);
            await page.waitForTimeout(2000);
            const cdp = await page.context().newCDPSession(page);
            await cdp.send("Profiler.enable");
            await cdp.send("Profiler.start");
            await scroll("overview-profiled");
            const { profile } = await cdp.send("Profiler.stop");
            fs.writeFileSync(path.join(output, "scroll.cpuprofile"), JSON.stringify(profile));
        }
        if (process.argv.includes("--smoke")) {
            await page.mouse.click(120, 310);
            await page.waitForTimeout(2000);
            await page.mouse.click(920, 283);
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, "select-all.png") });
            await page.mouse.click(1020, 283);
            await page.waitForTimeout(500);
            await page.screenshot({ path: path.join(output, "clear-selection.png") });
            await page.mouse.click(120, 550);
            await page.waitForTimeout(2000);
            await page.mouse.click(1000, 500);
            await page.waitForTimeout(1000);
            await page.screenshot({ path: path.join(output, "group-by-name.png") });
        }
        fs.writeFileSync(path.join(output, "results.json"), JSON.stringify(results, null, 2));
        fs.writeFileSync(path.join(output, "native-runtime.json"), JSON.stringify(nativeRequests, null, 2));
        fs.writeFileSync(path.join(output, "console.txt"), logs.join("\n"));
        if (logs.some(value => value && /RuntimeError|unhandled|Error:/.test(value))) {
            throw new Error("The WASM sample reported an error; inspect console.txt");
        }
        if (results[0].slowPercent > 10) {
            console.error("SCROLL BUDGET FAILED: more than 10% of overview frames exceed 34 ms");
            process.exitCode = 2;
        }
    } finally {
        if (browser) await browser.close();
        server.close();
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
