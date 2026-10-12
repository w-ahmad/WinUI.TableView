// Profile the published Uno sample with real browser wheel input.
const fs = require("node:fs");
const path = require("node:path");
const http = require("node:http");
const { chromium } = require("../tests/WinUI.TableView.Tests.Uno/node_modules/playwright");
const { PNG } = require("../tests/WinUI.TableView.Tests.Uno/node_modules/playwright-core/lib/utilsBundle");

async function main() {
    const root = path.resolve(process.argv[2] || "artifacts/scroll-publish/wwwroot");
    const output = path.resolve(process.argv[3] || "artifacts/scroll-profile");
    const record = process.argv.includes("--record-aot");
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
        await page.goto(`http://127.0.0.1:${server.address().port}`);
        await page.waitForTimeout(record ? 20000 : 8000);
        const results = [];
        async function scroll(name, navigationY) {
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
                await page.mouse.wheel(0, 80);
                await new Promise(resolve => setTimeout(resolve, 16));
            }
            const frames = await page.evaluate(() => {
                window.scrollMeasuring = false;
                return window.scrollFrames;
            });
            const sorted = [...frames].sort((a, b) => a - b);
            const result = {
                name, frames: frames.length,
                elapsedMs: Math.round(frames.reduce((sum, value) => sum + value, 0)),
                p50Ms: sorted[Math.floor(sorted.length * .5)],
                p95Ms: sorted[Math.floor(sorted.length * .95)],
                maxMs: sorted.at(-1),
                over34ms: frames.filter(value => value > 34).length,
                slowPercent: Math.round(100 * frames.filter(value => value > 34).length / frames.length)
            };
            await page.screenshot({ path: path.join(output, `${name}-after.png`) });
            const afterRows = await page.screenshot({ clip: rowClip });
            if (!record && beforeRows.equals(afterRows)) {
                throw new Error(`${name}: wheel input did not change the visible rows`);
            }
            results.push(result);
            console.log(JSON.stringify(result));
        }
        await scroll("overview");
        if (process.argv.includes("--warm")) await scroll("overview-warm");
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
            for (const [name, y] of [["grid-lines", 110], ["selection", 150], ["alternate-colors", 230],
                ["virtualization", 310], ["editing", 470], ["sorting", 510], ["filtering", 590],
                ["conditional-styling", 750], ["column-sizing", 790], ["headers", 830]]) {
                await scroll(name, y);
            }
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
