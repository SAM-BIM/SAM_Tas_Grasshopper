// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Properties;
using SAM.Analytical.Tas.GenOpt;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class SAMAnalyticalGenOpt : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5259b075-7da6-4d20-8364-67225c43dc4c");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_GH_AlgorithmRun;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalGenOpt()
          : base("SAMAnalytical.GenOpt", "SAMAnalytical.GenOpt",
              "SAM Analytical GenOpt.\nRuns the optimisation natively (SAM.Math kernel driving TasGenExecute, one evaluation at a time); Java and GenOpt are not used.\nSupported algorithms: GoldenSection, GPSHookeJeeves.",
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
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                Param_FilePath filePath = new Param_FilePath() { Name = "_scriptPath", NickName = "_scriptPath", Description = "Script path", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(filePath, ParamVisibility.Binding));

                GooParameterParam parameters = new GooParameterParam() { Name = "_parameters", NickName = "_parameters", Description = "Parameter", Access = GH_ParamAccess.list};
                result.Add(new GH_SAMParam(parameters, ParamVisibility.Binding));

                GooObjectiveParam objectives = new GooObjectiveParam() { Name = "_objectives", NickName = "_objectives", Description = "Objectives", Access = GH_ParamAccess.list};
                result.Add(new GH_SAMParam(objectives, ParamVisibility.Binding));

                GooAlgorithmParam algorithm = new GooAlgorithmParam() { Name = "_algorithm_", NickName = "_algorithm_", Description = "Algorithm_", Access = GH_ParamAccess.item, Optional = true };
                algorithm.SetPersistentData(new GoldenSectionAlgorithm());
                result.Add(new GH_SAMParam(algorithm, ParamVisibility.Binding));

                Param_Boolean  @boolean = new Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                // "successful" stays first. The native-result outputs are appended after it, so a placed component
                // keeps its saved output and can add the new ones from the zoomable UI.
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new Param_Boolean() { Name = "successful", NickName = "successful", Description = "True when the optimisation ended normally (Success, simulation limit or nullspace) and was not cancelled.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new Param_String() { Name = "outcome", NickName = "outcome", Description = "How the run ended (Success, MaximumSimulationsReached, EvaluationFailed, Cancelled, ...). Empty when the run was refused before it started.", Access = GH_ParamAccess.item }, ParamVisibility.Default));
                result.Add(new GH_SAMParam(new Param_Integer() { Name = "simulations", NickName = "simulations", Description = "Kernel simulation count: distinct Tas evaluations (cache hits and retries not counted). After a cancel it also includes the number assigned when the cancel was observed.", Access = GH_ParamAccess.item }, ParamVisibility.Default));
                result.Add(new GH_SAMParam(new Param_Number() { Name = "bestPoint", NickName = "bestPoint", Description = "Best point: parameter values in parameter order. Empty unless successful.", Access = GH_ParamAccess.list }, ParamVisibility.Default));
                result.Add(new GH_SAMParam(new Param_Number() { Name = "bestObjectives", NickName = "bestObjectives", Description = "Objective values at the best point, in objective order (the first is minimised). Empty unless successful.", Access = GH_ParamAccess.list }, ParamVisibility.Default));
                result.Add(new GH_SAMParam(new Param_String() { Name = "runDirectory", NickName = "runDirectory", Description = "Run folder: the project snapshot and one folder per Tas evaluation.", Access = GH_ParamAccess.item }, ParamVisibility.Default));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful = Params.IndexOfOutputParam("successful");
            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            index = Params.IndexOfInputParam("_run");

            bool run = false;
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!run)
            {
                return;
            }

            string path = null;
            index = Params.IndexOfInputParam("_scriptPath");
            if (index == -1 || !dataAccess.GetData(index, ref path) || string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Objective> objectives = new List<Objective>();
            index = Params.IndexOfInputParam("_objectives");
            if (index == -1 || !dataAccess.GetDataList(index, objectives) || objectives == null || objectives.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<IParameter> parameters = new List<IParameter>();
            index = Params.IndexOfInputParam("_parameters");
            if (index == -1 || !dataAccess.GetDataList(index, parameters) || parameters == null || parameters.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Algorithm algorithm = null;
            index = Params.IndexOfInputParam("_algorithm_");
            if (index == -1 || !dataAccess.GetData(index, ref algorithm) || algorithm == null)
            {
                algorithm = new GoldenSectionAlgorithm();
            }

            GenOptDocument genOptDocument = Create.GenOptDocument(path, parameters, objectives, algorithm);

            NativeGenOptReport nativeGenOptReport = Modify.RunNative(genOptDocument);

            foreach (LogRecord logRecord in nativeGenOptReport.Log)
            {
                GH_RuntimeMessageLevel gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Remark;
                if (logRecord.LogRecordType == LogRecordType.Error)
                {
                    gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Error;
                }
                else if (logRecord.LogRecordType == LogRecordType.Warning)
                {
                    gH_RuntimeMessageLevel = GH_RuntimeMessageLevel.Warning;
                }

                AddRuntimeMessage(gH_RuntimeMessageLevel, logRecord.Text);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, nativeGenOptReport.Successful);
            }

            index = Params.IndexOfOutputParam("outcome");
            if (index != -1)
            {
                dataAccess.SetData(index, nativeGenOptReport.Outcome == Math.OptimisationOutcome.Undefined ? null : nativeGenOptReport.Outcome.ToString());
            }

            index = Params.IndexOfOutputParam("simulations");
            if (index != -1)
            {
                dataAccess.SetData(index, nativeGenOptReport.Simulations);
            }

            index = Params.IndexOfOutputParam("bestPoint");
            if (index != -1)
            {
                dataAccess.SetDataList(index, nativeGenOptReport.BestPoint);
            }

            index = Params.IndexOfOutputParam("bestObjectives");
            if (index != -1)
            {
                dataAccess.SetDataList(index, nativeGenOptReport.BestObjectives);
            }

            index = Params.IndexOfOutputParam("runDirectory");
            if (index != -1)
            {
                dataAccess.SetData(index, nativeGenOptReport.RunDirectory);
            }
        }
    }
}