# SAM Grasshopper icon redesign — SAM_Tas_Grasshopper PR record

Branch `feature/sam-gh-icon-redesign`, based on `sow/2026-Q3` @ `9ddf8ff`. PR: SAM-BIM/SAM_Tas_Grasshopper#8.
Propagates the SAM icon design system from SAM-BIM/SAM#166 (head `cf4d924a`, open, not merged) to this repository,
with an approved Tas-only refinement: a Tas application / file-type domain corner.

## Current status
All **104** Grasshopper objects in this repo (94 components + 10 params) use redesigned icons: **104 / 104**.
The Tas domain corner (T3D / TBD / TSD / TPD / TCD) is **approved and integrated**. The branch is built and validated,
ready for final review, and **not merged**.

## Work completed
- `design/grasshopper-icons/`: the shared SAM-BIM icon kit. `icons.py`, `render.py`, `sam_classify.py` and `ICON_DESIGN_SYSTEM.md` are vendored **verbatim** from SAM#166 (hash-checked). `icons_ext.py` and `ICON_DESIGN_SYSTEM_EXT.md` are the frozen SAM-BIM extension v1, identical in every SAM-BIM repo. `tools/repo_rules.py` holds this repo's classification decisions.
- `tools/tas_domain.py` (this repo only) adds the Tas domain corner (see below). Pipeline: `inventory.py` → `classify.py` → `build.py` → `tas_domain.py` → `integrate.py`.
- **Manifest** (source of truth): `manifest.json` / `manifest.csv`. Per object it records GUID, class, source, project, object glyph, operation, modifiers, `icon_id`, `resource`, `sam_icon_id` (the plain SAM base icon), `tas_domain`, `review_id` and `domain_note`.
- **Generation**: 76 canonical SVGs → 24×24 PNGs (65 with a Tas corner). Review output is in `review/tas/`: `palette.png`, `contact_{T3D,TBD,TSD,TPD,TCD,GEN}.png`, `master.png`, `before_after.png`, plus `review/contact_sheet.png` and `review/REVIEW.md`.
- **Integration**: each project's existing resx/Bitmap mechanism; only the icon token inside each `Icon` getter changes.

| Project | Objects | Icon resources | Mechanism |
|---|---|---|---|
| `SAM.Analytical.Grasshopper.Tas` | 67 | 54 | resx / Bitmap |
| `SAM.Analytical.Grasshopper.Tas.GenOpt` | 16 | 5 | resx / Bitmap |
| `SAM.Analytical.Grasshopper.Tas.TPD` | 14 | 11 | resx / Bitmap |
| `SAM.Core.Grasshopper.Tas` | 3 | 3 | resx / Bitmap |
| `SAM.Core.Grasshopper.Tas.UKBR` | 2 | 2 | resx / Bitmap |
| `SAM.Weather.Grasshopper.Tas` | 2 | 2 | resx / Bitmap |

