// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt.Tests
{
    /// <summary>
    /// The built plugin assembly (SAM.Analytical.Grasshopper.Tas.GenOpt) is read as metadata: which external members it
    /// calls, which types it uses and which string literals it holds. The Grasshopper route must reach Tas only through
    /// GenOptDocument.RunNative.
    /// </summary>
    [TestFixture]
    public class NoJavaRouteTests
    {
        private HashSet<string> memberReferences;
        private HashSet<string> typeReferences;
        private List<string> userStrings;

        [OneTimeSetUp]
        public void Read()
        {
            string path = typeof(NativeGenOptReport).Assembly.Location;
            memberReferences = new HashSet<string>(StringComparer.Ordinal);
            typeReferences = new HashSet<string>(StringComparer.Ordinal);
            userStrings = new List<string>();

            using (FileStream fileStream = File.OpenRead(path))
            using (PEReader peReader = new PEReader(fileStream))
            {
                MetadataReader reader = peReader.GetMetadataReader();

                foreach (TypeReferenceHandle handle in reader.TypeReferences)
                {
                    typeReferences.Add(Name(reader, handle));
                }

                foreach (MemberReferenceHandle handle in reader.MemberReferences)
                {
                    MemberReference memberReference = reader.GetMemberReference(handle);
                    string parent = memberReference.Parent.Kind == HandleKind.TypeReference ? Name(reader, (TypeReferenceHandle)memberReference.Parent) : memberReference.Parent.Kind.ToString();
                    memberReferences.Add(parent + "::" + reader.GetString(memberReference.Name));
                }

                UserStringHandle userStringHandle = MetadataTokens.UserStringHandle(1);
                while (!userStringHandle.IsNil)
                {
                    userStrings.Add(reader.GetUserString(userStringHandle));
                    userStringHandle = reader.GetNextHandle(userStringHandle);
                }
            }
        }

        private static string Name(MetadataReader reader, TypeReferenceHandle handle)
        {
            TypeReference typeReference = reader.GetTypeReference(handle);
            string name = reader.GetString(typeReference.Name);
            if (typeReference.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return Name(reader, (TypeReferenceHandle)typeReference.ResolutionScope) + "+" + name;
            }

            return reader.GetString(typeReference.Namespace) + "." + name;
        }

        [Test]
        public void CallsRunNative()
        {
            Assert.That(memberReferences, Does.Contain("SAM.Analytical.Tas.GenOpt.GenOptDocument::RunNative"));
        }

        [TestCase("SAM.Analytical.Tas.GenOpt.GenOptDocument::Run")]
        [TestCase("SAM.Analytical.Tas.GenOpt.GenOptDocument::get_ExecutableFile")]
        [TestCase("SAM.Analytical.Tas.GenOpt.GenOptDocument::set_Command")]
        [TestCase("SAM.Analytical.Tas.GenOpt.GenOptDocument::GetPath")]
        [TestCase("SAM.Analytical.Tas.GenOpt.Query::TasGenOptJavaPath")]
        [TestCase("SAM.Analytical.Tas.GenOpt.Create::Command")]
        [TestCase("SAM.Core.Tas.Modify::SetProjectDirectory")]
        public void NeverCallsTheJavaRoute(string member)
        {
            Assert.That(memberReferences, Does.Not.Contain(member));
        }

        [TestCase("System.Diagnostics.Process")]
        [TestCase("System.Diagnostics.ProcessStartInfo")]
        [TestCase("System.IO.FileSystemWatcher")]
        [TestCase("Microsoft.Win32.Registry")]
        [TestCase("Microsoft.Win32.RegistryKey")]
        [TestCase("SAM.Analytical.Tas.GenOpt.ExecutableFile")]
        public void UsesNoProcessWatcherOrRegistry(string type)
        {
            Assert.That(typeReferences, Does.Not.Contain(type));
        }

        [TestCase("java.exe")]
        [TestCase("javaw")]
        [TestCase(".jar")]
        [TestCase(".bat")]
        [TestCase("cmd")]
        [TestCase("OutputListing")]
        [TestCase("GenOpt.log")]
        [TestCase("Config.ini")]
        [TestCase("TasOutputs.txt")]
        public void HoldsNoJavaOrGenOptFileName(string text)
        {
            Assert.That(userStrings.Where(x => x.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0), Is.Empty);
        }
    }
}
