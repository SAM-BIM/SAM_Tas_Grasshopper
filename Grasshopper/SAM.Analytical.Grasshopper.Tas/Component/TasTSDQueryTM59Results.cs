// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDQueryTM59Results : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d9f84a00-275e-401c-9c69-ad30b4ccb403");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.1.0";

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        /// <summary>
        /// Initializes a new instance of the TasTSDQueryTM59Results class.
        /// </summary>
        public TasTSDQueryTM59Results()
          : base("Tas.TSDQueryTM59Results", "Tas.TSDQueryTM59Results",
              "Reads TM59 overheating-criteria results from a TasTSD file for the given space or zone.\nThe assessment covers the summer period (1 May - 30 September). Use Inspect on the output to see the individual criterion outcomes.",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "A file path to a TasTSD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces or Zone", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_extended_", NickName = "_extended_", Description = "Return extended results", Access = GH_ParamAccess.item, Optional = true };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String { Name = "_tM52BuildingCategory", NickName = "_tM52BuildingCategory", Description = "Category of Buildings I, II, III or IV", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(TM52BuildingCategory.CategoryII.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                //Appended so every existing input keeps its saved Grasshopper port index.
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "overheatingScenarios_", NickName = "overheatingScenarios_", Description = "SAM Part O Overheating Scenarios. When supplied, they are authoritative over the TM59 ventilation criterion.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean saveReport = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_saveReport_", NickName = "_saveReport_", Description = "Set to True to write the report text to reportFilePath_. Leave False (the default) so a normal Grasshopper recomputation never creates or overwrites a file.", Access = GH_ParamAccess.item, Optional = true };
                saveReport.SetPersistentData(false);
                result.Add(new GH_SAMParam(saveReport, ParamVisibility.Voluntary));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "reportFilePath_", NickName = "reportFilePath_", Description = "File path the report text is written to when _saveReport_ is True.", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));

                return [.. result];
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "SAM Analytical Spaces", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59MechanicalVentilationResults", NickName = "tM59MechanicalVentilationResults", Description = "SAM TM59 Mechanical Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59NaturalVentilationResults", NickName = "tM59NaturalVentilationResults", Description = "SAM TM59 Natural Ventilation Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "tM59CorridorResults", NickName = "tM59CorridorResults", Description = "SAM TM59 Corridor Results", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortUpperLimitTemperatures", NickName = "indoorComfortULTemperatures Tupp", Description = "Indoor Comfort Upper Limit Temperatures Tupp \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8+3 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "indoorComfortLowerLimitTemperatures", NickName = "indoorComfortLLTemperatures Tll", Description = "Indoor Comfort Lower Limit Temperatures Tll \nTcomf = 0.33 Trm + 18.8  where TuppCatII =0.33 Trm + 18.8-4 ", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly extracted?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                //Appended so every existing output keeps its saved Grasshopper port index.
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "report", NickName = "report", Description = "Human-readable TM59 verification summary containing natural ventilation, mechanical ventilation and corridor results, margins, status and legend. Intended for direct connection to a Grasshopper Panel.", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "reportFilePath", NickName = "reportFilePath", Description = "The path the report was actually written to. Empty unless _saveReport_ is True and the write succeeded.", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));

                return [.. result];
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful;
            index_Successful = Params.IndexOfOutputParam("successful");
            if(index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref run))
                {
                    run = false;
                }
            }

            if (!run)
            {
                return;
            }

            index = Params.IndexOfInputParam("_pathTasTSD");
            if(index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path = null;
            if (!dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Space> spaces = null;
            List<Zone> zones = null;
            index = Params.IndexOfInputParam("_spaces_");
            if (index != -1)
            {
                List<IAnalyticalObject> analyticalObjects = [];
                if (!dataAccess.GetDataList(index, analyticalObjects))
                {
                    analyticalObjects = null;
                }

                spaces = analyticalObjects?.FindAll(x => x is Space).ConvertAll(x => x as Space);
                if (spaces != null && spaces.Count == 0)
                {
                    spaces = null;
                }

                zones = analyticalObjects?.FindAll(x => x is Zone).ConvertAll(x => x as Zone);
                if (zones != null && zones.Count == 0)
                {
                    zones = null;
                }
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<OverheatingScenario> overheatingScenarios = null;
            index = Params.IndexOfInputParam("overheatingScenarios_");
            if (index != -1)
            {
                List<IAnalyticalObject> analyticalObjects = [];
                if (dataAccess.GetDataList(index, analyticalObjects))
                {
                    overheatingScenarios = analyticalObjects.FindAll(x => x is OverheatingScenario).ConvertAll(x => x as OverheatingScenario);
                    if (overheatingScenarios.Count == 0)
                    {
                        overheatingScenarios = null;
                    }
                }
            }

            index = Params.IndexOfInputParam("_tM52BuildingCategory");
            string @string = null;
            if (index == -1 || !dataAccess.GetData(index, ref @string) || string.IsNullOrEmpty(@string))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!Core.Query.TryGetEnum(@string, out TM52BuildingCategory tM52BuildingCategory) || tM52BuildingCategory == TM52BuildingCategory.Undefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool extended = false;
            index = Params.IndexOfInputParam("_extended_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref extended))
                {
                    extended = false;
                }
            }

            TSDConversionSettings tSDConversionSettings = new ()
            {
                SpaceDataTypes = new HashSet<SpaceDataType>() { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : [.. spaces.ConvertAll(x => x?.Name)],
                ZoneNames = zones == null ? null : [.. zones.ConvertAll(x => x?.Name)],
                ConvertWeaterData = true,
                ConvertZones = true
            };

            AnalyticalModel analyticalModel_TSD = Analytical.Tas.Convert.ToSAM(path, tSDConversionSettings);

            //Everything from here on is TM59AssessmentCalculator's - the same sequence this component used to
            //state inline, now in SAM.Analytical where it can be called and tested. The TSD read above is the
            //only part that needs TAS. Create.TM59AssessmentCalculator stamps the two series keys the TSD
            //conversion writes and the provenance this assembly has always stamped, and builds the
            //SimulationSpaceMap from the zone guid TAS preserves across the round trip.
            //
            //That map is why this component no longer matches spaces by NAME. Every flat in a block has a
            //"Bedroom 2", and the old code restored one flat's internal condition onto another flat's room -
            //driving the assessment with the wrong occupancy profile and the wrong system, then reporting the
            //answer as if it belonged to the right room. Where an identity does not resolve the space is now
            //left out and the reason reported, rather than paired with a same-named room.
            TM59AssessmentCalculator tM59AssessmentCalculator = analyticalModel_TSD.TM59AssessmentCalculator(analyticalModel);
            tM59AssessmentCalculator.TM52BuildingCategory = tM52BuildingCategory;

            if (overheatingScenarios != null)
            {
                OverheatingScenarioMap overheatingScenarioMap = new(overheatingScenarios, analyticalModel, tM59AssessmentCalculator.SimulationSpaceMap);
                tM59AssessmentCalculator.VentilationStrategyMap = overheatingScenarioMap.VentilationStrategyMap;

                foreach (string refusal in overheatingScenarioMap.Refusals)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, refusal);
                }
            }

            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            List<string> associationRefusals = [.. tM59AssessmentCalculator.AssociationRefusals];

            List<Space> spaces_Result = tM59AssessmentCalculator.Spaces(spaces, zones);

            associationRefusals.AddRange(tM59AssessmentCalculator.AssociationRefusals);

            //Reported as warnings, not swallowed: a space missing from the assessment because its identity could
            //not be resolved is a gap the user has to be able to see.
            foreach (string associationRefusal in associationRefusals)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, associationRefusal);
            }

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(spaces_Result, extended);
            if (tM59AssessmentResult == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            foreach (string refusal in tM59AssessmentResult.VentilationStrategyRefusals)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, refusal);
            }

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.Spaces.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("tM59MechanicalVentilationResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.MechanicalVentilationResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("tM59NaturalVentilationResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.NaturalVentilationResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("tM59CorridorResults");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.CorridorResults.ConvertAll(x => new GooResult(x)));
            }

            index = Params.IndexOfOutputParam("indoorComfortUpperLimitTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.MaxIndoorComfortTemperatures?.Values);
            }

            index = Params.IndexOfOutputParam("indoorComfortLowerLimitTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tM59AssessmentResult.MinIndoorComfortTemperatures?.Values);
            }

            string report = new TM59AssessmentReport(tM59AssessmentResult, path).ToString();

            index = Params.IndexOfOutputParam("report");
            if (index != -1)
            {
                //A view over the result that was just published on the other outputs - it reads their numbers
                //and verdicts and reformats them. It runs no assessment, so connecting it cannot change them.
                dataAccess.SetData(index, report);
            }

            //Voluntary, and off by default: a normal Grasshopper recomputation (opening the file, a solver
            //pass triggered by an unrelated upstream change) must never create or overwrite a file on disk.
            //Only an explicit _saveReport_ = True does that, and only to the exact path stated - never a
            //fallback location, and never a false claim of success.
            bool saveReport = false;
            index = Params.IndexOfInputParam("_saveReport_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref saveReport);
            }

            if (saveReport)
            {
                string reportFilePath = null;
                index = Params.IndexOfInputParam("reportFilePath_");
                if (index != -1)
                {
                    dataAccess.GetData(index, ref reportFilePath);
                }

                if (string.IsNullOrWhiteSpace(reportFilePath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "_saveReport_ is True but reportFilePath_ is empty. Report not saved.");
                }
                else
                {
                    try
                    {
                        System.IO.File.WriteAllText(reportFilePath, report, System.Text.Encoding.UTF8);

                        int index_ReportFilePath = Params.IndexOfOutputParam("reportFilePath");
                        if (index_ReportFilePath != -1)
                        {
                            dataAccess.SetData(index_ReportFilePath, reportFilePath);
                        }
                    }
                    catch (Exception exception)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("Could not save report to '{0}': {1}", reportFilePath, exception.Message));
                    }
                }
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open TSD", Menu_OpenTSD, Resources.SAM_TasTSD3, true, false);
        }

        private void Menu_OpenTSD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_pathTasTSD");
            if (index_Path == -1)
            {
                return;
            }

            string path = null;

            object @object = null;

            @object = Params.Input[index_Path].VolatileData.AllData(true)?.OfType<object>()?.ElementAt(0);
            if (@object is IGH_Goo)
            {
                path = (@object as dynamic).Value?.ToString();
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}