## Tas domain corner (approved 2026-09-28)
- **Grammar**: the top-right folded file corner shows the Tas application/file. The bottom-right SAM badge shows the operation, unchanged. The object glyph and SAM-green subject are unchanged. Cross-domain/general objects (GEN) get no corner and keep the plain SAM icon.
- **Colours** (identity fills; EDSL's red frame `#E51A29` is not used, because red means Remove in SAM):

| Domain | Application | Colour | Source |
|---|---|---|---|
| T3D | 3D Modeller | `#F5DB53` | docs.edsl.net `tas3d.svg`; installed TAS3D.exe icon `#F5DB53` |
| TBD | Building Simulator | `#3F9F12` | docs.edsl.net `tbd.svg`; TBD.exe icon `#40A013` |
| TSD | Results Viewer | `#F8744A` | docs.edsl.net `tsd.svg`; TSD.exe icon `#FA764B` |
| TPD | Systems | `#1A72C0` | docs.edsl.net `tpd.svg`; TPD.exe icon `#1B73C1` |
| TCD | Construction Database | `#AD5100` | installed TCD.exe icon (docs.edsl.net has no TCD asset: 404) |

- **Classification** (`tools/tas_domain.py`, `DOMAIN` by class). Rule, approved: classify by the primary Tas file/object the component produces or modifies. Result: **T3D 4 · TBD 29 · TSD 19 · TPD 15 · TCD 14 · GEN 23 = 104**.
  - **TCD** holds `CreateTCD`, the construction / U-value / glazing / layer-thickness calculators (which run on `TCD.Document` in SAM_Tas' `ThermalTransmittanceCalculator`), and their calculation data/result params.
  - **GEN** holds GenOpt (16), UKBR (3), the two end-to-end workflows (T3D + TBD + TSD), `CreateWeatherData` (TWD/TBD/TSD) and `Wait`.
  - **Approved ambiguous cases**, annotated on the sheets:
    - `CopyCalendar` (TCR → TBD) → TBD
    - `T3DtoTBD` → TBD
    - `Tas.Simulate` → TBD
    - `SizingType` → TBD
    - `CreateSurfaceOutputSpec` and its param → TBD
    - `CreateDesignDays` → TBD
    - `SurfaceSimulationResults` → TSD
    - `CalculateResultantTemperatureFromTPD` → TPD
- **Review ids**: `TAS-<domain>-NNN`, one per object, in the manifest and on every sheet.
- The TSD and TPD TM59 queries were identical before and now differ by their corner.

## Design reuse
- **Reused SAM object families (30)**: `airflow`, `aperture`, `apertureConstruction`, `calendar`, `clock`, `cluster`, `construction`, `constructionLayer`, `designDay`, `fan`, `file`, `geometry`, `heat`, `izam`, `log`, `model`, `panel`, `profile`, `result`, `settings`, `shade`, `space`, `system`, `thermometer`, `type`, `value`, `weather`, `weatherData`, `weatherYear`, `zone`.
- **SAM-BIM ext v1 families used (4)**: `algorithm`, `element`, `energyCentre`, `objective`. The only ext verb used is `run`.

## Decisions and assumptions
- Grammar, palette, badge families and construction rules are unchanged (SAM#166). The Tas corner is a Tas-repo-only secondary accent. The shared kit files are untouched; the corner lives in `tools/tas_domain.py`.
- Tas files are drawn by the SAM object read or written:
  - TBD = `model`
  - TSD = `result`
  - TPD = `system` / `energyCentre`
  - T3D = `geometry`
  
  Direction: import = Tas → SAM, export = SAM → Tas, convert = Tas → Tas. The corner adds which Tas application is involved.
- Qualifier variants (`…By<X>`) share an icon intentionally. `review/REVIEW.md` lists the 16 shared icons.
- Legacy icon resources are kept (still referenced by context menus / AssemblyInfo). There is no GUID, name, nickname, category, subcategory, parameter or behaviour change.

## Files changed
- New: `design/grasshopper-icons/**`, `<project>/Resources/Icons/SAM_GH_*.png`, `docs/GH-IconRedesign.md`.
- Modified: 104 component/param `.cs` files (one icon token each; 57 of them also received the SPDX policy header), 6× `Resources.resx`, 6× `Resources.Designer.cs` (5 also received the SPDX header). No csproj change.

## Validation (final, after the Tas corner integration)
| Check | Result |
|---|---|
| `tools/classify.py` | 104 classified, 0 unclassified |
| `tools/tas_domain.py` | 104/104 objects have a Tas domain; 76 icons (65 with a corner); identical-pixel groups 0; idempotent (second run identical) |
| `tools/integrate.py` re-parse | 104/104 objects reference their `SAM_GH_*` resource; every PNG exists |
| `tools/check_source.py` (vs merge base) | vendored files OK; icon-token swaps 104; SPDX headers added 57; non-icon changes 0; ComponentGuid 105 → 105 UNCHANGED |
| CI `spdx-check` simulated locally (same test, two-dot diff vs base tip) | 111 checked `.cs` files, 0 missing header |
| `SAM_Tas_Grasshopper.sln` with Visual Studio MSBuild, Debug, serial | Build succeeded, 0 errors (VS MSBuild required: COM references) |
| `tools/check_assemblies.py build` | all 6 assemblies embed every required 24×24 icon → OK; all 6 installed to `%APPDATA%\SAM` |
| `tests/GhIconTest` (real Rhino 8 / Grasshopper, Rhino.Testing) | **104/104** objects load by GUID; name/category match; icon = manifest PNG (max diff 1 level, premultiplied-alpha rounding) |
| Visual review (`review/tas/*`, 24 px on normal / warning / dark bodies) | all icons legible; corner readable on all bodies (TSD coral on the orange warning body and T3D yellow on the light body rely on the ink hairline) |
| Repository test projects | none in this repository |

## SPDX header policy (CI `spdx-check`)
The repository SPDX check requires the LGPL-3.0-or-later SPDX line and the copyright line in every `.cs` file a PR changes. The icon-token swaps touched 62 older files that predated the policy (components and `Resources.Designer.cs`), and the kit test `IconTests.cs` had no header. The standard 2-line header was added to them; nothing else changed. `tools/check_source.py` accepts exactly this header as the only non-icon addition.

## Unresolved issues / risks
- A parallel VS MSBuild (`-m`) run can fail the Weather project's post-build `xcopy` of `files/Grasshopper/UserObjects` (exit 4, file contention). A serial build is clean. This post-build step is pre-existing and not changed by this PR.
- The param class `GooLayerThicknessCalculationResultParam` is named `LayerThicknessCalculationData` in source (pre-existing naming). It is classified by class.
- Built against sibling repos as checked out locally (SAM on `feature/sam-gh-icon-redesign` = SAM#166). The icon changes are API-neutral.

## Recommended next step
Final review of this PR (`review/tas/master.png`, `review/tas/palette.png`), then merge by the maintainer. After merge, add the `PROJECT_PROGRESS.md` closeout entry on `sow/2026-Q3` with the merge SHA. SAM#166 (the reference design system) stays open.
