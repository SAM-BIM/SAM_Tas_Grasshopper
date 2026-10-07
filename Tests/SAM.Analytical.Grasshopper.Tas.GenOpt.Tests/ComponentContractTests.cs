// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using NUnit.Framework;
using SAM.Analytical.Grasshopper.Tas.GenOpt.Tests.Helpers;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests
{
    /// <summary>
    /// The contract of SAMAnalytical.GenOpt: identity and input/output order. Needs Rhino 8; ignored elsewhere (see
    /// RhinoHost). Grasshopper types are only touched after the one-time set-up has registered the Rhino assembly
    /// resolver.
    /// <para>
    /// Reading saved components is deliberately not tested here: outside Rhino, Grasshopper's component server cannot
    /// load (rhcommon_c), shows a modal loading error and reads every parameter back as Param_GenericObject. Opening
    /// definitions saved by the previous plugin is part of the real Rhino/Grasshopper acceptance instead.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ComponentContractTests
    {
        [OneTimeSetUp]
        public void RequireRhino()
        {
            RhinoHost.Require();
        }

        private static GH_Component Component()
        {
            return new SAMAnalyticalGenOpt();
        }

        private static List<string> Signature(IEnumerable<IGH_Param> parameters)
        {
            return parameters.Select(x => x.Name + "|" + x.NickName + "|" + x.GetType().FullName + "|" + x.Access + "|" + x.Optional).ToList();
        }

        [Test]
        public void Identity_IsUnchanged()
        {
            GH_Component component = Component();

            Assert.That(component.ComponentGuid, Is.EqualTo(new Guid("5259b075-7da6-4d20-8364-67225c43dc4c")));
            Assert.That(component.Name, Is.EqualTo("SAMAnalytical.GenOpt"));
            Assert.That(component.NickName, Is.EqualTo("SAMAnalytical.GenOpt"));
            Assert.That(component.Category, Is.EqualTo("SAM WIP"));
            Assert.That(component.SubCategory, Is.EqualTo("Tas"));
            Assert.That(component.Exposure, Is.EqualTo(GH_Exposure.tertiary));
            Assert.That(((IGH_SAMComponent)component).LatestComponentVersion, Is.EqualTo("1.0.2"));
        }

        [Test]
        public void Inputs_AreUnchanged()
        {
            Assert.That(Signature(Component().Params.Input), Is.EqualTo(new[]
            {
                "_scriptPath|_scriptPath|Grasshopper.Kernel.Parameters.Param_FilePath|item|False",
                "_parameters|_parameters|SAM.Analytical.Grasshopper.Tas.GenOpt.GooParameterParam|list|False",
                "_objectives|_objectives|SAM.Analytical.Grasshopper.Tas.GenOpt.GooObjectiveParam|list|False",
                "_algorithm_|_algorithm_|SAM.Analytical.Grasshopper.Tas.GenOpt.GooAlgorithmParam|item|True",
                "_run|_run|Grasshopper.Kernel.Parameters.Param_Boolean|item|False",
            }));
        }

        [Test]
        public void Outputs_KeepSuccessfulFirstAndAppendTheNativeResult()
        {
            Assert.That(Signature(Component().Params.Output), Is.EqualTo(new[]
            {
                "successful|successful|Grasshopper.Kernel.Parameters.Param_Boolean|item|False",
                "outcome|outcome|Grasshopper.Kernel.Parameters.Param_String|item|False",
                "simulations|simulations|Grasshopper.Kernel.Parameters.Param_Integer|item|False",
                "bestPoint|bestPoint|Grasshopper.Kernel.Parameters.Param_Number|list|False",
                "bestObjectives|bestObjectives|Grasshopper.Kernel.Parameters.Param_Number|list|False",
                "runDirectory|runDirectory|Grasshopper.Kernel.Parameters.Param_String|item|False",
            }));
        }

        [Test]
        public void ComponentWithOnlyTheOldOutput_CanAddTheNewOutputsButNoInput()
        {
            // The parameter set of a component placed by the previous plugin: the same inputs, "successful" only.
            GH_Component component = Component();
            while (component.Params.Output.Count > 1)
            {
                component.Params.UnregisterOutputParameter(component.Params.Output[component.Params.Output.Count - 1]);
            }

            // CreateParameter is not called: it clones through Grasshopper's component server, which cannot start
            // outside Rhino and shows a modal loading error. The real acceptance adds the outputs inside Rhino.
            IGH_VariableParameterComponent variable = (IGH_VariableParameterComponent)component;
            Assert.That(variable.CanInsertParameter(GH_ParameterSide.Output, 1), Is.True);
            Assert.That(variable.CanRemoveParameter(GH_ParameterSide.Output, 0), Is.False, "successful stays mandatory.");
            Assert.That(variable.CanInsertParameter(GH_ParameterSide.Input, 5), Is.False, "No input can be added.");
        }
    }
}
