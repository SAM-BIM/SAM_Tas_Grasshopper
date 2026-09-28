"""SAM_Tas_Grasshopper only: EDSL Tas application / file-type domain accent (approved 2026-09-28).

Adds a restrained secondary accent to the SAM icon grammar: a folded file corner in the top-right of the 24x24 icon,
coloured by the native EDSL Tas application whose file/object the component primarily produces or modifies.
The SAM grammar is untouched: object glyph + SAM-green subject + operation badge (bottom-right, SAM colours).

    top-right corner  = which Tas application/file (T3D / TBD / TSD / TPD / TCD)          <- this repo only
    bottom-right disc = what the component does (Create / Get / Modify / Import / Export ...)   <- SAM, unchanged
    no corner         = genuinely cross-domain / general (GEN): pixel-identical to the SAM icon

Colours: official EDSL fills of https://docs.edsl.net/_media/{tas3d,tbd,tsd,tpd}.svg (inspected 2026-09-28),
cross-checked against the icons embedded in the installed TAS3D/TBD/TSD/TPD.exe (within 1-2 levels). docs.edsl.net
publishes no TCD asset (404): TCD's colour is the dominant colour of the icon embedded in the installed TCD.exe.
EDSL's shared red frame (#E51A29) is not used: in the SAM grammar red means Remove.

Pipeline in this repo:  inventory.py -> classify.py -> build.py -> tas_domain.py -> integrate.py
tas_domain.py makes the accented icons canonical (idempotent): svg/ + png/24 hold them; manifest icon_id / resource /
icon_file point at them; sam_icon_id keeps the SAM base id; tas_domain / review_id / domain_note are added.
Review output: review/tas/ (palette, one sheet per domain, master, before/after), review/contact_sheet.png, REVIEW.md.
"""
import collections
import csv
import hashlib
import html
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build  # noqa: E402  (sheet renderer shared with the repo review sheets)
import icons  # noqa: E402
import icons_ext  # noqa: E402,F401
import render  # noqa: E402
import sam_classify as C  # noqa: E402

DESIGN = os.path.dirname(HERE)
BASE = os.path.join(DESIGN, "tas", "sam_base")  # plain SAM renders of accented ids, for before/after only
REVIEW = os.path.join(DESIGN, "review", "tas")

# ------------------------------------------------------------------ EDSL Tas identities
TAS = collections.OrderedDict([
    ("T3D", dict(colour="#F5DB53", app="3D Modeller", file=".t3d", exe="#F5DB53")),
    ("TBD", dict(colour="#3F9F12", app="Building Simulator", file=".tbd", exe="#40A013")),
    ("TSD", dict(colour="#F8744A", app="Results Viewer", file=".tsd", exe="#FA764B")),
    ("TPD", dict(colour="#1A72C0", app="Systems", file=".tpd", exe="#1B73C1")),
    # docs.edsl.net publishes no TCD asset (404); TCD = dominant colour of the icon embedded in the installed TCD.exe
    ("TCD", dict(colour="#AD5100", app="Construction Database", file=".tcd", exe="#AD5100")),
])
GEN = "GEN"  # genuinely cross-domain / general: no accent (icon identical to the SAM one)

