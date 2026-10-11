// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests.Helpers
{
    /// <summary>
    /// Grasshopper components cannot be created without real Grasshopper and RhinoCommon assemblies; the NuGet
    /// RhinoCommon is a reference assembly. Tests that create one call <see cref="Require"/>, which resolves both from
    /// a Rhino 8 installation and ignores the test where there is none (the CI runner).
    /// </summary>
    public static class RhinoHost
    {
        private static readonly string[] directories =
        {
            @"C:\Program Files\Rhino 8\System",
            @"C:\Program Files\Rhino 8\Plug-ins\Grasshopper",
        };

        private static bool registered;

        /// <summary>SAM_GENOPT_TESTS_NO_RHINO=1 reproduces a runner without Rhino (as CI) on a machine that has it.</summary>
        public static bool Installed =>
            Environment.GetEnvironmentVariable("SAM_GENOPT_TESTS_NO_RHINO") != "1" &&
            File.Exists(Path.Combine(directories[0], "RhinoCommon.dll")) &&
            File.Exists(Path.Combine(directories[1], "Grasshopper.dll"));

        public static void Require()
        {
            if (!Installed)
            {
                Assert.Ignore("Rhino 8 is not installed; Grasshopper components cannot be created here.");
            }

            if (registered)
            {
                return;
            }

            registered = true;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string fileName = new AssemblyName(e.Name).Name + ".dll";
                foreach (string directory in directories)
                {
                    string path = Path.Combine(directory, fileName);
                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }

                return null;
            };
        }
    }
}
