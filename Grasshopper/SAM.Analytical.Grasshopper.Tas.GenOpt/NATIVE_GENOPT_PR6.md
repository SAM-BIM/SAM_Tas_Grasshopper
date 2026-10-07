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

## Validation, risks, next step

In the PR description (final numbers). No licensed or Rhino acceptance rerun: report wording and native execution are
unchanged; PR4 acceptance stands. Next: owner review, merge after SAM_Tas PR6, then the post-merge closeout.
