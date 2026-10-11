// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.GenOpt;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public static partial class Create
    {
        /// <summary>
        /// The document the SAMAnalytical.GenOpt component runs, built from its inputs exactly as the component has
        /// always built it: the workspace is the script's folder, the script is the file's text, and a missing
        /// algorithm means the default <see cref="GoldenSectionAlgorithm"/>. Nothing is converted or validated here;
        /// <see cref="Analytical.Tas.GenOpt.GenOptDocument.RunNative"/> does that.
        /// </summary>
        public static GenOptDocument GenOptDocument(string scriptPath, IEnumerable<IParameter> parameters, IEnumerable<Objective> objectives, Algorithm algorithm)
        {
            GenOptDocument result = new GenOptDocument(System.IO.Path.GetDirectoryName(scriptPath));
            result.Algorithm = algorithm ?? new GoldenSectionAlgorithm();
            result.AddScript(System.IO.File.ReadAllText(scriptPath));

            if (objectives != null)
            {
                foreach (Objective objective in objectives)
                {
                    result.AddObjective(objective);
                }
            }

            if (parameters != null)
            {
                foreach (IParameter parameter in parameters)
                {
                    result.AddParameter(parameter);
                }
            }

            return result;
        }
    }
}
