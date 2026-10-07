// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.GenOpt;
using SAM.Core;
using SAM.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    /// <summary>
    /// What the SAMAnalytical.GenOpt component reports for one native run: read from the kernel's structured
    /// <see cref="OptimisationResult"/>, never from GenOpt output files. It holds no Grasshopper types; the component
    /// turns <see cref="Log"/> into runtime messages (Message = remark).
    /// </summary>
    public sealed class NativeGenOptReport
    {
        private static readonly IReadOnlyList<double> none = Array.AsReadOnly(new double[0]);

        /// <summary>The run was refused or failed outside the kernel (invalid settings, missing TasGenExecute, ...).</summary>
        public NativeGenOptReport(Exception exception)
        {
            Outcome = OptimisationOutcome.Undefined;
            BestPoint = none;
            BestObjectives = none;
            Log = new Log();
            Log.Add("{0}", LogRecordType.Error, Message(exception));
        }

        public NativeGenOptReport(NativeGenOptRun nativeGenOptRun, bool cancelledAfterRun)
            : this(nativeGenOptRun?.Result, nativeGenOptRun?.ParameterNames, nativeGenOptRun?.ObjectiveNames, nativeGenOptRun?.Workspace?.RunDirectory, cancelledAfterRun)
        {
        }

        /// <param name="result">The kernel result.</param>
        /// <param name="parameterNames">Parameter names in coordinate order.</param>
        /// <param name="objectiveNames">Objective names in output order; the first is minimised.</param>
        /// <param name="runDirectory">The run folder (project snapshot and evaluation folders).</param>
        /// <param name="cancelledAfterRun">
        /// The user's cancel was observed only after the kernel returned, or the progress dialog could not confirm
        /// that it shut down (so a cancel may be unobserved). The result is then withheld, as SAM's workflow
        /// components do: success is never claimed for a run the user asked to stop.
        /// </param>
        public NativeGenOptReport(OptimisationResult result, IReadOnlyList<string> parameterNames, IReadOnlyList<string> objectiveNames, string runDirectory, bool cancelledAfterRun)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            Outcome = result.Outcome;
            Simulations = result.Simulations;
            RunDirectory = runDirectory;
            BestPoint = none;
            BestObjectives = none;
            Log = new Log();

            switch (result.Outcome)
            {
                case OptimisationOutcome.Success:
                    Successful = true;
                    Log.Add("{0}", LogRecordType.Message, string.Format(CultureInfo.InvariantCulture, "Native optimisation finished: Success after {0} simulations.", result.Simulations));
                    break;

                case OptimisationOutcome.MaximumSimulationsReached:
                    Successful = true;
                    Log.Add("{0}", LogRecordType.Warning, string.Format(CultureInfo.InvariantCulture, "The simulation limit was reached ({0} simulations) before the stopping criterion was met; the lowest point found is reported.", result.Simulations));
                    break;

                case OptimisationOutcome.Nullspace:
                    Successful = true;
                    Log.Add("{0}", LogRecordType.Warning, "Golden section stopped on two consecutive equal objective values (nullspace); the lowest point found is reported.");
                    break;

                case OptimisationOutcome.Cancelled:
                    Log.Add("{0}", LogRecordType.Message, string.Format(CultureInfo.InvariantCulture, "Optimisation cancelled by user (kernel simulation count {0}, which includes the number assigned when the cancel was observed). No best point is reported; the completed evaluations remain in the run folder.", result.Simulations));
                    break;

                case OptimisationOutcome.EvaluationFailed:
                    Log.Add("{0}", LogRecordType.Error, string.Format(CultureInfo.InvariantCulture, "Tas evaluation failed at simulation {0}: {1}", result.FailedSimulation, result.FailureMessage));
                    break;

                case OptimisationOutcome.InitialPointInfeasible:
                    Log.Add("{0}", LogRecordType.Error, "The initial point is outside the parameter bounds.");
                    break;

                default:
                    Log.Add("{0}", LogRecordType.Error, "The optimisation stopped with an error: " + result.Outcome + ".");
                    break;
            }

            if (Successful && cancelledAfterRun)
            {
                Successful = false;
                Log.Add("{0}", LogRecordType.Message, "Optimisation cancelled by user as the run finished (kernel outcome: " + result.Outcome + "). The result is withheld; the evaluation folders remain in the run folder.");
            }

            if (Successful)
            {
                OptimisationTraceEntry best = Best(result);
                if (best != null)
                {
                    BestPoint = best.Coordinates;
                    BestObjectives = best.Outputs;
                    Log.Add("{0}", LogRecordType.Message, "Best point: " + Text(parameterNames, best.Coordinates) + "; " + Text(objectiveNames, best.Outputs) + ".");
                }

                if (result.Interval != null)
                {
                    Log.Add("{0}", LogRecordType.Message, string.Format(CultureInfo.InvariantCulture, "Final interval: [{0}, {1}].", result.Interval.Lower, result.Interval.Upper));
                }
            }

            if (result.Retries > 0)
            {
                Log.Add("{0}", LogRecordType.Message, string.Format(CultureInfo.InvariantCulture, "{0} evaluation(s) failed once and were retried.", result.Retries));
            }

            if (!string.IsNullOrWhiteSpace(runDirectory))
            {
                Log.Add("{0}", LogRecordType.Message, "Run folder: " + runDirectory);
            }
        }

        /// <summary>True when the run ended normally (Success, simulation limit or nullspace) and was not cancelled.</summary>
        public bool Successful { get; }

        /// <summary>The kernel outcome; <see cref="OptimisationOutcome.Undefined"/> when the run never started.</summary>
        public OptimisationOutcome Outcome { get; }

        public int Simulations { get; }

        /// <summary>The best point's parameter values in coordinate order; empty unless <see cref="Successful"/>.</summary>
        public IReadOnlyList<double> BestPoint { get; }

        /// <summary>The best point's objective values in output order; empty unless <see cref="Successful"/>.</summary>
        public IReadOnlyList<double> BestObjectives { get; }

        public string RunDirectory { get; }

        public Log Log { get; }

        /// <summary>
        /// The kernel's reported minimum (pattern search). Golden section reports none, so its best point is the
        /// lowest objective among all entries, the first one on a tie (the PR3 acceptance definition).
        /// </summary>
        public static OptimisationTraceEntry Best(OptimisationResult result)
        {
            if (result == null)
            {
                return null;
            }

            if (result.Minimum != null)
            {
                return result.Minimum;
            }

            OptimisationTraceEntry best = null;
            foreach (OptimisationTraceEntry entry in result.Entries)
            {
                if (double.IsNaN(entry.Objective))
                {
                    continue;
                }

                if (best == null || entry.Objective < best.Objective)
                {
                    best = entry;
                }
            }

            return best;
        }

        public static string Text(IReadOnlyList<string> names, IReadOnlyList<double> values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            return string.Join(", ", values.Select((x, i) => (names != null && i < names.Count ? names[i] : "#" + i) + " = " + x.ToString(CultureInfo.InvariantCulture)));
        }

        private static string Message(Exception exception)
        {
            switch (exception)
            {
                case GenOptCompatibilityException _:
                    return "Invalid GenOpt settings for the native route: " + exception.Message;

                case NotSupportedException _:
                    return "Not supported by the native route: " + exception.Message;

                case System.IO.FileNotFoundException fileNotFoundException:
                    return exception.Message + " Path: '" + fileNotFoundException.FileName + "'.";

                case System.IO.DirectoryNotFoundException _:
                    return exception.Message;

                default:
                    return "Native optimisation failed (" + exception?.GetType().Name + "): " + exception?.Message;
            }
        }
    }
}