# ------------------------------------------------------------------ domain of every Tas GH object (by class)
# Rule (approved): classify by the primary Tas file/object the component produces or modifies.
# Evidence per class: Tas document types referenced in the component source, the original icon the authors
# chose (SAM_TasT3D / SAM_TasTBD3 / SAM_TasTSD3 / SAM_TasTPD3), and the SAM_Tas library call. AMBIGUOUS = explained.
DOMAIN = {
    # T3D - 3D Modeller
    "TasOpenT3DDocument": "T3D", "TasUpdateT3D": "T3D", "GooSAMT3DDocumentParam": "T3D",
    "gbXMLTasT3D": "T3D",  # gbXML -> T3D import (writes a T3D)
    # TBD - Building Simulator
    "SAMAnalyticalTBD": "TBD", "SAMAnalyticalSAP": "TBD", "SAMAnalyticalTM59": "TBD", "SAMAnalyticalFromTBD": "TBD",
    "SAMAnalyticalCreateTBDByTM59": "TBD", "TasCreateTBDByPartL": "TBD", "TasUpdateIZAMs": "TBD",
    "TasUpdateIZAMsBySpaceParameter": "TBD", "TasAssignAdiabaticConstruction": "TBD",
    "TasAssignRooflightBuildingElementType": "TBD", "TasRemoveSchedules": "TBD", "TasUpdateApertureControl": "TBD",
    "TasUpdateBuildingElements": "TBD", "TasUpdateConstructions": "TBD", "TasUpdateDesignDays": "TBD",
    "TasUpdateDesignLoads": "TBD", "TasUpdateFacingExternal": "TBD", "TasUpdateShading": "TBD",
    "TasUpdateWeatherData": "TBD", "TasUpdateZones": "TBD", "TasUpdateAdaptiveSetpointACCI": "TBD",
    "TasSizing": "TBD",
    "TasCopyCalendar": "TBD", "TasT3DtoTBD": "TBD", "TasSimulate": "TBD", "SAMAnalyticalSizingType": "TBD",
    "TasCreateSurfaceOutputSpec": "TBD", "GooSurfaceOutputSpecParam": "TBD", "TasCreateDesignDays": "TBD",
    # TSD - Results Viewer
    "SAMAnalyticalFromTSD": "TSD", "TasTSDAddResults": "TSD", "TasTSDAddBuildingResults": "TSD",
    "TasTSDCreateAdjacencyCluster": "TSD", "TasTSDCreateSpaceSimulationResults": "TSD",
    "TasTSDQueryResultsByHourOfYear": "TSD", "TasTSDQueryResultsByPercentage": "TSD",
    "TasTSDQueryResultsByTotalPercentage": "TSD", "TasTSDQueryTM52Results": "TSD", "TasTSDQueryTM59Results": "TSD",
    "TasTSDQueryZoneResultsByHourOfYear": "TSD", "TasTSDQueryZoneResultsByPercentage": "TSD",
    "TasDesignDayNames": "TSD", "TasLogPartODiagnostics": "TSD", "TasTSDDailyIndoorComfortTemperatures": "TSD",
    "SAMAnalyticalTasPanelDataType": "TSD", "SAMAnalyticalTasSpaceDataType": "TSD", "TasWeatherYear": "TSD",
    "SAMAnalyticalSurfaceSimulationResults": "TSD",
    # TPD - Systems
    "CreateTPDByAnalyticalModel": "TPD", "SAMSystemsCreateTPDBySystemEnergyCentre": "TPD",
    "SAMSystemsCreateTPDByTSDAndSystemEnergyCentre": "TPD", "SAMSystemsFromTPD": "TPD", "SAMSystemsResultPeriod": "TPD",
    "TasCalculateResultantTemperatureFromTPD": "TPD",
    "TasSystemResults": "TPD", "TasSystemSpaceResults": "TPD", "TasTPDQueryTM59Results": "TPD",
    "SAMSystemsTASTPDAssignZone": "TPD", "SAMSystemsTASTPDModifyTPDByAirflows": "TPD",
    "SAMSystemsTASTPDModifyTPDByAirflowsBySpaces": "TPD", "SAMSystemsTASTPDModifyTPDFanByAirflows": "TPD",
    "SAMSystemsTASTPDSimulate": "TPD", "TasCreateTPDBySystemTypeName": "TPD",
    # TCD - Construction Database: CreateTCD and the construction / U-value / glazing / layer-thickness calculators
    # (SAM_Tas ThermalTransmittanceCalculator runs on TCD.Document) and their data/result params
    "TasCreateTCD": "TCD",
    "SAMAnalyticalCalculateConstructions": "TCD", "SAMAnalyticalCalculateGlazing": "TCD",
    "SAMAnalyticalCalculateThicknesses": "TCD", "SAMAnalyticalCalculateThermalTransmittance": "TCD",
    "SAMAnalyticalCalculateThermalTransmittanceByAperture": "TCD",
    "TasCreateApertureConstructionCalculationData": "TCD", "TasCreateConstructionCalculationData": "TCD",
    "TasCreateLayerThicknessCalculationData": "TCD", "TasCreateLayerThicknessCalculationDataByConstruction": "TCD",
    "GooThermalTransmittanceCalculationDataParam": "TCD", "GooThermalTransmittanceCalculationResultParam": "TCD",
    "GooLayerThicknessCalculationDataParam": "TCD", "GooLayerThicknessCalculationResultParam": "TCD",
    # GEN - genuinely cross-domain / general (no corner)
    "SAMAnalyticalWorkflowTBD": GEN, "SAMAnalyticalWorkflowgbXML": GEN,  # T3D + TBD + TSD end-to-end
    "TasCreateWeatherData": GEN,                                          # TWD, TBD or TSD
    "SAMAnalyticalWait": GEN,
    "TasOpenUKBRFile": GEN, "GooUKBRFileParam": GEN, "TasUpdateUKBRFile": GEN,  # UKBR (compliance), not a core app
    "SAMAnalyticalGenOpt": GEN, "GooAlgorithmParam": GEN, "GooObjectiveParam": GEN, "GooParameterParam": GEN,  # GenOpt
}
GENOPT_ALGORITHMS = ("DiscreteArmijoGradient", "FibonacciDivision", "GPSHookeJeeves", "GenOptGoldenSection",
                     "HybridGeneralizedPSPO", "Mesh", "MultiStartGPS", "NelderMeadONeillcs", "Parametric",
                     "ParticleStormConstriction", "ParticleStormIntertia", "ParticleStormMesh")
