// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Weather;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalCreateTBDByTM59 : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("4759368e-268c-4f12-b320-cb3941476062");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCreateTBDByTM59()
          : base("SAMAnalytical.CreateTBDByTM59", "SAMAnalytical.CreateTBDByTM59",
              "Create TBD file ByTM59",
              "SAM WIP", "Tas")
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_path_TBD", NickName = "_path_TBD", Description = "A file path to a Tas file TBD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                GooTextMapParam gooTextMapParam = new GooTextMapParam() { Name = "textMap_", NickName = "textMap_", Description = "SAM Core TextMap", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(gooTextMapParam, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_includeSAP_", NickName = "_IncludeSAP_", Description = "Include SAP", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_zoneCategory_SAP", NickName = "_zoneCategory_SAP", Description = "Zone Category", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));


                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                //Appended so every existing input keeps its saved Grasshopper port index.
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "overheatingScenarios_", NickName = "overheatingScenarios_", Description = "SAM Part O Overheating Scenarios. When supplied, they are authoritative in the TM59 XML opened by TAS.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return [.. result];
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("successful");
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            string path = null;
            index = Params.IndexOfInputParam("_path_TBD");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            analyticalModel = new AnalyticalModel(analyticalModel);

            WeatherData weatherData = null;
            index = Params.IndexOfInputParam("weatherData_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref weatherData))
                {
                    weatherData = null;
                }
            }

            TextMap textMap = null;
            index = Params.IndexOfInputParam("textMap_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref textMap))
                {
                    textMap = null;
                }
            }

            if (textMap == null)
            {
                textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();
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

            List<DesignDay> heatingDesignDays = [];
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            List<DesignDay> coolingDesignDays = [];
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            bool includeSAP = true;
            index = Params.IndexOfInputParam("_includeSAP_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref includeSAP);
            }

            string zoneCategory = null;
            index = Params.IndexOfInputParam("_zoneCategory_SAP");
            if (index != -1)
            {
                dataAccess.GetData(index, ref zoneCategory);
            }

            bool successful = false;

            bool converted = Analytical.Tas.Convert.ToTBD(analyticalModel, path, weatherData, coolingDesignDays, heatingDesignDays, true);
            if (converted)
            {
                bool converted_TM59 = false;
                string path_TM59 = null;

                if (Analytical.Tas.TM59.Modify.TryCreatePath(path, out path_TM59))
                {
                    TM59Manager tM59Manager = new(textMap);

                    if (overheatingScenarios == null)
                    {
                        //Compatibility for definitions that predate OverheatingScenario. The existing path is
                        //preserved; when scenarios are supplied the branch below bypasses this superseded model
                        //derivation and writes the authoritative strategies instead.
                        converted_TM59 = Analytical.Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, tM59Manager);
                    }
                    else
                    {
                        SimulationSpaceMap simulationSpaceMap = SimulationSpaceMap.Identity(analyticalModel.GetSpaces());
                        OverheatingScenarioMap overheatingScenarioMap = new(overheatingScenarios, analyticalModel, simulationSpaceMap);

                        foreach (string refusal in overheatingScenarioMap.Refusals)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, refusal);
                        }

                        if (overheatingScenarioMap.IsComplete)
                        {
                            converted_TM59 = Analytical.Tas.TM59.Convert.ToXml(analyticalModel, path_TM59, tM59Manager, overheatingScenarioMap.VentilationStrategyMap, out List<string> refusals);

                            foreach (string refusal in refusals)
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, refusal);
                            }
                        }
                    }
                }

                //A refused TM59 conversion must not leave a previous run's XML beside the freshly rewritten
                //TBD - TAS would open ventilation strategies that no longer correspond to this model or its
                //scenarios. This covers both refusals: an incomplete scenario map (ToXml never called) and a
                //conversion that refuses after being called (for example, a model that now holds no spaces).
                if (!converted_TM59 && !string.IsNullOrWhiteSpace(path_TM59) && System.IO.File.Exists(path_TM59))
                {
                    System.IO.File.Delete(path_TM59);
                }

                successful = converted_TM59;

                if (includeSAP)
                {
                    if(string.IsNullOrWhiteSpace(zoneCategory))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Zone Category has not been provided. SAP could not be generated");
                        successful = false;
                    }
                    else
                    {
                        if (!Analytical.Tas.SAP.Modify.TryCreatePath(path, out string path_SAP))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "SAP path could not be created. SAP could not be generated");
                            successful = false;
                        }
                        else
                        {
                            bool converted_SAP = Analytical.Tas.SAP.Convert.ToFile(analyticalModel, path_SAP, zoneCategory, textMap);

                            //SAP is still attempted, but its success cannot turn a refused or failed TM59 XML
                            //back into overall success.
                            successful = successful && converted_SAP;
                        }
                        
                    }
                }
            }

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, successful);
            }
        }
    }
}
