import argparse
import json
from pathlib import Path
import re
import subprocess
import sys
import time


APP_ID = "com.W.Ahmad.WinUI.TableView.Tests"
PROJECT = Path(__file__).resolve().parents[1]
SUMMARY = re.compile(r"Uno tests complete: (\d+) passed, (\d+) failed\.")


def check_results(output):
    matches = list(SUMMARY.finditer(output))
    if not matches:
        raise RuntimeError("The Uno test host did not report completion.")
    passed, failed = map(int, matches[-1].groups())
    if passed + failed == 0:
        raise RuntimeError("The Uno test host did not execute any tests.")
    if failed:
        raise RuntimeError(f"Uno tests failed: {passed} passed, {failed} failed.")
    print(f"Uno tests complete: {passed} passed, {failed} failed.", flush=True)


def wait_for_results(process, log_path, timeout):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        output = log_path.read_text(errors="replace")
        if SUMMARY.search(output):
            print(output, flush=True)
            check_results(output)
            return
        if process.poll() is not None:
            print(output, flush=True)
            raise RuntimeError(f"Test log process exited before completion: {process.returncode}")
        time.sleep(0.5)
    print(log_path.read_text(errors="replace"), flush=True)
    raise TimeoutError(f"Uno tests did not finish within {timeout} seconds.")


def find_artifact(pattern):
    artifacts = list((PROJECT / "bin" / "Debug").glob(pattern))
    if len(artifacts) != 1:
        raise RuntimeError(f"Expected one test app matching {pattern}, found: {artifacts}")
    return artifacts[0]


def stop_process(process):
    if process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()


def run_android(log_path, timeout):
    apk = find_artifact("net10.0-android/*-Signed.apk")
    subprocess.run(["adb", "wait-for-device"], check=True, timeout=120)
    subprocess.run(["adb", "install", "-r", str(apk)], check=True, timeout=120)
    subprocess.run(["adb", "logcat", "-c"], check=True)

    with log_path.open("w") as log:
        process = subprocess.Popen(
            ["adb", "logcat", "-v", "brief", "UnoTests:I", "AndroidRuntime:E", "*:S"],
            stdout=log, stderr=subprocess.STDOUT)
        try:
            launch = subprocess.run(
                ["adb", "shell", "monkey", "-p", APP_ID,
                 "-c", "android.intent.category.LAUNCHER", "1"],
                check=True, capture_output=True, text=True, timeout=60)
            print(launch.stdout, flush=True)
            if "Events injected: 1" not in launch.stdout:
                raise RuntimeError(f"Could not launch the Android test app: {launch.stderr}")
            wait_for_results(process, log_path, timeout)
        finally:
            stop_process(process)
            subprocess.run(["adb", "shell", "am", "force-stop", APP_ID], check=True)


def run_ios(log_path, timeout):
    app = find_artifact("net10.0-ios/iossimulator-*/*.app")
    devices = json.loads(subprocess.check_output(
        ["xcrun", "simctl", "list", "devices", "available", "--json"], text=True))
    candidates = [
        device
        for runtime, runtime_devices in devices["devices"].items()
        if runtime.endswith(".iOS-26-0")
        for device in runtime_devices
        if device["isAvailable"] and device["name"].startswith("iPad")
    ]
    if not candidates:
        raise RuntimeError("An available iOS 26.0 iPad simulator is required.")
    device = candidates[0]
    udid = device["udid"]
    booted_by_runner = device["state"] != "Booted"
    if booted_by_runner:
        subprocess.run(["xcrun", "simctl", "boot", udid], check=True)
    try:
        subprocess.run(["xcrun", "simctl", "bootstatus", udid, "-b"], check=True, timeout=180)
        subprocess.run(["open", "-a", "Simulator", "--args", "-CurrentDeviceUDID", udid], check=True)
        subprocess.run(["xcrun", "simctl", "install", udid, str(app)], check=True, timeout=120)
        with log_path.open("w") as log:
            process = subprocess.Popen(
                ["xcrun", "simctl", "launch", "--console", "--terminate-running-process", udid, APP_ID],
                stdout=log, stderr=subprocess.STDOUT)
            try:
                wait_for_results(process, log_path, timeout)
            finally:
                stop_process(process)
    finally:
        if booted_by_runner:
            subprocess.run(["xcrun", "simctl", "shutdown", udid], check=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("platform", choices=["android", "ios"], nargs="?")
    parser.add_argument("--check-log", type=Path)
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()
    if args.check_log:
        check_results(args.check_log.read_text(errors="replace"))
        return
    if not args.platform:
        parser.error("A platform or --check-log is required.")
    if args.timeout <= 0:
        parser.error("--timeout must be positive.")
    results = PROJECT / "test-results"
    results.mkdir(parents=True, exist_ok=True)
    log_path = results / f"{args.platform}.log"
    if args.platform == "android":
        run_android(log_path, args.timeout)
    else:
        run_ios(log_path, args.timeout)


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, TimeoutError, subprocess.SubprocessError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
