// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas.TPD
{
    public class TasTPDQueryTM59Results : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("68f3f8e7-884f-4fe6-832e-da69e30d4fe9");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTPD3;


        /// <summary>
        /// Initializes a new instance of the TasTPDQueryTM59Results class.
        /// </summary>
        public TasTPDQueryTM59Results()
          : base("Tas.TPDQueryTM59Results", "Tas.TPDQueryTM59Results",
              "Query TPD for TM59Results" +
               "this node will query results for summer 01 May to 30 September for a given space or zone and output when inspect results",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTPD", NickName = "_pathTasTPD", Description = "A file path to a TasTPD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_spaces_", NickName = "_spaces_", Description = "SAM Analytical Spaces or Zone", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_extended_", NickName = "_extended_", Description = "Return extended results", Access = GH_ParamAccess.item, Optional = true };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String { Name = "_tM52BuildingCategory", NickName = "_tM52BuildingCategory", Description = "Category of Buildings I, II, III or IV", Access = GH_ParamAccess.item, Optional = true };
                @string.SetPersistentData(TM52BuildingCategory.CategoryII.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer @integer;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                //Appended so every existing input keeps its saved Grasshopper port index.
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "overheatingScenarios_", NickName = "overheatingScenarios_", Description = "SAM Part O Overheating Scenarios. When supplied, they are authoritative over the TM59 ventilation criterion.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

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

            index = Params.IndexOfInputParam("_pathTasTPD");
            if(index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TPD = null;
            if (!dataAccess.GetData(index, ref path_TPD) || string.IsNullOrWhiteSpace(path_TPD) || !System.IO.File.Exists(path_TPD))
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

            //Optional, and appended after the existing inputs. When scenarios are supplied they are authoritative
            //over the ventilation criterion; when absent the documented legacy derivation stands, so a saved
            //definition behaves as it did.
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

            //Step 1 of 3, and the only part that needs TAS: read the TPD's per-zone system results, and the
            //companion TSD beside it for the radiant series.
            List<SystemSpaceResult> systemSpaceResults = Analytical.Tas.TPD.Convert.ToSAM_SpaceSystemResults(path_TPD, out string path_TSD);
            if(systemSpaceResults is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            TSDConversionSettings tSDConversionSettings = new ()
            {
                SpaceDataTypes = new HashSet<Analytical.Tas.SpaceDataType>() { Analytical.Tas.SpaceDataType.MeanRadiantTemperature, Analytical.Tas.SpaceDataType.OccupantSensibleGain },
                SpaceNames = spaces == null ? null : [.. spaces.ConvertAll(x => x?.Name)],
                ZoneNames = zones == null ? null : [.. zones.ConvertAll(x => x?.Name)],
                ConvertWeaterData = true,
                ConvertZones = true
            };

            AnalyticalModel analyticalModel_TSD = Analytical.Tas.Convert.ToSAM(path_TSD, tSDConversionSettings);
            if (analyticalModel_TSD is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            //Step 2 of 3: the TAS-specific preparation, which used to be stated inline here.
            //
            //THIS IS THE LEGACY, APPROXIMATE TPD ROUTE. It runs ONE simulation and SYNTHESISES the resultant
            //temperature as the mean of the TSD's mean radiant temperature and the TPD's zone temperature. It is
            //NOT the authoritative TPD-full route: that one
            //(Analytical.Tas.TPD.Modify.CalculateResultantTemperature) deliberately pays for a SECOND TAS
            //simulation on a COPY of the TBD and reads a real resultant temperature out of the second TSD. The two
            //are kept separately callable on purpose, and a failure of that route must never arrive here - a
            //synthesised series reported in place of a simulated one is an approximation wearing the real thing's
            //clothes.
            //
            //The arithmetic lives in SAM.Analytical.Tas.TPD and not in TM59AssessmentCalculator, because averaging
            //a radiant temperature with a systems-model zone temperature is a TAS accommodation for a series TPD
            //does not produce. The assessment must stay engine-neutral: preparation differs, assessment does not.
            //
            //It also no longer matches TPD results to spaces by NAME. Every flat in a block has a "Bedroom 2", and
            //the old inline code took the first result whose name matched - silently reporting one dwelling's
            //system results against another dwelling's room.
            Analytical.Tas.TPD.ApproximateResultantTemperatureMap approximateResultantTemperatureMap = new(
                analyticalModel_TSD,
                systemSpaceResults,
                Analytical.Tas.Query.SimulationSpaceKey,
                Analytical.Tas.SpaceDataType.MeanRadiantTemperature.Text(),
                Analytical.Tas.SpaceDataType.ResultantTemperature.Text());

            foreach (string refusal in approximateResultantTemperatureMap.Refusals)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, refusal);
            }

            //Checked rather than assumed: the preparation returns no model on its own refusal branches, and
            //dereferencing that would surface a NullReferenceException where a refusal was already reported.
            if (!approximateResultantTemperatureMap.IsSupported)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The TPD preparation produced no model to assess.");
                return;
            }

            //Step 3 of 3: the common, engine-neutral assessment - the same one the TSD-simple route runs. This
            //component no longer holds its own copy of the TM59 recipe.
            TM59AssessmentCalculator tM59AssessmentCalculator = approximateResultantTemperatureMap.AnalyticalModel.TM59AssessmentCalculator(analyticalModel);
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

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open TPD", Menu_OpenTPD, Resources.SAM_TasTPD3, true, false);
        }

        private void Menu_OpenTPD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_pathTasTPD");
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