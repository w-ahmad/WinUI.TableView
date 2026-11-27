const { test, expect } = require("@playwright/test");

// The suite's total time grows with every test and varies with the runner, so fail on a stall instead.
const stallTimeoutMs = 180_000;

test("shared Uno suite passes in WebAssembly", async ({ page }, testInfo) => {
    const output = [];
    let summary;
    let pageError;
    let lastOutputAt = Date.now();
    let lastTest = "(none started)";

    page.on("console", message => {
        const text = message.text();
        output.push(text);
        console.log(text);
        lastOutputAt = Date.now();
        const run = text.match(/^RUN (\S+)/);
        if (run) {
            lastTest = run[1];
        }
        const match = text.match(/Uno tests complete: (\d+) passed, (\d+) failed\./);
        if (match) {
            summary = { passed: Number(match[1]), failed: Number(match[2]) };
        }
    });
    page.on("pageerror", error => {
        output.push(error.stack || error.message);
        console.error(error.stack || error.message);
        pageError = error;
    });

    try {
        await page.addInitScript(() => {
            window.addEventListener("error", event =>
                console.error(`WASM unhandled error: ${event.error?.stack || event.message}`));
            window.addEventListener("unhandledrejection", event =>
                console.error(`WASM unhandled rejection: ${event.reason?.stack || event.reason}`));
        });
        await page.goto("/");
        lastOutputAt = Date.now();
        while (!summary) {
            if (pageError) {
                throw pageError;
            }
            if (Date.now() - lastOutputAt > stallTimeoutMs) {
                throw new Error(`The Uno test suite logged nothing for ${stallTimeoutMs / 1000}s; last test started: ${lastTest}`);
            }
            await page.waitForTimeout(1_000);
        }
        expect(summary.passed, "The suite must execute tests").toBeGreaterThan(0);
        expect(summary.failed, "All shared tests must pass").toBe(0);
    } finally {
        await testInfo.attach("uno-test-output", {
            body: output.join("\n"),
            contentType: "text/plain"
        });
    }
});
