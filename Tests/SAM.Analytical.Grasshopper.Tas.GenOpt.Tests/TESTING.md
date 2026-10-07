# SAM.Analytical.Grasshopper.Tas.GenOpt.Tests

Tests for the Grasshopper side of the native GenOpt route (PR4) only. The SAM.Math kernel (PR2) and the SAM_Tas
adapter/evaluator (PR3) have their own suites and are not re-tested here.

## What is covered

| File | Needs | Covers |
|---|---|---|
| `NativeBoundaryTests` | PR3's `StubTasGenExecute` (built through the ProjectReference) | Component inputs → `Create.GenOptDocument` (unchanged mapping); `Modify.RunNative` → `GenOptDocument.RunNative` gives exactly the direct PR3 result (bit for bit); progress is the kernel's, on the calling thread; best point (golden section: lowest entry; Hooke-Jeeves: kernel minimum); evaluation failure → error with the evaluator's message; cancel between evaluations and during one (the running TasGenExecute finishes, nothing starts after); missing TasGenExecute; refused algorithms/settings (verbatim error, no substitution, no folder created). |
| `NativeGenOptReportTests` | nothing (real SAM.Math results from a delegate evaluator) | Outcome → `successful`/messages for every outcome; cancel observed after the run withholds the result; NaN objectives; exception messages; texts with braces. |
| `NoJavaRouteTests` | nothing (reads the built plugin's metadata) | The plugin calls `GenOptDocument.RunNative` and never `Run()`, `ExecutableFile`, `Command`, `TasGenOptJavaPath`, `SetProjectDirectory`; uses no `Process`, `FileSystemWatcher` or registry; holds no java/jar/bat/cmd/OutputListing/GenOpt-file string. |
| `ComponentContractTests` | **Rhino 8 installed** | GUID, name, category, version 1.0.2; the 5 inputs unchanged; `successful` first with the 5 new outputs appended; a component with only the old output can add the new ones but no input. |

`ComponentContractTests` are **ignored** where Rhino 8 is not installed (CI): the NuGet RhinoCommon is a reference
assembly, so components cannot be created. Set `SAM_GENOPT_TESTS_NO_RHINO=1` to reproduce that locally.

Reading saved components is not tested headlessly: outside Rhino, Grasshopper's component server cannot initialise
(`rhcommon_c`), shows a modal "Grasshopper Loading Error" and reads parameters back as `Param_GenericObject`. Opening
definitions saved by the previous plugin is covered by the real Rhino/Grasshopper acceptance (see
`Grasshopper/SAM.Analytical.Grasshopper.Tas.GenOpt/NATIVE_GENOPT_GRASSHOPPER.md`).

## Running

Build first: SAM (`build\SAM.Math.dll`, `SAM.Core.dll`, `SAM.Core.Grasshopper.dll`), SAM_Windows, SAM_Tas
(`SAM.Analytical.Tas.GenOpt`) and `SAM_Tas_Grasshopper.sln` (the tests reference `build\` outputs, as the plugin
projects do). Then:

```
dotnet test Tests/SAM.Analytical.Grasshopper.Tas.GenOpt.Tests/SAM.Analytical.Grasshopper.Tas.GenOpt.Tests.csproj -c Release
```

The project is not part of `SAM_Tas_Grasshopper.sln`, so the SAM_Deploy BuildAll is unaffected; CI runs it as a
separate step.
