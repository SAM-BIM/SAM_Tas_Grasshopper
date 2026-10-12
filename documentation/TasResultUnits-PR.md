# Tas consumption/peak units, option A of SAM_Tas#92: `Tas.AddBuildingResults` (SAM_Tas_Grasshopper)

Branch `fix/tas-result-units-kwh` → base `sow/2026-Q4` (cut from `93108f0`). Record date: 2026-10-12. Part of a coordinated
set of four PRs; the design, compatibility table and merge order are recorded in SAM's
`documentation/TasResultUnits-PR.md` (record of record). This PR merges **after SAM and before SAM_Tas**.

## Current status

PR open, **not merged**. Depends on the SAM PR (`fix/tas-result-units-kwh`); its CI builds SAM and SAM_Tas from their
same-named branches until those merge. Awaiting final-head CI and **owner review**.

## Work completed

- `Classes/BuildingResults.cs` (new): the component's outputs computed from an `AnalyticalModelSimulationResult`, with no
  Tas, COM or Grasshopper type. Consumption and peaks come from SAM's `Query.ConsumptionOrPeakLoad`, which returns kWh/kW
  whichever units the result was written in, so the component's own `/ 1000` is gone. Per area/volume arithmetic is
  unchanged: kWh/m2, kWh/m3, and peak **W/m2, W/m3** (kW ÷ area × 1000), each rounded to 0.01 as before.
- `Component/TasTSDAddBuildingResults.cs`: uses `BuildingResults`; warns and outputs NaN when the result's units are not
  known (impossible from SAM_Tas's converter today). Output descriptions of the four peak-per-area/volume outputs corrected
  from `[kWh/m2]`/`[kWh/m3]` to **`[W/m2]`/`[W/m3]`** (their values were always W/m2, W/m3). Output names, nicknames,
  GUID and `LatestComponentVersion` unchanged (the values are unchanged, so no component update is forced on users).
- `Tests/SAM.Analytical.Grasshopper.Tas.Tests` (new project, not in the solution; same pattern as the GenOpt tests) and a
  CI step `Run Tas Grasshopper offline tests` in `.github/workflows/build.yml`.

## Compatibility

The component calls SAM_Tas's converter on the fly, so it receives an unmarked Wh/W result until SAM_Tas's PR merges and a
marked kWh/kW result afterwards; both give bit-identical outputs (tested). Merging this before SAM_Tas is therefore safe;
merging SAM_Tas first is not (the old component would divide the new kWh by 1000 again).

## Owner decisions

As in SAM's record. Specific here: description fix only (no `LatestComponentVersion` bump); unknown units → NaN plus a
runtime warning.

## Files changed

- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Classes/BuildingResults.cs` (new).
- `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/TasTSDAddBuildingResults.cs`.
- `Tests/SAM.Analytical.Grasshopper.Tas.Tests/SAM.Analytical.Grasshopper.Tas.Tests.csproj`, `BuildingResultsTests.cs` (new).
- `.github/workflows/build.yml` (one test step).
- `documentation/TasResultUnits-PR.md` (this record).

## Validation

- `SAM_Tas_Grasshopper.sln` Release (VS 18 MSBuild) on the SAM and SAM_Tas branch builds: 0 errors. The test bin's
  `SAM.Analytical.Grasshopper.Tas.dll` equals `build\` (SHA-256 `ff516a77…`); `SAM.Analytical.dll` `6e5658aa…`.
- New tests **8/8**: legacy (Wh/W) and marked (kWh/kW) results give outputs **bit-identical** to each other and to the
  pre-correction component arithmetic, on the recorded #92 numbers (15 227.84… kWh, 38.727… kW; peak per area
  31.37 W/m2) and on a 2 000-case seeded sweep including zero area/volume; OpenStudio results with and without the
  marker; unknown units → NaN, peak hours kept; null refused.
- Existing `SAM.Analytical.Grasshopper.Tas.GenOpt.Tests` **53/53**.
- Mutation: 3/3 killed (divides again; peak per area without × 1000; raw parameter read).
- PR CI (`build`, `spdx`): see the PR.

## Unresolved issues and risks

- Grasshopper definitions keep their saved output descriptions until the component is refreshed; only the text differs.

## Next step

Owner review. After SAM merges: rerun this PR's CI against merged Q4, merge, then SAM_Tas; closeout on `sow/2026-Q4`
(docs-only, `[skip ci]`).
