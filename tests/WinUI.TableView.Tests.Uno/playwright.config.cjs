const path = require("node:path");
const { defineConfig } = require("@playwright/test");
const wwwroot = path.join(__dirname, "bin", "Release", "net10.0-browserwasm", "wwwroot");

module.exports = defineConfig({
    testDir: "./ci",
    testMatch: "wasm.spec.cjs",
    timeout: 660_000,
    workers: 1,
    reporter: [["list"], ["junit", { outputFile: "test-results/wasm.xml" }]],
    use: {
        baseURL: "http://127.0.0.1:8080",
        viewport: { width: 1280, height: 900 },
        screenshot: "only-on-failure"
    },
    webServer: {
        command: `${process.platform === "win32" ? "python" : "python3"} -m http.server 8080 --bind 127.0.0.1 --directory "${wwwroot}"`,
        url: "http://127.0.0.1:8080",
        timeout: 30_000,
        reuseExistingServer: false
    }
});
