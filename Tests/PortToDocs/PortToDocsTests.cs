using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Libraries.Tests
{
    public class PortToDocsTests : BasePortTests
    {
        public PortToDocsTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        // Verifies the basic case of porting all regular fields.
        public void Port_Basic()
        {
            PortToDocs("Basic");
        }

        [Fact]
        public void Port_DontAddMissingRemarks()
        {
            PortToDocs("DontAddMissingRemarks");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name match their assembly.
        public void Port_AssemblyAndNamespaceSame()
        {
            PortToDocs("AssemblyAndNamespaceSame");
        }

        [Fact]
        // Verifies porting of APIs living in namespaces whose name does not match their assembly.
        public void Port_AssemblyAndNamespaceDifferent()
        {
            PortToDocs("AssemblyAndNamespaceDifferent",
                assemblyName: "MyAssembly",
                namespaceName: "MyNamespace");
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // No interface strings should be ported.
        public void Port_Remarks_NoEII_NoInterfaceRemarks()
        {
            PortToDocs("Remarks_NoEII_NoInterfaceRemarks",
                skipInterfaceImplementations: true,
                skipInterfaceRemarks: true);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message and interface method remarks.
        public void Port_Remarks_WithEII_WithInterfaceRemarks()
        {
            PortToDocs("Remarks_WithEII_WithInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: false);
        }

        [Fact]
        // Ports Type remarks from IntelliSense xml.
        // Ports Method remarks from IntelliSense xml.
        // Ports EII message but no interface method remarks.
        public void Port_Remarks_WithEII_NoInterfaceRemarks()
        {
            PortToDocs("Remarks_WithEII_NoInterfaceRemarks",
                skipInterfaceImplementations: false,
                skipInterfaceRemarks: true);
        }

        [Fact]
        /// Verifies that new exceptions are ported.
        public void Port_Exceptions()
        {
            PortToDocs("Exceptions");
        }

        [Fact]
        /// Verifies that when an exception has already been ported, but went through
        /// language review, does not get ported if its above the difference threshold.
        public void Port_Exception_ExistingCref()
        {
            PortToDocs("Exception_ExistingCref",
                portExceptionsExisting: true,
                exceptionCollisionThreshold: 60);
        }

        private void PortToDocs(
            string testDataDir,
            bool disablePrompts = true,
            bool printUndoc = false,
            bool save = true,
            bool skipInterfaceImplementations = true,
            bool skipInterfaceRemarks = true,
            bool portTypeRemarks = true,
            bool portMemberRemarks = true,
            bool portExceptionsExisting = false,
            int exceptionCollisionThreshold = 70,
            string assemblyName = TestData.TestAssembly,
            string namespaceName = null, // Most namespaces have the same assembly name
            string typeName = TestData.TestType)
        {
            using TestDirectory tempDir = new TestDirectory();

            PortToDocsTestData testData = new PortToDocsTestData(
                tempDir,
                testDataDir,
                skipInterfaceImplementations: skipInterfaceImplementations,
                assemblyName: assemblyName,
                namespaceName: namespaceName,
                typeName: typeName
            );

            Configuration c = new()
            {
                Direction = Configuration.PortingDirection.ToDocs,
                DisablePrompts = disablePrompts,
                ExceptionCollisionThreshold = exceptionCollisionThreshold,
                PortExceptionsExisting = portExceptionsExisting,
                PortMemberRemarks = portMemberRemarks,
                PortTypeRemarks = portTypeRemarks,
                PrintUndoc = printUndoc,
                Save = save,
                SkipInterfaceImplementations = skipInterfaceImplementations,
                SkipInterfaceRemarks = skipInterfaceRemarks
            };

            c.IncludedAssemblies.Add(assemblyName);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                c.IncludedNamespaces.Add(namespaceName);
            }

            c.DirsDocsXml.Add(testData.DocsDir);
            c.DirsIntelliSense.Add(testData.IntelliSenseAndDLLDir);

            var porter = new ToDocsPorter(c);
            porter.Start();

            Verify(testData);
        }

        private void Verify(PortToDocsTestData testData)
        {
            string[] expectedLines = File.ReadAllLines(testData.ExpectedFilePath);
            string[] actualLines = File.ReadAllLines(testData.ActualFilePath);

            for (int i = 0; i < expectedLines.Length; i++)
            {
                Assert.True(i < expectedLines.Length);
                Assert.True(i < actualLines.Length);

                string expectedLine = expectedLines[i];
                string actualLine = actualLines[i];

                // Print some more details before asserting
                if (expectedLine != actualLine)
                {
                    string expected = GetProblematicLines("Expected", expectedLines, i);
                    string actual = GetProblematicLines("Actual", actualLines, i);

                    Output.WriteLine(expected);
                    Output.WriteLine(actual);
                }

                Assert.Equal(expectedLine, actualLine);
            }

            // Check at the end, because we first want to fail on different lines
            Assert.Equal(expectedLines.Length, actualLines.Length);
        }


        private static string GetProblematicLines(string title, string[] lines, int lineNumber)
        {
            string output = $"{title}:{Environment.NewLine}";

            for (int i = 5; i >= 1; i--)
            {
                if ((lineNumber - i) >= 0)
                {
                    output += $"[{lineNumber - i}] {lines[lineNumber - i]}{Environment.NewLine}";
                }
            }

            output += $"[{lineNumber}] {lines[lineNumber]}{Environment.NewLine}";

            for (int i = 1; i <= 5; i++)
            {
                if ((lineNumber + i) < lines.Length)
                {
                    output += $"[{lineNumber + i}] {lines[lineNumber + i]}{Environment.NewLine}";
                }
            }

            return output;
        }
    }
}