for _a in GENOPT_ALGORITHMS:
    DOMAIN[f"SAMAnalytical{_a}Algorithm"] = GEN

AMBIGUOUS = {  # approved classifications, kept visible on the review sheets
    "TasCopyCalendar": "TCR -> TBD: writes the TBD calendar, so TBD.",
    "TasT3DtoTBD": "T3D -> TBD: produces the TBD, so TBD.",
    "TasSimulate": "Runs the Building Simulator on a TBD (writes a TSD): kept TBD (approved).",
    "SAMAnalyticalSizingType": "Option of the TBD simulation / sizing (old icon was TSD): TBD.",
    "TasCreateSurfaceOutputSpec": "Output spec of a TBD simulation: TBD.",
    "GooSurfaceOutputSpecParam": "Param of the TBD simulation output spec: TBD.",
    "TasCreateDesignDays": "Reads design days from a TBD or a TSD; design days are defined in the TBD: TBD.",
    "SAMAnalyticalSurfaceSimulationResults": "Surface results on a SAM model originate from a TSD: TSD.",
    "TasCalculateResultantTemperatureFromTPD": "TPD project; re-simulates TBD/TSD from a TPD run: TPD.",
}


# ------------------------------------------------------------------ accent
def accent(domain):
    """Folded file corner, top-right: white separator + EDSL colour + one ink hairline (clear of the badge)."""
    c = TAS[domain]["colour"]
    return ('<path d="M15.6 0L24 8.4" stroke="#FFFFFF" stroke-width="1.6" fill="none"/>'
            f'<polygon points="16.4,0 24,0 24,7.6" fill="{c}"/>'
            '<path d="M16.4 0L24 7.6" stroke="#111111" stroke-width="0.9" fill="none" stroke-linecap="round"/>')


def tas_svg(r, domain, iid):
    svg = icons.icon_svg(r["object"], r["op"], plural=r["plural"], container=r["container"], comment=f"SAM GH icon {iid}")
    return svg if domain == GEN else svg.replace("</svg>", accent(domain) + "</svg>")


def final_id(base, domain):
    return base if domain == GEN else f"{base}_{domain.lower()}"


def mirror(folder, ext, keep):
    os.makedirs(folder, exist_ok=True)
    for fn in os.listdir(folder):
        if fn.endswith(ext) and fn[: -len(ext)] not in keep:
            os.remove(os.path.join(folder, fn))


