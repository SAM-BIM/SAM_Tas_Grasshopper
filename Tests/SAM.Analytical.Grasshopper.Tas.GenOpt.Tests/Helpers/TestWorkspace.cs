// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests.Helpers
{
    /// <summary>
    /// A short, unique temporary workspace holding Script.txt, as the component's <c>_scriptPath</c> points at one.
    /// The script is a JSON spec for PR3's stub TasGenExecute (SAM_Tas StubTasGenExecute.Program.Spec). Deleted on
    /// dispose.
    /// </summary>
    public sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace(string script)
        {
            Directory = Path.Combine(Path.GetTempPath(), "SAMGenOptGH", Guid.NewGuid().ToString("N").Substring(0, 10));
            System.IO.Directory.CreateDirectory(Directory);
            ScriptPath = Path.Combine(Directory, "Script.txt");
            File.WriteAllText(ScriptPath, script);
        }

        public string Directory { get; }

        public string ScriptPath { get; }

        /// <summary>The default native run folder parent (<c>SAM_NativeGenOpt</c> in the workspace).</summary>
        public string RunsDirectory => Path.Combine(Directory, "SAM_NativeGenOpt");

        /// <summary>The protocol stand-in, copied next to the test assembly by its ProjectReference.</summary>
        public static string StubExecutable => Path.Combine(AppContext.BaseDirectory, "StubTasGenExecute.exe");

        /// <summary>Names of the evaluation folders of the single run in this workspace, sorted.</summary>
        public List<string> EvaluationFolders(string runDirectory)
        {
            string evaluations = Path.Combine(runDirectory, "evaluations");
            if (!System.IO.Directory.Exists(evaluations))
            {
                return new List<string>();
            }

            return System.IO.Directory.GetDirectories(evaluations).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>A stub spec: quadratic objective sum(w_i (x_i - c_i)^2) + offset; output k &gt; 0 is k times the coordinate sum.</summary>
        public static string StubScript(double[] center, IEnumerable<string> outputs = null, IDictionary<string, string> modes = null, IDictionary<string, int> sleepMs = null)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["kind"] = "quadratic",
                ["center"] = center ?? new double[0],
                ["weight"] = new double[0],
                ["offset"] = 0.0,
                ["quantum"] = 1.0,
                ["outputs"] = outputs ?? new[] { "Result" },
                ["modes"] = modes ?? new Dictionary<string, string>(),
                ["sleepMs"] = sleepMs ?? new Dictionary<string, int>(),
                ["format"] = "R",
                ["datePrefix"] = true,
            });
        }
    }
}
