// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.GenOpt;
using SAM.Core.Windows.WPF;
using SAM.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    // Not extension methods: GenOptDocument.RunNative(...) is an instance method with all-optional parameters, and an
    // instance method always wins over an extension of the same name.
    public static partial class Modify
    {
        private const string CancelNote = "Cancel takes effect when the current Tas evaluation finishes - a running TasGenExecute simulation is never interrupted.";

        /// <summary>
        /// Runs <paramref name="genOptDocument"/> natively (<see cref="GenOptDocument.RunNative"/>: SAM.Math kernel,
        /// TasGenExecute, no Java) with a progress dialog that carries a Cancel button, and reports the outcome.
        /// <para>
        /// The dialog runs on its own UI thread (<see cref="ProgressWindowHost"/>), the pattern of the Tas workflow
        /// component (SAM.Analytical.Grasshopper.Tas Modify.RunWorkflow). The run itself stays on this thread, one
        /// evaluation at a time. Cancellation is cooperative: a running TasGenExecute finishes, then the kernel stops
        /// before the next evaluation.
        /// </para>
        /// </summary>
        public static NativeGenOptReport RunNative(GenOptDocument genOptDocument)
        {
            if (genOptDocument == null)
            {
                return new NativeGenOptReport(new ArgumentNullException(nameof(genOptDocument)));
            }

            NativeGenOptRun nativeGenOptRun = null;
            Exception exception = null;
            bool cancelledAfterRun;

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                // Not a using: the dialog is disposed BEFORE the final cancellation check below. Dispose closes the
                // window and joins its thread, so afterwards no further CancelRequested can arrive (see RunWorkflow).
                ProgressWindowHost progressWindowHost = new ProgressWindowHost("GenOpt", System.Math.Max(1, genOptDocument.OptimizationSettings?.MaxIterations ?? 1), true, CancelNote);

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();
                    // No number: golden section evaluates its first two points before the kernel reports anything.
                    progressWindowHost.Update("Running the first Tas evaluations", false);

                    ProgressText progressText = new ProgressText(genOptDocument);

                    nativeGenOptRun = RunNative(genOptDocument, null, x =>
                    {
                        bool newSimulation = progressText.Add(x, out string text);
                        progressWindowHost.Max = x.MaximumSimulations;
                        progressWindowHost.Update(text, newSimulation);
                    }, cancellationTokenSource.Token, out exception);
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // Past this point no cancel can be raised. If the host could not confirm its shutdown, a click may be
                // unobserved, so success cannot be claimed (same rule as RunWorkflow).
                cancelledAfterRun = cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted;
            }

            if (nativeGenOptRun == null)
            {
                return new NativeGenOptReport(exception ?? new InvalidOperationException("The native optimisation returned no result."));
            }

            return new NativeGenOptReport(nativeGenOptRun, cancelledAfterRun && nativeGenOptRun.Result.Outcome != OptimisationOutcome.Cancelled);
        }

        /// <summary>
        /// The run behind <see cref="RunNative(GenOptDocument)"/> without the dialog:
        /// <see cref="GenOptDocument.RunNative"/> with the default run folder, progress delivered synchronously on this
        /// thread, and any refusal or failure returned through <paramref name="exception"/> instead of thrown.
        /// </summary>
        /// <param name="tasGenExecutePath">Null for the installed TasGenExecute.</param>
        public static NativeGenOptRun RunNative(GenOptDocument genOptDocument, string tasGenExecutePath, Action<OptimisationProgress> progress, CancellationToken cancellationToken, out Exception exception)
        {
            exception = null;

            if (genOptDocument == null)
            {
                exception = new ArgumentNullException(nameof(genOptDocument));
                return null;
            }

            try
            {
                return genOptDocument.RunNative(null, tasGenExecutePath, progress == null ? null : new ActionProgress(progress), cancellationToken);
            }
            catch (Exception exception_Run)
            {
                exception = exception_Run;
                return null;
            }
        }

        /// <summary>
        /// Calls the action on the reporting thread. <see cref="Progress{T}"/> would post to the Grasshopper UI
        /// thread's context, which this run blocks, so every report would arrive after the run.
        /// </summary>
        private sealed class ActionProgress : IProgress<OptimisationProgress>
        {
            private readonly Action<OptimisationProgress> action;

            public ActionProgress(Action<OptimisationProgress> action)
            {
                this.action = action;
            }

            public void Report(OptimisationProgress value)
            {
                action(value);
            }
        }

        /// <summary>Progress dialog text, one line per trace entry; a new simulation number advances the bar.</summary>
        private sealed class ProgressText
        {
            private readonly IReadOnlyList<string> parameterNames;
            private readonly IReadOnlyList<string> objectiveNames;
            private int simulation;
            private double lowest = double.NaN;

            public ProgressText(GenOptDocument genOptDocument)
            {
                parameterNames = genOptDocument.CommandFile?.Parameters?.ConvertAll(x => x?.Name);
                objectiveNames = genOptDocument.ConfigFile?.Simulation?.ObjectiveFunctionLocation?.Objectives?.ConvertAll(x => x?.Name);
            }

            public bool Add(OptimisationProgress optimisationProgress, out string text)
            {
                OptimisationTraceEntry entry = optimisationProgress.Entry;

                bool result = entry.Simulation > simulation;
                if (result)
                {
                    simulation = entry.Simulation;
                }

                if (!double.IsNaN(entry.Objective) && (double.IsNaN(lowest) || entry.Objective < lowest))
                {
                    lowest = entry.Objective;
                }

                text = string.Format(CultureInfo.InvariantCulture, "Simulation {0}: {1} -> {2} (lowest {3})",
                    entry.Simulation,
                    NativeGenOptReport.Text(parameterNames, entry.Coordinates),
                    NativeGenOptReport.Text(objectiveNames, entry.Outputs.Take(1).ToList()),
                    lowest);

                return result;
            }
        }
    }
}
