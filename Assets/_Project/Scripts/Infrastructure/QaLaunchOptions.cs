using System;
using System.Diagnostics;

namespace CayTheGioi.Infrastructure
{
    /// <summary>
    /// Opt-in QA launch arguments for standalone builds (T09). A normal launch passes none of them, so the
    /// player keeps using Application.persistentDataPath and the real UTC clock.
    ///   -ctg-save-dir &lt;path&gt;   isolated save directory (never the player's real save)
    ///   -ctg-utc &lt;unixSeconds&gt; QA clock base; only honoured together with -ctg-save-dir
    ///   -ctg-scenario &lt;name&gt;   run a QA scenario (see Bootstrap/QaRunner)
    ///   -ctg-report &lt;path&gt;     JSON report path; -ctg-shots &lt;dir&gt; screenshot directory
    ///   -ctg-expect-elapsed &lt;seconds&gt; expected offline elapsed seconds for the offline scenarios
    /// </summary>
    public static class QaLaunchOptions
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        public static readonly string SaveDirectory = Read("-ctg-save-dir");
        public static readonly string Scenario = Read("-ctg-scenario");
        public static readonly string ReportPath = Read("-ctg-report");
        public static readonly string ScreenshotDirectory = Read("-ctg-shots");
        public static readonly string Extra = Read("-ctg-extra");
        public static readonly long? UtcBaseSeconds = ReadLong("-ctg-utc");
        public static readonly long? ExpectedElapsedSeconds = ReadLong("-ctg-expect-elapsed");
        public static bool Active => !string.IsNullOrEmpty(SaveDirectory);

        /// <summary>QA clock: base plus real seconds since launch. Null when no QA base was requested.</summary>
        public static long? QaUtcNow() => Active && UtcBaseSeconds.HasValue ? UtcBaseSeconds.Value + (long)Clock.Elapsed.TotalSeconds : (long?)null;

        private static string Read(string key)
        {
            string[] args;
            try { args = Environment.GetCommandLineArgs(); } catch { return null; }
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args[i + 1])) return args[i + 1];
            return null;
        }
        private static long? ReadLong(string key)
        {
            string value = Read(key);
            return long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long result) ? result : (long?)null;
        }
    }
}
