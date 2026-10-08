# Native GenOpt in Grasshopper - PR6 record (consumer side)

Branch `feature/native-optimisation-pr6-retire-legacy`, base `sow/2026-Q4` @ `a01f6705`. Part of the Java-free GenOpt
PR6 set; the full record (classified inventory, decisions, deployment follow-up) is SAM_Tas
`SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_GENOPT_PR6.md`. **Depends on the SAM_Tas PR6 PR (merge it first).** CI
clones SAM_Tas at this branch name until then. `PROJECT_PROGRESS.md` is not changed here.

## Status

Code, tests and evidence complete; PR open for owner review. Not merged.

## What changed

- `NativeGenOptReport` reads its rules from SAM_Tas `NativeGenOptOutcome` (which runs succeed, withholding after a
  cancel, best point, interval, refusal wording). Its own `Best` and refusal mapping are removed. Log lines, their order
  and their record types are unchanged.
- `Modify.RunNative`'s progress text keeps the running lowest with `NativeGenOptOutcome.IsLower` (same rule; prints the
  same `NaN` before the first number).
- Tests: GPSCoordinateSearch is now refused as "Not supported by the native route: GenOpt algorithm
  'GPSCoordinateSearch' ..." (SAM_Tas PR6 B4; was "Invalid GenOpt settings"); the NaN best-point test asserts the
  report's best point equals `NativeGenOptOutcome.Best`; `NoJavaRouteTests.UsesTheSharedResultRules` pins the use of
  `NativeGenOptOutcome` (`.ctor`, `IsLower`, `RefusalMessage`) in the built plugin.
- Not changed: the component, its parameters, outputs and GUID, `RunNative` call, cancellation, the unsupported
  algorithm components (they remain and are refused at run).

## Files changed

`Classes/NativeGenOptReport.cs`, `Modify/RunNative.cs`, this record; tests `NativeGenOptReportTests.cs`,
`NativeBoundaryTests.cs`, `NoJavaRouteTests.cs`.

## Validation (head `f4094e3`, against the PR6 SAM_Tas build `31ae7ef`)

- Release MSBuild (VS 18) of `SAM.Analytical.Grasshopper.Tas.GenOpt` and its test project, `APPDATA`/`USERPROFILE`
  redirected to scratch, real `NUGET_PACKAGES`: 0 errors. A full `SAM_Tas_Grasshopper.sln` Rebuild under the scratch
  profile fails only in the unrelated `SAM.Core.Grasshopper.Tas.UKBR` post-build `xcopy` to the scratch Grasshopper
  UserObjects folder (environmental; the GenOpt plugin itself built).
- `dotnet test Tests/SAM.Analytical.Grasshopper.Tas.GenOpt.Tests -c Release` with `SAM_GENOPT_TESTS_NO_RHINO=1`:
  **49 passed, 4 skipped** (the Rhino-hosted `ComponentContractTests`, also skipped in CI). Running those 4 with Rhino
  hung in the unattended session (Rhino initialisation, with the scratch and with the real profile; killed after the
  timeout; the real `%APPDATA%\SAM` was checked unchanged). They pin GUID/inputs/outputs, which this PR does not touch.
- First run found one wrong test expectation of mine (prefix of the GPSCoordinateSearch refusal), fixed before commit.
- PR CI `build` and `spdx`: green on `f4094e3`. `git diff --check` clean.
- No Rhino or licensed acceptance rerun: wording and native execution unchanged; PR4 acceptance stands.

## Risks

- Grasshopper users now see "Not supported by the native route" for GPSCoordinateSearch (was "Invalid GenOpt settings").
- Needs SAM_Tas PR6 merged first.

## Next step

Owner review; merge after SAM_Tas PR6; then the post-merge `PROJECT_PROGRESS.md` closeout.
