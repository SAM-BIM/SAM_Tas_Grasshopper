"""Repository-specific classification decisions for SAM_Tas_Grasshopper (the only non-shared tool file).

OVERRIDES     : component display name -> (object glyph, op, extra)   extra: None | "plural" | "library" | note
PARAM_OBJECTS : param type key (Goo<X>Param class or typeof(X) name) -> glyph | (glyph, container, plural)
OBJECTS/VERBS : extra noun/verb rules tried before the shared ones (same shapes as SAM's OBJECTS/VERBS)
"""
# Tas files: draw the SAM object read/written (TBD = building model, TSD = results, TPD = plant/energy centre,
# T3D = 3D geometry), with import (Tas -> SAM) / export (SAM -> Tas) / convert (Tas -> Tas); `file` only when the file itself is the subject.
OVERRIDES = {
    # TBD (building model)
    "SAMAnalytical.TBD": ("model", "export", None), "SAMAnalytical.TBD_SAP": ("model", "export", None),
    "SAMAnalytical.TBD_TM59": ("model", "export", None), "SAMAnalytical.FromTBD": ("model", "import", None),
    "SAMAnalytical.CreateTBDByTM59": ("file", "create", "compliance TBD file"), "Tas.CreateTBDByPartL": ("file", "create", "compliance TBD file"),
    "Tas.T3DtoTBD": ("model", "convert", None),
    "Tas.Simulate": ("file", "run", "simulates a TBD file into a TSD"),
    "SAMAnalytical.WorkflowTBD": ("model", "run", "full Tas workflow"), "SAMAnalytical.WorkflowgbXML": ("model", "run", "full Tas workflow"),
    "Tas.Sizing": ("heat", "run", "Tas sizing"), "Tas.UpdateDesignLoads": ("heat", "update", None),
    "Tas.UpdateFacingExternal": ("space", "update", None), "Tas.UpdateShading": ("shade", "update", None),
    "Tas.RemoveSchedules": ("profile", "remove", "plural"), "Tas.CopyCalendar": ("calendar", "copy", None),
    "Tas.CreateTCD": ("construction", "export", "library"),
    "SAMAnalytical.CalculateGlazing": ("apertureConstruction", "calculate", None),
    "SAMAnalytical.CalculateThicknesses": ("constructionLayer", "calculate", "plural"),
    "SAMAnalytical.CalculateUValues": ("heat", "calculate", None), "SAMAnalytical.CalculateUValuesByApertures": ("heat", "calculate", None),
    "Tas.CreateLayerThicknessCalculationData": ("constructionLayer", "create", None),
    "Tas.CreateLayerThicknessCalculationDataByConstruction": ("constructionLayer", "create", None),
    "Tas.CreateSurfaceOutputSpec": ("settings", "create", None),
    "SAMAnalytical.Wait": ("clock", "run", "waits"),
    # T3D (3D geometry)
    "Tas.OpenT3DDocument": ("geometry", "import", None), "Tas.UpdateT3D": ("geometry", "update", None),
    "gbXML.TasT3D": ("geometry", "convert", "gbXML -> T3D"),
    # TSD (results)
    "SAMAnalytical.FromTSD": ("result", "import", None), "Tas.TSDCreateAdjacencyCluster": ("cluster", "create", None),
    "Tas.TSDCreateSpaceSimulationResults": ("result", "create", None), "Tas.TSDAddResults": ("result", "add", None),
    "Tas.TSDQueryResultsByHourOfYear": ("result", "get", None), "Tas.TSDQueryResultsByPercentage": ("result", "get", None),
    "Tas.TSDQueryResultsByTotalPercentage": ("result", "get", None),
    "Tas.TSDQueryZoneResultsByHourOfYear": ("zone", "get", None), "Tas.TSDQueryZoneResultsByPercentage": ("zone", "get", None),
    "Tas.TSDQueryTM52Results": ("thermometer", "get", None), "Tas.TSDQueryTM59Results": ("thermometer", "get", None),
    "Tas.TPDQueryTM59Results": ("thermometer", "get", None),
    "SAMAnalytical.SurfaceSimulationResults": ("result", "get", "plural"),
    "Tas.DesignDayNames": ("designDay", "get", None), "Tas.LogPartODiagnostics": ("log", "export", None),
    "Tas.WeatherYear": ("weatherYear", "import", None),
    # TPD (plant / energy centre)
    "SAMAnalytical.TPDByAnalyticalModel": ("system", "export", None), "Tas.CreateTPDBySystemTypeName": ("system", "export", None),
    "SAMSystems.CreateTPDBySystemEnergyCentre": ("energyCentre", "export", None),
    "SAMSystems.CreateTPDByTSDAndSystemEnergyCentre": ("energyCentre", "export", None),
    "SAMSystems.FromTPD": ("energyCentre", "import", None), "TasTPD.Simulate": ("system", "run", "plant simulation"),
    "TasTPD.ModifyTPDByAirflows": ("airflow", "set", None), "TasTPD.ModifyTPDByAirflowsBySpaces": ("airflow", "set", None),
    "TasTPD.ModifyTPDFanByAirflows": ("fan", "modify", None),
    "Tas.SystemResults": ("result", "convert", None), "Tas.SystemSpaceResults": ("result", "convert", None),
    # GenOpt
    "SAMAnalytical.GenOpt": ("algorithm", "run", "GenOpt optimisation"),
}
for _a in ("DiscreteArmijoGradient", "FibonacciDivision", "GPSHookeJeeves", "GoldenSection", "HybridGeneralizedPSPO", "Mesh",
           "MultiStartGPS", "NelderMeadONeillcs", "Parametric", "ParticleStormConstriction", "ParticleStormIntertia", "ParticleStormMesh"):
    OVERRIDES[f"SAMAnalytical.{_a}Algorithm"] = ("algorithm", "create", None)
PARAM_OBJECTS = {
    "LayerThicknessCalculationData": "constructionLayer", "LayerThicknessCalculationResult": "result",
    "ThermalTransmittanceCalculationData": "heat", "ThermalTransmittanceCalculationResult": "result",
    "SAMT3DDocument": "geometry", "UKBRFile": "file", "Parameter": "value", "SurfaceOutputSpec": "settings",
}
OBJECTS = []
VERBS = []
