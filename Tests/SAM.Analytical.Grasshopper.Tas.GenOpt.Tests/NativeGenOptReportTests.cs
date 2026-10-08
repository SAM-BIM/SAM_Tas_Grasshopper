// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core;
using SAM.Math;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests
{
    /// <summary>
    /// How a kernel result becomes what the component shows. The results are real SAM.Math results, produced with a
    /// delegate evaluator; no process is involved.
    /// </summary>
    [TestFixture]
    public class NativeGenOptReportTests
    {
        private static readonly string[] ParameterNames = { "x" };
        private static readonly string[] ObjectiveNames = { "Result", "Cost" };

        private static OptimisationResult Run(Optimiser optimiser, Func<double, double> objective, CancellationToken cancellationToken = default(CancellationToken))
        {
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 10, -5, 35, 2) }, 2);
            return optimiser.Run(problem, new DelegateObjectiveEvaluator((request, token) =>
            {
                double x = request.Coordinates[0];
                double f = objective(x);
                return double.IsInfinity(f) ? ObjectiveEvaluation.Failure("injected failure") : ObjectiveEvaluation.Success(f, 2 * x);
            }), null, cancellationToken);
        }

        private static GoldenSection GoldenSection()
        {
            return new GoldenSection { StoppingCriterion = GoldenSectionStoppingCriterion.AbsoluteDifference, AbsoluteDifference = 0.1 };
        }

        [Test]
        public void MaximumSimulationsReached_IsSuccessfulWithAWarningAndTheCurrentLowestPoint()
        {
            OptimisationResult result = Run(new HookeJeeves { MaximumSimulations = 3 }, x => (x - 5) * (x - 5));
            Assert.That(result.Outcome, Is.EqualTo(OptimisationOutcome.MaximumSimulationsReached));

            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            Assert.That(report.Successful, Is.True);
            Assert.That(report.Log.Count(x => x.LogRecordType == LogRecordType.Warning), Is.EqualTo(1));
            Assert.That(report.BestPoint, Is.EqualTo(result.Minimum.Coordinates));
            Assert.That(report.BestObjectives, Is.EqualTo(result.Minimum.Outputs));
        }

        [Test]
        public void GoldenSection_BestPointIsTheFirstLowestEntry()
        {
            // A flat objective: every entry ties, so the first one is reported (the PR3 acceptance definition).
            OptimisationResult result = Run(GoldenSection(), x => 7.0);
            Assert.That(result.Minimum, Is.Null, "Golden section reports no minimum entry.");
            Assert.That(result.Entries.Count, Is.GreaterThan(1));

            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            Assert.That(report.BestPoint, Is.EqualTo(result.Entries[0].Coordinates));
            Assert.That(report.Log.Select(x => x.Text), Has.Some.StartsWith("Final interval: ["));
        }

        [Test]
        public void GoldenSection_BestPointIsTheLowestObjective()
        {
            OptimisationResult result = Run(GoldenSection(), x => (x - 4.9) * (x - 4.9));
            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            double lowest = result.Entries.Min(x => x.Objective);
            Assert.That(report.Successful, Is.True);
            Assert.That(report.BestObjectives[0], Is.EqualTo(lowest));
            Assert.That(report.BestPoint, Is.EqualTo(result.Entries.First(x => x.Objective == lowest).Coordinates));
            Assert.That(report.Log.Select(x => x.Text), Has.Some.EqualTo("Best point: " + NativeGenOptReport.Text(ParameterNames, report.BestPoint) + "; " + NativeGenOptReport.Text(ObjectiveNames, report.BestObjectives) + "."));
        }

        [Test]
        public void NotANumberObjectives_AreNeverTheBestPoint()
        {
            // The first golden-section points are about 10.3 and 19.7 on [-5, 35].
            OptimisationResult result = Run(GoldenSection(), x => x > 15 ? double.NaN : x);
            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            Assert.That(result.Entries.Any(x => double.IsNaN(x.Objective)), Is.True);
            Assert.That(report.BestObjectives, Is.Not.Empty);
            Assert.That(double.IsNaN(report.BestObjectives[0]), Is.False);
            Assert.That(report.BestPoint, Is.EqualTo(NativeGenOptOutcome.Best(result).Coordinates), "The component reports SAM_Tas' best point (PR6).");
        }

        [Test]
        public void CancelObservedAfterTheRun_WithholdsTheResult()
        {
            OptimisationResult result = Run(new HookeJeeves(), x => (x - 5) * (x - 5));
            Assert.That(result.Outcome, Is.EqualTo(OptimisationOutcome.Success));

            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, @"C:\runs\r1", true);

            Assert.That(report.Successful, Is.False);
            Assert.That(report.Outcome, Is.EqualTo(OptimisationOutcome.Success), "The kernel's own outcome is still reported.");
            Assert.That(report.BestPoint, Is.Empty);
            Assert.That(report.BestObjectives, Is.Empty);
            Assert.That(report.Log.Select(x => x.Text), Has.Some.Contains("The result is withheld"));
        }

        [Test]
        public void Cancelled_IsARemarkWithoutABestPoint()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();
                OptimisationResult result = Run(new HookeJeeves(), x => x, cancellationTokenSource.Token);
                Assert.That(result.Outcome, Is.EqualTo(OptimisationOutcome.Cancelled));

                NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, true);

                Assert.That(report.Successful, Is.False);
                Assert.That(report.BestPoint, Is.Empty);
                Assert.That(report.Log.Select(x => x.LogRecordType), Is.EqualTo(new[] { LogRecordType.Message }));
            }
        }

        [Test]
        public void EvaluationFailed_IsAnError()
        {
            OptimisationResult result = Run(new HookeJeeves(), x => double.PositiveInfinity);
            Assert.That(result.Outcome, Is.EqualTo(OptimisationOutcome.EvaluationFailed));

            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            Assert.That(report.Successful, Is.False);
            Assert.That(report.Log.Where(x => x.LogRecordType == LogRecordType.Error).Select(x => x.Text), Is.EqualTo(new[] { "Tas evaluation failed at simulation 1: injected failure" }));
        }

        [Test]
        public void InitialPointInfeasible_IsAnError()
        {
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 50, -5, 35, 2) }, 1);
            OptimisationResult result = new HookeJeeves().Run(problem, new DelegateObjectiveEvaluator((request, token) => ObjectiveEvaluation.Success(request.Coordinates[0])));
            Assume.That(result.Outcome, Is.EqualTo(OptimisationOutcome.InitialPointInfeasible));

            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, null, false);

            Assert.That(report.Successful, Is.False);
            Assert.That(report.Log.Single(x => x.LogRecordType == LogRecordType.Error).Text, Is.EqualTo("The initial point is outside the parameter bounds."));
        }

        [Test]
        public void RunFolder_IsReportedAndBracesInMessagesAreKept()
        {
            // Log.Add takes a format string; texts are passed as an argument so paths and evaluator messages with
            // braces are never reformatted.
            OptimisationResult result = Run(new HookeJeeves(), x => double.PositiveInfinity);
            NativeGenOptReport report = new NativeGenOptReport(result, ParameterNames, ObjectiveNames, @"C:\runs\{0}", false);

            Assert.That(report.RunDirectory, Is.EqualTo(@"C:\runs\{0}"));
            Assert.That(report.Log.Select(x => x.Text), Has.Some.EqualTo(@"Run folder: C:\runs\{0}"));
        }

        [Test]
        public void Exceptions_AreErrorsNamingTheCause()
        {
            Assert.That(new NativeGenOptReport(new GenOptCompatibilityException("AbsDiffFunction ...")).Log.Single().Text, Is.EqualTo("Invalid GenOpt settings for the native route: AbsDiffFunction ..."));
            Assert.That(new NativeGenOptReport(new NotSupportedException("Parametric ...")).Log.Single().Text, Is.EqualTo("Not supported by the native route: Parametric ..."));
            Assert.That(new NativeGenOptReport(new FileNotFoundException("TasGenExecute was not found.", @"C:\x\TasGenExecute.exe")).Log.Single().Text, Is.EqualTo(@"TasGenExecute was not found. Path: 'C:\x\TasGenExecute.exe'."));
            Assert.That(new NativeGenOptReport(new DirectoryNotFoundException("The optimisation workspace does not exist: 'C:\\w'.")).Log.Single().Text, Is.EqualTo("The optimisation workspace does not exist: 'C:\\w'."));
            Assert.That(new NativeGenOptReport(new IOException("disk full")).Log.Single().Text, Is.EqualTo("Native optimisation failed (IOException): disk full"));
            Assert.That(new NativeGenOptReport(new IOException("x")).Log.Single().LogRecordType, Is.EqualTo(LogRecordType.Error));
        }
    }
}
