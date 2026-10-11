const { test, expect } = require("@playwright/test");

test("shared Uno suite passes in WebAssembly", async ({ page }, testInfo) => {
    const output = [];
    let summary;
    let pageError;

    page.on("console", message => {
        const text = message.text();
        output.push(text);
        console.log(text);
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
        await expect.poll(() => {
            if (pageError) {
                throw pageError;
            }
            return summary;
        }, { timeout: 600_000, message: "Waiting for the Uno test suite to finish" }).toBeDefined();
        expect(summary.passed, "The suite must execute tests").toBeGreaterThan(0);
        expect(summary.failed, "All shared tests must pass").toBe(0);
    } finally {
        await testInfo.attach("uno-test-output", {
            body: output.join("\n"),
            contentType: "text/plain"
        });
    }
});
