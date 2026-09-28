# SAM Grasshopper icon redesign — SAM_Tas_Grasshopper PR record

Branch `feature/sam-gh-icon-redesign`, based on `sow/2026-Q3` @ `9ddf8ff`. PR: SAM-BIM/SAM_Tas_Grasshopper#8.
Propagates the SAM icon design system from SAM-BIM/SAM#166 (head `cf4d924a`, open, not merged) to this repository.

## Current status
All **104** Grasshopper objects in this repo (94 components + 10 params) use redesigned icons: **104 / 104**.
Built and validated; ready for review. **Not merged.**

## Work completed
- `design/grasshopper-icons/`: the shared SAM-BIM icon kit. `icons.py`, `render.py`, `sam_classify.py` and `ICON_DESIGN_SYSTEM.md` are vendored **verbatim** from SAM#166 (hash-checked). `icons_ext.py` and `ICON_DESIGN_SYSTEM_EXT.md` are the frozen SAM-BIM extension v1 (identical in every SAM-BIM repo). `tools/repo_rules.py` holds this repo's explicit decisions.
- **Inventory**: `tools/inventory.py` parses C# source (every non-abstract class declaring `ComponentGuid`).
- **Manifest** (source of truth): `manifest.json` / `manifest.csv` — per object: GUID, class, source, project, object glyph, operation, modifiers, icon id, resource, glyph/badge origin.
- **Generation**: 75 canonical SVGs → 24×24 PNGs; review sheet `review/contact_sheet.png` (native 24 px on GH normal / orange-warning / dark bodies + 3×) and `review/REVIEW.md`.
- **Integration**: each project's existing mechanism; only the icon token inside each `Icon` getter changes.

| Project | Objects | Icon resources | Mechanism |
|---|---|---|---|
| `SAM.Analytical.Grasshopper.Tas` | 67 | 54 | resx / Bitmap |
| `SAM.Analytical.Grasshopper.Tas.GenOpt` | 16 | 5 | resx / Bitmap |
| `SAM.Analytical.Grasshopper.Tas.TPD` | 14 | 11 | resx / Bitmap |
| `SAM.Core.Grasshopper.Tas` | 3 | 3 | resx / Bitmap |
| `SAM.Core.Grasshopper.Tas.UKBR` | 2 | 2 | resx / Bitmap |
| `SAM.Weather.Grasshopper.Tas` | 2 | 2 | resx / Bitmap |

## Design reuse
- **Reused SAM object families (30)**: `airflow`, `aperture`, `apertureConstruction`, `calendar`, `clock`, `cluster`, `construction`, `constructionLayer`, `designDay`, `fan`, `file`, `geometry`, `heat`, `izam`, `log`, `model`, `panel`, `profile`, `result`, `settings`, `shade`, `space`, `system`, `thermometer`, `type`, `value`, `weather`, `weatherData`, `weatherYear`, `zone`
- **New SAM-BIM ext v1 families used (4)**: `algorithm`, `element`, `energyCentre`, `objective`
- **Verbs**: `add`, `calculate`, `convert`, `copy`, `create`, `export`, `get`, `import`, `modify`, `remove`, `run`, `set`, `update`, `value`; new ext verb: `run`
- Distinct icons: **75** (67 on SAM glyphs, 8 on ext glyphs). Icon ids shared with SAM render pixel-identically to SAM's.

## Decisions and assumptions
- Grammar, palette, badge families and construction rules are unchanged (SAM#166). No text, no new colours.
- Qualifier variants (`…By<X>`) share an icon intentionally (see `review/REVIEW.md`).
- Interop direction: external → SAM = import ↓, SAM → external = export ↑.
- Tas files are drawn by the SAM object read/written: TBD = `model`, TSD = `result`, TPD = `system`/`energyCentre`, T3D = `geometry`; import = Tas → SAM, export = SAM → Tas, convert = Tas → Tas. `file` is used only when the file itself is the subject (create TBD by TM59/Part L, simulate a TBD, UKBR file).
- Simulation/workflow components use the ext verb `run` (Evaluate family): `Tas.Simulate` = file ▶, workflows = model ▶, TPD simulate = system ▶, sizing = heat ▶, GenOpt = algorithm ▶.
- The 12 GenOpt algorithm components share `algorithm_create` (the algorithm name is the qualifier); `Objective` uses ext `objective`.
- U-value calculations use SAM `heat` (heat transfer coefficient); TM52/TM59 queries use SAM `thermometer` (SAM's TM52 convention).
- Legacy icon resources are kept (still referenced by context menus / AssemblyInfo); no GUID, name, nickname, category, subcategory, parameter or behaviour change.

## Files changed
- New: `design/grasshopper-icons/**`, `<project>/Resources/Icons/SAM_GH_*.png`, `docs/GH-IconRedesign.md`.
- Modified: 104 component/param `.cs` files (one icon token each), 6× `Resources.resx`, 6× `Resources.Designer.cs`. No csproj change.

## Validation
| Check | Result |
|---|---|
| `tools/classify.py` | 104 classified, 0 unclassified |
| `tools/build.py` identical-pixel collision check | 0 groups (75 distinct icons; 16 intentionally shared icon(s) for qualifier variants, listed in `review/REVIEW.md`) |
| Icon ids shared with SAM#166 vs SAM's `png/24` | 26 shared, 26 byte-identical |
| `tools/integrate.py` re-parse | 104/104 objects reference their `SAM_GH_*` resource; every PNG exists |
| `tools/check_source.py` vs `origin/sow/2026-Q3` | vendored files OK; icon-token swaps: 104, non-icon changes: 0; base 105, now 105 -> UNCHANGED |
| `dotnet build SAM_Tas_Grasshopper.sln (Visual Studio MSBuild) -c Debug` | Build succeeded, 0 errors (VS MSBuild required: COM references; `dotnet build` cannot resolve them) |
| `tools/check_assemblies.py` | every assembly embeds every required 24×24 icon → OK |
| `tests/GhIconTest` (real Rhino 8 / Grasshopper, Rhino.Testing) | 104/104 objects load by GUID, name/category match, icon = manifest PNG (max diff 1 level, premultiplied-alpha rounding) |
| Repository test projects | none in this repository |
| Visual review (`review/contact_sheet.png`, 24 px on normal / warning / dark bodies) | all icons legible; no collisions |

## Unresolved issues / risks
- A parallel VS MSBuild (`-m`) run can fail the Weather project's post-build `xcopy` of `files/Grasshopper/UserObjects` (exit 4, file contention); a serial build is clean. Pre-existing post-build step, not changed by this PR.
- The param class `GooLayerThicknessCalculationResultParam` is named `LayerThicknessCalculationData` in source (pre-existing naming); it is classified by class (`result`).
- Built against sibling repos as checked out locally (SAM on `feature/sam-gh-icon-redesign` = SAM#166); icon changes are API-neutral.

## Recommended next step
Review this PR (compare `review/contact_sheet.png`), then merge by the maintainer. After merge, add the `PROJECT_PROGRESS.md` closeout entry on `sow/2026-Q3` with the merge SHA. SAM#166 (the reference design system) remains open.
