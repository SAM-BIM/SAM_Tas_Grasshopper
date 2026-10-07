// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Tests.Helpers;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core;
using SAM.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests
{
    /// <summary>
    /// The Grasshopper side of the native route, end to end through PR3's stub TasGenExecute (a real child process):
    /// component inputs -> <see cref="Create.GenOptDocument"/> -> <see cref="Modify.RunNative(GenOptDocument, string, Action{OptimisationProgress}, CancellationToken, out Exception)"/>
    /// -> <see cref="NativeGenOptReport"/>. The kernel and the evaluator themselves are PR2/PR3's and are not re-tested.
    /// </summary>
    [TestFixture]
    public class NativeBoundaryTests
    {
        private static readonly IParameter GoldenSectionParameter = new NumberParameter { Name = "Setpoint", Initial = 3, Min = -5, Max = 35, Step = 1 };
        private static readonly IParameter HookeJeevesParameter = new NumberParameter { Name = "Setpoint", Initial = 10, Min = -5, Max = 35, Step = 2 };
        private static readonly string[] Outputs = { "Result", "Cost", "CO2" };

        private static List<Objective> Objectives()
        {
            return Outputs.Select(x => new Objective(x)).ToList();
        }

        [OneTimeSetUp]
        public void StubIsPresent()
        {
            Assert.That(File.Exists(TestWorkspace.StubExecutable), Is.True, "PR3's StubTasGenExecute must be built next to the tests: " + TestWorkspace.StubExecutable);
        }

        [Test]
        public void GenOptDocument_IsBuiltFromTheInputsAsBefore()
        {
            using (TestWorkspace workspace = new TestWorkspace("// script text"))
            {
                GPSHookeJeevesAlgorithm algorithm = new GPSHookeJeevesAlgorithm();
                GenOptDocument genOptDocument = Create.GenOptDocument(workspace.ScriptPath, new[] { HookeJeevesParameter }, Objectives(), algorithm);

                Assert.That(genOptDocument.Directory, Is.EqualTo(workspace.Directory));
                Assert.That(genOptDocument.ScriptFile.Script.Text, Is.EqualTo("// script text"));
                Assert.That(genOptDocument.Algorithm, Is.SameAs(algorithm));
                Assert.That(genOptDocument.CommandFile.Parameters, Is.EqualTo(new[] { HookeJeevesParameter }));

                List<Objective> objectives = genOptDocument.ConfigFile.Simulation.ObjectiveFunctionLocation.Objectives;
                Assert.That(objectives.Select(x => x.Name), Is.EqualTo(Outputs));
                Assert.That(objectives.Select(x => x.Delimiter), Is.EqualTo(Outputs.Select(x => x + "::")));

                Assert.That(Create.GenOptDocument(workspace.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), null).Algorithm, Is.InstanceOf<GoldenSectionAlgorithm>(), "A missing algorithm is still the default GoldenSection.");
            }
        }

        [Test]
        public void GoldenSection_ThroughTheComponentPath_IsTheDirectRunNativeResult()
        {
            string script = TestWorkspace.StubScript(new[] { 4.9 }, Outputs);
            using (TestWorkspace viaComponent = new TestWorkspace(script))
            using (TestWorkspace direct = new TestWorkspace(script))
            {
                int thread = Environment.CurrentManagedThreadId;
                List<OptimisationProgress> progress = new List<OptimisationProgress>();
                List<int> threads = new List<int>();

                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(viaComponent.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), new GoldenSectionAlgorithm()), TestWorkspace.StubExecutable, x =>
                {
                    progress.Add(x);
                    threads.Add(Environment.CurrentManagedThreadId);
                }, CancellationToken.None, out Exception exception);

                Assert.That(exception, Is.Null);
                Assert.That(run, Is.Not.Null);

                GenOptDocument reference = Create.GenOptDocument(direct.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), new GoldenSectionAlgorithm());
                OptimisationResult expected = reference.RunNative(null, TestWorkspace.StubExecutable).Result;

                OptimisationResult actual = run.Result;
                Assert.That(actual.Outcome, Is.EqualTo(OptimisationOutcome.Success));
                Assert.That(actual.Outcome, Is.EqualTo(expected.Outcome));
                Assert.That(actual.Simulations, Is.EqualTo(expected.Simulations));
                Assert.That(Bits(actual.Entries), Is.EqualTo(Bits(expected.Entries)), "Same candidate sequence and objectives, bit for bit.");

                // Progress is the kernel's own, delivered synchronously on the calling thread (the dialog cannot rely on
                // a SynchronizationContext: the Grasshopper UI thread is blocked by the run).
                Assert.That(progress.Count, Is.EqualTo(actual.Entries.Count + actual.MainIterations.Count));
                Assert.That(threads.Distinct(), Is.EqualTo(new[] { thread }));

                NativeGenOptReport report = new NativeGenOptReport(run, false);
                OptimisationTraceEntry lowest = actual.Entries.Where(x => x.Objective == actual.Entries.Min(y => y.Objective)).First();
                Assert.That(report.Successful, Is.True);
                Assert.That(report.Outcome, Is.EqualTo(OptimisationOutcome.Success));
                Assert.That(report.Simulations, Is.EqualTo(actual.Simulations));
                Assert.That(report.BestPoint, Is.EqualTo(lowest.Coordinates));
                Assert.That(report.BestObjectives, Is.EqualTo(lowest.Outputs));
                Assert.That(report.RunDirectory, Does.StartWith(viaComponent.RunsDirectory));
                Assert.That(viaComponent.EvaluationFolders(report.RunDirectory).Count, Is.EqualTo(actual.Simulations));
                Assert.That(report.Log.Any(x => x.LogRecordType == LogRecordType.Error || x.LogRecordType == LogRecordType.Warning), Is.False);
                Assert.That(report.Log.Select(x => x.Text), Has.Some.StartsWith("Best point: Setpoint = "));
            }
        }

        [Test]
        public void HookeJeeves_BestPointIsTheKernelMinimum()
        {
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 5.0 }, Outputs)))
            {
                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, new[] { HookeJeevesParameter }, Objectives(), new GPSHookeJeevesAlgorithm()), TestWorkspace.StubExecutable, null, CancellationToken.None, out Exception exception);

                Assert.That(exception, Is.Null);
                Assert.That(run.Result.Outcome, Is.EqualTo(OptimisationOutcome.Success));
                Assert.That(run.Result.Minimum, Is.Not.Null);

                NativeGenOptReport report = new NativeGenOptReport(run, false);
                Assert.That(report.Successful, Is.True);
                Assert.That(report.BestPoint, Is.EqualTo(run.Result.Minimum.Coordinates));
                Assert.That(report.BestPoint, Is.EqualTo(new[] { 5.0 }));
                Assert.That(report.BestObjectives, Is.EqualTo(run.Result.Minimum.Outputs));
            }
        }

        [Test]
        public void EvaluationFailure_IsAnErrorCarryingTheEvaluatorMessage()
        {
            Dictionary<string, string> modes = new Dictionary<string, string> { ["2"] = "errorFile" };
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 4.9 }, Outputs, modes)))
            {
                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), new GoldenSectionAlgorithm()), TestWorkspace.StubExecutable, null, CancellationToken.None, out Exception exception);

                Assert.That(exception, Is.Null, "An evaluation failure is a kernel outcome, not an exception.");
                NativeGenOptReport report = new NativeGenOptReport(run, false);

                Assert.That(report.Outcome, Is.EqualTo(OptimisationOutcome.EvaluationFailed));
                Assert.That(report.Successful, Is.False);
                Assert.That(report.BestPoint, Is.Empty);
                Assert.That(report.BestObjectives, Is.Empty);
                List<string> errors = report.Log.Where(x => x.LogRecordType == LogRecordType.Error).Select(x => x.Text).ToList();
                Assert.That(errors, Has.Count.EqualTo(1));
                Assert.That(errors[0], Does.StartWith("Tas evaluation failed at simulation 2: "));
                Assert.That(errors[0], Does.Contain(run.Result.FailureMessage));
                Assert.That(errors[0], Does.Contain("injected script exception"));
            }
        }

        [Test]
        public void CancelBetweenEvaluations_StopsBeforeTheNextOne()
        {
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 4.9 }, Outputs)))
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                // The dialog's Cancel button does exactly this: cancel the token the run was given. Golden section
                // evaluates its first two points as one batch, so the first report follows two evaluations.
                List<string> atCancel = null;
                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), new GoldenSectionAlgorithm()), TestWorkspace.StubExecutable, x =>
                {
                    if (atCancel == null)
                    {
                        atCancel = Directory.GetDirectories(workspace.RunsDirectory).SelectMany(workspace.EvaluationFolders).ToList();
                        cancellationTokenSource.Cancel();
                    }
                }, cancellationTokenSource.Token, out Exception exception);

                Assert.That(exception, Is.Null);
                NativeGenOptReport report = new NativeGenOptReport(run, false);

                Assert.That(report.Outcome, Is.EqualTo(OptimisationOutcome.Cancelled));
                Assert.That(report.Successful, Is.False);
                Assert.That(report.BestPoint, Is.Empty);
                Assert.That(atCancel, Is.Not.Empty);
                Assert.That(workspace.EvaluationFolders(report.RunDirectory), Is.EqualTo(atCancel), "No evaluation started after the cancel.");
                Assert.That(report.Log.Where(x => x.LogRecordType == LogRecordType.Message).Select(x => x.Text), Has.Some.StartsWith("Optimisation cancelled by user"));
                Assert.That(report.Log.Any(x => x.LogRecordType == LogRecordType.Error), Is.False);
            }
        }

        [Test]
        public void CancelDuringAnEvaluation_LetsTasGenExecuteFinish()
        {
            Dictionary<string, int> sleep = new Dictionary<string, int> { ["1"] = 2500 };
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 4.9 }, Outputs, null, sleep)))
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                // Cancel from another thread while the first TasGenExecute is running, as a click on the dialog's own
                // thread would.
                Task cancel = Task.Run(async () =>
                {
                    DateTime timeout = DateTime.UtcNow.AddSeconds(30);
                    while (DateTime.UtcNow < timeout && !(Directory.Exists(workspace.RunsDirectory) && Directory.GetDirectories(workspace.RunsDirectory).Any(x => Directory.Exists(Path.Combine(x, "evaluations", "0001")))))
                    {
                        await Task.Delay(50);
                    }

                    await Task.Delay(500);
                    cancellationTokenSource.Cancel();
                });

                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), new GoldenSectionAlgorithm()), TestWorkspace.StubExecutable, null, cancellationTokenSource.Token, out Exception exception);
                cancel.Wait();

                Assert.That(exception, Is.Null);
                Assert.That(run.Result.Outcome, Is.EqualTo(OptimisationOutcome.Cancelled));
                Assert.That(workspace.EvaluationFolders(run.Workspace.RunDirectory), Is.EqualTo(new[] { "0001" }));
                Assert.That(File.Exists(Path.Combine(run.Workspace.EvaluationsDirectory, "0001", "stub-finished.txt")), Is.True, "The running evaluation was not killed.");
            }
        }

        [Test]
        public void MissingTasGenExecute_IsAnErrorAndNothingIsCreated()
        {
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 4.9 }, Outputs)))
            {
                string missing = Path.Combine(workspace.Directory, "missing", "TasGenExecute.exe");
                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, new[] { GoldenSectionParameter }, Objectives(), null), missing, null, CancellationToken.None, out Exception exception);

                Assert.That(run, Is.Null);
                Assert.That(exception, Is.InstanceOf<FileNotFoundException>());

                NativeGenOptReport report = new NativeGenOptReport(exception);
                Assert.That(report.Successful, Is.False);
                Assert.That(report.Outcome, Is.EqualTo(OptimisationOutcome.Undefined));
                Assert.That(report.Log.Single().LogRecordType, Is.EqualTo(LogRecordType.Error));
                Assert.That(report.Log.Single().Text, Does.Contain("TasGenExecute was not found.").And.Contain(missing));
                Assert.That(Directory.Exists(workspace.RunsDirectory), Is.False);
            }
        }

        private static IEnumerable<TestCaseData> Refusals()
        {
            yield return new TestCaseData(new ParametricAlgorithm(), new[] { GoldenSectionParameter }, typeof(NotSupportedException), "Not supported by the native route: ").SetName("Refused_Parametric");
            yield return new TestCaseData(new NelderMeadONeillcsAlgorithm(), new[] { GoldenSectionParameter }, typeof(NotSupportedException), "Not supported by the native route: ").SetName("Refused_NelderMead");
            yield return new TestCaseData(new MeshAlgorithm(), new[] { GoldenSectionParameter }, typeof(NotSupportedException), "Not supported by the native route: ").SetName("Refused_Mesh");
            yield return new TestCaseData(new GPSCoordinateSearchAlgorithm(), new[] { HookeJeevesParameter }, typeof(NotSupportedException), "Not supported by the native route: ").SetName("Refused_GPSCoordinateSearch");
            yield return new TestCaseData(new GPSHookeJeevesAlgorithm { MeshSizeDivider = 0 }, new[] { HookeJeevesParameter }, typeof(GenOptCompatibilityException), "Invalid GenOpt settings for the native route: ").SetName("Refused_InvalidMesh");
            yield return new TestCaseData(new GoldenSectionAlgorithm(), new[] { GoldenSectionParameter, new NumberParameter { Name = "Other", Initial = 0, Min = 0, Max = 1, Step = 1 } }, typeof(GenOptCompatibilityException), "Invalid GenOpt settings for the native route: ").SetName("Refused_GoldenSectionWithTwoParameters");
        }

        [TestCaseSource(nameof(Refusals))]
        public void Refusal_IsAnErrorAndNothingRuns(Algorithm algorithm, IParameter[] parameters, Type exceptionType, string prefix)
        {
            using (TestWorkspace workspace = new TestWorkspace(TestWorkspace.StubScript(new[] { 4.9 }, Outputs)))
            {
                NativeGenOptRun run = Modify.RunNative(Create.GenOptDocument(workspace.ScriptPath, parameters, Objectives(), algorithm), TestWorkspace.StubExecutable, null, CancellationToken.None, out Exception exception);

                Assert.That(run, Is.Null);
                Assert.That(exception, Is.InstanceOf(exceptionType));

                NativeGenOptReport report = new NativeGenOptReport(exception);
                Assert.That(report.Successful, Is.False);
                Assert.That(report.Log.Single().LogRecordType, Is.EqualTo(LogRecordType.Error));
                Assert.That(report.Log.Single().Text, Is.EqualTo(prefix + exception.Message), "The refusal is surfaced verbatim, never replaced by another algorithm.");
                Assert.That(Directory.Exists(workspace.RunsDirectory), Is.False, "Refused before any folder or process.");
            }
        }

        private static List<string> Bits(IEnumerable<OptimisationTraceEntry> entries)
        {
            return entries.Select(x => x.Simulation + "|" + x.MainIteration + "|" + x.SubIteration + "|" + x.Event + "|" +
                string.Join(",", x.Coordinates.Select(y => BitConverter.DoubleToInt64Bits(y))) + "|" +
                string.Join(",", x.Outputs.Select(y => BitConverter.DoubleToInt64Bits(y)))).ToList();
        }
    }
}
