#if WINDOWS
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace WinUI.TableView.SampleApp;

/// <summary>
/// Records UI-thread frame intervals and GC pauses to a CSV while the app runs, when the
/// TABLEVIEW_FRAME_LOG environment variable names the file to write.
/// </summary>
/// <remarks>
/// A row per rendered frame: elapsed milliseconds, the interval since the previous frame, how much
/// the process's total GC pause time grew in that interval, and the collection counts per generation.
/// A long interval with a matching pause delta is a GC stall; one without is UI-thread work. The file
/// is appended in chunks, so the tail of a run can lag by up to a chunk until the next flush.
/// </remarks>
internal static class ScrollProbe
{
    public static void TryStart()
    {
        if (Environment.GetEnvironmentVariable("TABLEVIEW_FRAME_LOG") is not { Length: > 0 } path)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var lastMs = 0d;
        var lastPause = GC.GetTotalPauseDuration();
        var buffer = new StringBuilder("ms,interval_ms,gc_pause_delta_ms,gen0,gen1,gen2\n");

        CompositionTarget.Rendering += (_, _) =>
        {
            var now = stopwatch.Elapsed.TotalMilliseconds;
            var pause = GC.GetTotalPauseDuration();

            buffer.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{now:F1},{now - lastMs:F1},{(pause - lastPause).TotalMilliseconds:F1},{GC.CollectionCount(0)},{GC.CollectionCount(1)},{GC.CollectionCount(2)}"));

            // Chunked appends keep the per-frame cost to the StringBuilder; a long gap flushes too, so
            // the data is on disk whenever the app sits idle.
            if (buffer.Length > 8_192 || now - lastMs > 500d)
            {
                File.AppendAllText(path, buffer.ToString());
                buffer.Clear();
            }

            lastMs = now;
            lastPause = pause;
        };
    }
}
#endif
