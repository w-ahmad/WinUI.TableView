import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from run_mobile_tests import check_results, wait_for_results


class TestResultsTests(unittest.TestCase):
    def test_successful_suite(self):
        check_results("Uno tests complete: 336 passed, 0 failed.")

    def test_failed_suite(self):
        with self.assertRaisesRegex(RuntimeError, "1 failed"):
            check_results("Uno tests complete: 335 passed, 1 failed.")

    def test_empty_suite(self):
        with self.assertRaisesRegex(RuntimeError, "did not execute"):
            check_results("Uno tests complete: 0 passed, 0 failed.")

    def test_missing_summary(self):
        with self.assertRaisesRegex(RuntimeError, "did not report completion"):
            check_results("PASS SomeTest")

    def test_android_log_prefix(self):
        check_results("I/UnoTests(1234): Uno tests complete: 336 passed, 0 failed.")

    def test_last_summary_is_authoritative(self):
        with self.assertRaisesRegex(RuntimeError, "1 failed"):
            check_results("Uno tests complete: 1 passed, 0 failed.\n"
                          "Uno tests complete: 1 passed, 1 failed.")

    def test_process_exiting_without_results_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            log_path = Path(directory) / "test.log"
            log_path.write_text("Test host crashed")
            with subprocess.Popen([sys.executable, "-c", "pass"]) as process:
                process.wait()
                with self.assertRaisesRegex(RuntimeError, "exited before completion"):
                    wait_for_results(process, log_path, 1)

    def test_timeout_without_results_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            log_path = Path(directory) / "test.log"
            log_path.write_text("PASS SomeTest")
            with subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)"]) as process:
                try:
                    with self.assertRaisesRegex(TimeoutError, "did not finish"):
                        wait_for_results(process, log_path, 0.1)
                finally:
                    process.terminate()
                    process.wait()


if __name__ == "__main__":
    unittest.main()
