// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SAM.Core.Grasshopper.Tas
{
    public class AssemblyInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "SAM";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "SAM Tas Toolkit";
            }
        }
        public override Guid Id
        {
            get
            {
                // Was accidentally identical to SAM.Analytical.Grasshopper.Tas's plugin Id
                // (copy-paste in the original 2020 commit that added both AssemblyInfo.cs
                // files - see git history). Regenerated here since Analytical is the
                // primary/larger TAS assembly (66 components vs. this project's 3) and
                // keeps its historical Id; this was the accidental duplicate.
                return new Guid("4ef141f3-2d5a-4a38-9c17-94fa4b3c0eb4");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Michal Dengusiak & Jakub Ziolkowski at Hoare Lea";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "Michal Dengusiak -> michaldengsiak@hoarelea.com and Jakub Ziolkowski -> jakubziolkowski@hoarelea.com";
            }
        }
    }
}