def main():
    mpath = os.path.join(DESIGN, "manifest.json")
    rows = json.load(open(mpath, encoding="utf-8"))
    missing = [r["class"] for r in rows if r["class"] not in DOMAIN]
    assert not missing, f"unclassified Tas domain: {missing}"
    order = list(TAS) + [GEN]
    rows.sort(key=lambda r: (order.index(DOMAIN[r["class"]]), r["kind"] == "param", r["object"], r["op"] or "", r["name"] or ""))
    count = collections.Counter()
    out = []
    for r in rows:
        d = DOMAIN[r["class"]]
        count[d] += 1
        base = C.icon_id(r)
        fid = final_id(base, d)
        r = {k: v for k, v in r.items() if k not in ("proposed_icon_id", "proposed_resource", "tas_domain", "review_id",
                                                      "domain_note", "sam_icon_id")}
        r.update(icon_id=fid, resource=C.resource_name(fid), icon_file=f"svg/{fid}.svg", sam_icon_id=base,
                 tas_domain=d, review_id=f"TAS-{d}-{count[d]:03d}", domain_note=AMBIGUOUS.get(r["class"]))
        out.append(r)
    rows = out

    # ---- canonical icons (svg + 24 px png) and plain SAM renders (before/after only)
    ids = collections.OrderedDict()
    for r in rows:
        ids.setdefault(r["icon_id"], r)
    bases = collections.OrderedDict((r["sam_icon_id"], r) for r in rows if r["sam_icon_id"] != r["icon_id"])
    jobs = [(os.path.join(DESIGN, "svg", i + ".svg"), os.path.join(DESIGN, "png", "24", i + ".png"), tas_svg(r, r["tas_domain"], i))
            for i, r in ids.items()]
    jobs += [(os.path.join(BASE, "svg", b + ".svg"), os.path.join(BASE, "png", "24", b + ".png"), tas_svg(r, GEN, b))
             for b, r in bases.items()]
    for svg_path, _, svg in jobs:
        os.makedirs(os.path.dirname(svg_path), exist_ok=True)
        open(svg_path, "w", encoding="utf-8", newline="\n").write(svg)
    mirror(os.path.join(DESIGN, "svg"), ".svg", ids)
    mirror(os.path.join(DESIGN, "png", "24"), ".png", ids)
    mirror(os.path.join(BASE, "svg"), ".svg", bases)
    mirror(os.path.join(BASE, "png", "24"), ".png", bases)
    render.rasterise([j[0] for j in jobs], [j[1] for j in jobs])
    by_hash = collections.defaultdict(list)
    for iid in ids:
        by_hash[hashlib.sha1(open(os.path.join(DESIGN, "png", "24", iid + ".png"), "rb").read()).hexdigest()].append(iid)
    dup = [v for v in by_hash.values() if len(v) > 1]

    # ---- review sheets: one tile per object, labelled with its review id
    os.makedirs(REVIEW, exist_ok=True)

    def tile(r):
        png = os.path.join(DESIGN, "png", "24", r["icon_id"] + ".png")
        lab = (f"<b style='font-size:12px'>{r['review_id']}</b><br>{html.escape(build.display_name(r))}"
               f"<br><i>{r['icon_id']} · {r['object']} · {r['op'] or 'param'}</i>"
               + (f"<br><i style='color:#B23A00'>{html.escape(r['domain_note'])}</i>" if r["domain_note"] else ""))
        return (png, lab, bool(r["domain_note"]))

    titles = {d: f"TAS {d} · {TAS[d]['app']} ({TAS[d]['colour']})" for d in TAS}
    titles[GEN] = "TAS GEN · cross-domain / general (no corner)"
    for d in order:
        sel = [r for r in rows if r["tas_domain"] == d]
        groups = collections.OrderedDict()
        for r in sel:
            groups.setdefault("Parameters" if r["kind"] == "param" else r["object"], []).append(tile(r))
        build.sheet(titles[d], f"{len(sel)} objects. Folded top-right corner = Tas application/file "
                    f"({TAS[d]['colour'] if d in TAS else 'none'}); bottom-right badge = SAM operation (unchanged). "
                    "Green frame + note = ambiguous case, classified by the approved rule.",
                    list(groups.items()), os.path.join(REVIEW, f"contact_{d}.png"), cols=4)
    summary = " · ".join(f"{d} {count[d]}" for d in order)
    master = [(titles[d], [tile(r) for r in rows if r["tas_domain"] == d]) for d in order]
    build.sheet("SAM_Tas_Grasshopper · Tas domain master sheet", f"{len(rows)} objects · {summary}. One tile per object, review id first.",
                master, os.path.join(REVIEW, "master.png"), cols=5, width=1900)
    build.sheet(f"{build.REPO_NAME} - SAM GH icons", f"{len(rows)} Grasshopper objects · {len(ids)} distinct icons · {summary}. "
                "Native 24 px on GH normal / orange-warning / dark bodies, plus 3x. Top-right corner = Tas application.",
                master, os.path.join(DESIGN, "review", "contact_sheet.png"), cols=5, width=1900)
    palette()
    before_after(rows)
    review_md(rows, ids, dup, count, order)

    json.dump(rows, open(mpath, "w", encoding="utf-8"), indent=1)
    with open(os.path.join(DESIGN, "manifest.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    print(dict(count), f"{len(ids)} icons ({sum(1 for r in ids.values() if r['tas_domain'] != GEN)} with a Tas corner), "
          f"identical-pixel groups: {dup}")
    return 1 if dup else 0


def review_md(rows, ids, dup, count, order):
    by_icon = collections.OrderedDict()
    for r in rows:
        by_icon.setdefault(r["icon_id"], []).append(r)
    shared = [(i, rs) for i, rs in by_icon.items() if len(rs) > 1]
    lines = [f"# {build.REPO_NAME} - icon review", "",
             f"- Objects: **{len(rows)}** ({sum(r['kind'] == 'component' for r in rows)} components, "
             f"{sum(r['kind'] == 'param' for r in rows)} params)",
             "- Tas domains: " + ", ".join(f"**{d}** {count[d]}" for d in order),
             f"- Distinct icons: **{len(ids)}** (Tas corner on {sum(1 for r in ids.values() if r['tas_domain'] != GEN)})",
             f"- Identical-pixel groups (different icon ids): **{len(dup)}** {dup if dup else ''}",
             f"- Icons shared by several objects (intentional: qualifier variants, same noun + verb + domain): **{len(shared)}**", ""]
    lines += [f"  - `{i}`: " + ", ".join(f"{r['review_id']} {build.display_name(r)}" for r in rs) for i, rs in shared]
    open(os.path.join(DESIGN, "review", "REVIEW.md"), "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")


def palette():
    """Reference sheet: the five EDSL Tas identities, their source values and one sample icon each."""
    samples = {"T3D": "geometry_import", "TBD": "model_export", "TSD": "result_get", "TPD": "energyCentre_export",
               "TCD": "heat_calculate"}
    items = []
    for d, t in TAS.items():
        iid = f"{samples[d]}_{d.lower()}"
        png = os.path.join(DESIGN, "png", "24", iid + ".png")
        src = (f"docs.edsl.net SVG {t['colour']} · Tas .exe icon {t['exe']}" if d != "TCD"
               else f"no docs.edsl.net asset · TCD.exe icon {t['exe']}")
        sw = f"<span style='display:inline-block;width:46px;height:18px;background:{t['colour']};border:1px solid #111;vertical-align:middle'></span>"
        items.append((png, f"<b style='font-size:13px'>{d}</b> · {t['app']} · {t['file']}<br>{sw} <b>{t['colour']}</b>"
                           f"<br><i>{src}</i><br><i>sample: {iid}</i>", False))
    build.sheet("Tas application identities (EDSL)",
                "Source: EDSL Tas documentation assets https://docs.edsl.net/_media/{tas3d,tbd,tsd,tpd}.svg, cross-checked with the "
                "installed Tas executables; TCD from the installed TCD.exe icon (no docs asset). EDSL's red frame (#E51A29) is not used "
                "(SAM red = Remove). GEN = cross-domain / general: no corner.",
                [("Tas domain accent: folded top-right corner", items)], os.path.join(REVIEW, "palette.png"), cols=2, width=1100)


def before_after(rows):
    """Plain SAM icon (before) vs Tas-accented icon (after) for representative objects of every domain."""
    pick = ["TasOpenT3DDocument", "TasUpdateT3D", "SAMAnalyticalTBD", "SAMAnalyticalFromTBD", "TasT3DtoTBD", "TasSimulate",
            "SAMAnalyticalFromTSD", "TasTSDQueryResultsByHourOfYear", "TasTSDQueryTM59Results", "TasTPDQueryTM59Results",
            "SAMSystemsFromTPD", "SAMSystemsTASTPDSimulate", "TasCreateTCD", "SAMAnalyticalCalculateThermalTransmittance",
            "SAMAnalyticalWorkflowTBD", "SAMAnalyticalGenOpt"]
    by = {r["class"]: r for r in rows}
    items = []
    for c in pick:
        r = by[c]
        changed = r["sam_icon_id"] != r["icon_id"]
        before = os.path.join(BASE if changed else DESIGN, "png", "24", r["sam_icon_id"] + ".png")
        after = os.path.join(DESIGN, "png", "24", r["icon_id"] + ".png")
        items.append((before, f"<b>{r['review_id']} BEFORE</b><br>{html.escape(build.display_name(r))}<br><i>{r['sam_icon_id']}</i>", False))
        items.append((after, f"<b>{r['review_id']} AFTER</b><br>{r['tas_domain']}{' corner' if changed else ' (no corner)'}"
                             f"<br><i>{r['icon_id']}</i>", changed))
    build.sheet("Tas domain accent · before / after",
                "Left: plain SAM grammar icon (PR #8 before this change). Right: with the Tas domain corner. GEN objects are unchanged on "
                "purpose; the TSD and TPD TM59 queries were identical before and are now distinguished.",
                [("Representative objects", items)], os.path.join(REVIEW, "before_after.png"), cols=4)


if __name__ == "__main__":
    sys.exit(main())
