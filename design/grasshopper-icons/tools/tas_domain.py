"""SAM_Tas_Grasshopper only: EDSL Tas application / file-type domain accent (PROPOSAL, review stage).

Adds a restrained secondary accent to the SAM icon grammar: a folded file corner in the top-right of the 24x24 icon,
coloured by the native EDSL Tas application the component works with. The SAM grammar is untouched:
object glyph + SAM-green subject + operation badge (bottom-right, SAM colours) stay exactly as in SAM#166.

    top-right corner = which Tas application/file (T3D / TBD / TSD / TPD)   <- new, Tas repo only
    bottom-right disc = what the component does (Create / Get / Modify / Import / Export ...)   <- SAM, unchanged

Colours are the official EDSL identity fills from https://docs.edsl.net/_media/{tas3d,tbd,tsd,tpd}.svg (inspected
2026-09-28), cross-checked against the icons embedded in the installed Tas executables (TAS3D/TBD/TSD/TPD.exe,
dominant colours within 1-2 levels). EDSL's shared red frame (#E51A29) is deliberately NOT used: in the SAM grammar
red means Remove.

    python tas_domain.py   -> manifest.json/.csv gain tas_domain, review_id, proposed_icon_id, proposed_resource;
                              tas/svg + tas/png/24 (proposed icons); review/tas/*.png (palette, 5 domain sheets,
                              master sheet, before/after). Current integrated icon_id/resource are left unchanged.
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

DESIGN = os.path.dirname(HERE)
OUT = os.path.join(DESIGN, "tas")
REVIEW = os.path.join(DESIGN, "review", "tas")

# ------------------------------------------------------------------ EDSL Tas identities (docs.edsl.net SVG fills)
TAS = collections.OrderedDict([
    ("T3D", dict(colour="#F5DB53", app="3D Modeller", file=".t3d", exe="#F5DB53")),
    ("TBD", dict(colour="#3F9F12", app="Building Simulator", file=".tbd", exe="#40A013")),
    ("TSD", dict(colour="#F8744A", app="Results Viewer", file=".tsd", exe="#FA764B")),
    ("TPD", dict(colour="#1A72C0", app="Systems", file=".tpd", exe="#1B73C1")),
])
GEN = "GEN"  # genuinely cross-domain / general: no accent (icon identical to the current one)

# ------------------------------------------------------------------ domain of every Tas GH object (by class)
# Rule: the Tas application whose document the component primarily reads/writes as its subject.
# Evidence per class: Tas document types referenced in the component source, the original icon the authors
# chose (SAM_TasT3D / SAM_TasTBD3 / SAM_TasTSD3 / SAM_TasTPD3), and the SAM_Tas library call. "AMBIGUOUS" = explained.
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
    "TasCopyCalendar": "TBD",            # AMBIGUOUS: TCR -> TBD; writes the TBD calendar
    "TasT3DtoTBD": "TBD",                # AMBIGUOUS: T3D -> TBD conversion; produces the TBD
    "TasSimulate": "TBD",                # AMBIGUOUS: runs the Building Simulator on a TBD, writes a TSD
    "SAMAnalyticalSizingType": "TBD",    # AMBIGUOUS: sizing option of the TBD simulation (old icon: TSD)
    "TasCreateSurfaceOutputSpec": "TBD", "GooSurfaceOutputSpecParam": "TBD",  # AMBIGUOUS: TBD simulation output request
    "TasCreateDesignDays": "TBD",        # AMBIGUOUS: reads design days from a TBD or a TSD
    # TSD - Results Viewer
    "SAMAnalyticalFromTSD": "TSD", "TasTSDAddResults": "TSD", "TasTSDAddBuildingResults": "TSD",
    "TasTSDCreateAdjacencyCluster": "TSD", "TasTSDCreateSpaceSimulationResults": "TSD",
    "TasTSDQueryResultsByHourOfYear": "TSD", "TasTSDQueryResultsByPercentage": "TSD",
    "TasTSDQueryResultsByTotalPercentage": "TSD", "TasTSDQueryTM52Results": "TSD", "TasTSDQueryTM59Results": "TSD",
    "TasTSDQueryZoneResultsByHourOfYear": "TSD", "TasTSDQueryZoneResultsByPercentage": "TSD",
    "TasDesignDayNames": "TSD", "TasLogPartODiagnostics": "TSD", "TasTSDDailyIndoorComfortTemperatures": "TSD",
    "SAMAnalyticalTasPanelDataType": "TSD", "SAMAnalyticalTasSpaceDataType": "TSD", "TasWeatherYear": "TSD",
    "SAMAnalyticalSurfaceSimulationResults": "TSD",  # AMBIGUOUS: reads TSD-derived surface results from the SAM model
    # TPD - Systems
    "CreateTPDByAnalyticalModel": "TPD", "SAMSystemsCreateTPDBySystemEnergyCentre": "TPD",
    "SAMSystemsCreateTPDByTSDAndSystemEnergyCentre": "TPD", "SAMSystemsFromTPD": "TPD", "SAMSystemsResultPeriod": "TPD",
    "TasCalculateResultantTemperatureFromTPD": "TPD",  # AMBIGUOUS: TPD -> TBD/TSD follow-up run
    "TasSystemResults": "TPD", "TasSystemSpaceResults": "TPD", "TasTPDQueryTM59Results": "TPD",
    "SAMSystemsTASTPDAssignZone": "TPD", "SAMSystemsTASTPDModifyTPDByAirflows": "TPD",
    "SAMSystemsTASTPDModifyTPDByAirflowsBySpaces": "TPD", "SAMSystemsTASTPDModifyTPDFanByAirflows": "TPD",
    "SAMSystemsTASTPDSimulate": "TPD", "TasCreateTPDBySystemTypeName": "TPD",
    # GEN - genuinely cross-domain / general (no accent)
    "SAMAnalyticalWorkflowTBD": GEN, "SAMAnalyticalWorkflowgbXML": GEN,  # T3D + TBD + TSD end-to-end
    "TasCreateWeatherData": GEN,                                          # TWD, TBD or TSD
    "SAMAnalyticalWait": GEN,
    "TasOpenUKBRFile": GEN, "GooUKBRFileParam": GEN, "TasUpdateUKBRFile": GEN,  # UKBR (compliance), not a core app
    "TasCreateTCD": GEN,                                                  # TCD construction database
    # construction calculators: run on the TCD (Construction Database) engine, not on T3D/TBD/TSD/TPD
    "SAMAnalyticalCalculateConstructions": GEN, "SAMAnalyticalCalculateGlazing": GEN,
    "SAMAnalyticalCalculateThicknesses": GEN, "SAMAnalyticalCalculateThermalTransmittance": GEN,
    "SAMAnalyticalCalculateThermalTransmittanceByAperture": GEN,
    "TasCreateApertureConstructionCalculationData": GEN, "TasCreateConstructionCalculationData": GEN,
    "TasCreateLayerThicknessCalculationData": GEN, "TasCreateLayerThicknessCalculationDataByConstruction": GEN,
    "GooThermalTransmittanceCalculationDataParam": GEN, "GooThermalTransmittanceCalculationResultParam": GEN,
    "GooLayerThicknessCalculationDataParam": GEN, "GooLayerThicknessCalculationResultParam": GEN,
    # GenOpt optimisation (drives Tas runs; not an EDSL application)
    "SAMAnalyticalGenOpt": GEN, "GooAlgorithmParam": GEN, "GooObjectiveParam": GEN, "GooParameterParam": GEN,
}
GENOPT_ALGORITHMS = ("DiscreteArmijoGradient", "FibonacciDivision", "GPSHookeJeeves", "GenOptGoldenSection",
                     "HybridGeneralizedPSPO", "Mesh", "MultiStartGPS", "NelderMeadONeillcs", "Parametric",
                     "ParticleStormConstriction", "ParticleStormIntertia", "ParticleStormMesh")
for _a in GENOPT_ALGORITHMS:
    DOMAIN[f"SAMAnalytical{_a}Algorithm"] = GEN

AMBIGUOUS = {
    "TasCopyCalendar": "Reads a calendar from a TCR file and writes it into a TBD -> TBD (the file it changes).",
    "TasT3DtoTBD": "Converts T3D -> TBD. Touches both; classified by its output, the TBD.",
    "TasSimulate": "Runs the Building Simulator on a TBD and writes a TSD. The simulation belongs to TBD; TSD is its output.",
    "SAMAnalyticalSizingType": "Enum feeding Tas.Simulate / Tas.Sizing (TBD). The original icon was the TSD one.",
    "TasCreateSurfaceOutputSpec": "Spec of the surface outputs a TBD simulation should write (consumed by Tas.Simulate).",
    "GooSurfaceOutputSpecParam": "Param for the TBD simulation output spec (same reasoning).",
    "TasCreateDesignDays": "Reads design days from either a TBD or a TSD. Design days are defined in the TBD.",
    "SAMAnalyticalSurfaceSimulationResults": "Queries surface results already on a SAM model; they originate from a TSD.",
    "TasCalculateResultantTemperatureFromTPD": "Lives in the TPD project; takes a TPD run and re-simulates the TBD/TSD.",
}


# ------------------------------------------------------------------ accent
def accent(domain):
    """Folded file corner, top-right: white separator + EDSL colour + one ink hairline (kept clear of the badge)."""
    c = TAS[domain]["colour"]
    return ('<path d="M15.6 0L24 8.4" stroke="#FFFFFF" stroke-width="1.6" fill="none"/>'
            f'<polygon points="16.4,0 24,0 24,7.6" fill="{c}"/>'
            '<path d="M16.4 0L24 7.6" stroke="#111111" stroke-width="0.9" fill="none" stroke-linecap="round"/>')


def tas_svg(r, domain):
    svg = icons.icon_svg(r["object"], r["op"], plural=r["plural"], container=r["container"],
                         comment=f"SAM GH icon {r['icon_id']} + Tas {domain} accent")
    return svg if domain == GEN else svg.replace("</svg>", accent(domain) + "</svg>")


def proposed_id(r, domain):
    return r["icon_id"] if domain == GEN else f"{r['icon_id']}_{domain.lower()}"


def main():
    mpath = os.path.join(DESIGN, "manifest.json")
    rows = json.load(open(mpath, encoding="utf-8"))
    missing = [r["class"] for r in rows if r["class"] not in DOMAIN]
    assert not missing, f"unclassified Tas domain: {missing}"
    order = list(TAS) + [GEN]
    rows.sort(key=lambda r: (order.index(DOMAIN[r["class"]]), r["kind"] == "param", r["object"], r["op"] or "", r["name"] or ""))
    count = collections.Counter()
    for r in rows:
        d = DOMAIN[r["class"]]
        count[d] += 1
        r["tas_domain"] = d
        r["review_id"] = f"TAS-{d}-{count[d]:03d}"
        r["proposed_icon_id"] = proposed_id(r, d)
        r["proposed_resource"] = "SAM_GH_" + "".join(p[:1].upper() + p[1:] for p in r["proposed_icon_id"].split("_"))
        r["domain_note"] = AMBIGUOUS.get(r["class"])

    # ---- proposed icons (svg + 24 px png)
    ids = collections.OrderedDict()
    for r in rows:
        ids.setdefault(r["proposed_icon_id"], r)
    svgs, pngs = [], []
    for iid, r in ids.items():
        p = os.path.join(OUT, "svg", iid + ".svg")
        os.makedirs(os.path.dirname(p), exist_ok=True)
        open(p, "w", encoding="utf-8", newline="\n").write(tas_svg(r, r["tas_domain"]))
        svgs.append(p)
        pngs.append(os.path.join(OUT, "png", "24", iid + ".png"))
    for sub in ("svg", os.path.join("png", "24")):  # mirror the manifest exactly
        d = os.path.join(OUT, sub)
        os.makedirs(d, exist_ok=True)
        for fn in os.listdir(d):
            if os.path.splitext(fn)[0] not in ids:
                os.remove(os.path.join(d, fn))
    render.rasterise(svgs, pngs)
    by_hash = collections.defaultdict(list)
    for iid in ids:
        by_hash[hashlib.sha1(open(os.path.join(OUT, "png", "24", iid + ".png"), "rb").read()).hexdigest()].append(iid)
    dup = [v for v in by_hash.values() if len(v) > 1]

    # ---- review sheets: one tile per object, labelled with its review id
    os.makedirs(REVIEW, exist_ok=True)

    def tile(r):
        png = os.path.join(OUT, "png", "24", r["proposed_icon_id"] + ".png")
        lab = (f"<b style='font-size:12px'>{r['review_id']}</b><br>{html.escape(build.display_name(r))}"
               f"<br><i>{r['proposed_icon_id']} · {r['object']} · {r['op'] or 'param'}</i>"
               + (f"<br><i style='color:#B23A00'>ambiguous: {html.escape(r['domain_note'])}</i>" if r["domain_note"] else ""))
        return (png, lab, bool(r["domain_note"]))

    titles = {d: f"TAS {d} · {TAS[d]['app']} ({TAS[d]['colour']})" for d in TAS}
    titles[GEN] = "TAS GEN · cross-domain / general (no accent)"
    for d in order:
        sel = [r for r in rows if r["tas_domain"] == d]
        groups = collections.OrderedDict()
        for r in sel:
            groups.setdefault("Parameters" if r["kind"] == "param" else r["object"], []).append(tile(r))
        build.sheet(titles[d], f"{len(sel)} objects. Folded top-right corner = Tas application/file ({TAS[d]['colour'] if d in TAS else 'none'}); "
                    "bottom-right badge = SAM operation (unchanged). Ambiguous classifications have a green frame and a note.",
                    list(groups.items()), os.path.join(REVIEW, f"contact_{d}.png"), cols=4)
    build.sheet("SAM_Tas_Grasshopper · Tas domain master sheet",
                f"{len(rows)} objects · T3D {count['T3D']} · TBD {count['TBD']} · TSD {count['TSD']} · TPD {count['TPD']} · GEN {count[GEN]}. "
                "One tile per object, review id first.",
                [(titles[d], [tile(r) for r in rows if r["tas_domain"] == d]) for d in order],
                os.path.join(REVIEW, "master.png"), cols=5, width=1900)
    palette(rows)
    before_after(rows)

    json.dump(rows, open(mpath, "w", encoding="utf-8"), indent=1)
    with open(os.path.join(DESIGN, "manifest.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    print(dict(count), f"{len(ids)} proposed icons, identical-pixel groups: {dup}")
    return 1 if dup else 0


def palette(rows):
    """Small reference sheet: the four EDSL Tas identities, their source values and one sample icon each."""
    samples = {"T3D": "geometry_import", "TBD": "model_export", "TSD": "result_get", "TPD": "energyCentre_export"}
    items = []
    for d, t in TAS.items():
        iid = f"{samples[d]}_{d.lower()}"
        png = os.path.join(OUT, "png", "24", iid + ".png")
        sw = (f"<span style='display:inline-block;width:46px;height:18px;background:{t['colour']};border:1px solid #111;vertical-align:middle'></span>")
        items.append((png, f"<b style='font-size:13px'>{d}</b> · {t['app']} · {t['file']}<br>{sw} <b>{t['colour']}</b>"
                           f"<br><i>docs.edsl.net SVG {t['colour']} · Tas .exe icon {t['exe']}</i><br><i>sample: {iid}</i>", False))
    build.sheet("Tas application identities (EDSL)", "Source: official EDSL Tas documentation assets https://docs.edsl.net/_media/{tas3d,tbd,tsd,tpd}.svg, "
                "cross-checked with the installed Tas executables. EDSL's red frame (#E51A29) is not used (SAM red = Remove).",
                [("Tas domain accent: folded top-right corner", items)], os.path.join(REVIEW, "palette.png"), cols=2, width=1100)


def before_after(rows):
    """Current (integrated) icon vs proposed Tas-accented icon for representative objects of every domain."""
    pick = ["TasOpenT3DDocument", "TasUpdateT3D", "SAMAnalyticalTBD", "SAMAnalyticalFromTBD", "TasUpdateConstructions",
            "TasSimulate", "SAMAnalyticalFromTSD", "TasTSDQueryResultsByHourOfYear", "TasTSDQueryTM59Results",
            "TasTPDQueryTM59Results", "SAMSystemsFromTPD", "SAMSystemsTASTPDSimulate", "SAMAnalyticalWorkflowTBD",
            "SAMAnalyticalCalculateThermalTransmittance"]
    by = {r["class"]: r for r in rows}
    items = []
    for c in pick:
        r = by[c]
        before = os.path.join(DESIGN, "png", "24", r["icon_id"] + ".png")
        after = os.path.join(OUT, "png", "24", r["proposed_icon_id"] + ".png")
        items.append((before, f"<b>{r['review_id']} BEFORE</b><br>{html.escape(build.display_name(r))}<br><i>{r['icon_id']}</i>", False))
        items.append((after, f"<b>{r['review_id']} AFTER</b><br>{r['tas_domain']} accent<br><i>{r['proposed_icon_id']}</i>", r["tas_domain"] != GEN))
    build.sheet("Tas domain accent · before / after", "Left: icon currently in PR #8. Right: proposed. GEN objects are unchanged on purpose "
                "(e.g. TPD vs TSD TM59 queries were identical before and are now distinguished by the corner).",
                [("Representative objects", items)], os.path.join(REVIEW, "before_after.png"), cols=4)


if __name__ == "__main__":
    sys.exit(main())
