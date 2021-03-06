// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using TestUtilities;
using TestUtilities.UI;
using TestUtilities.UI.Python;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Thread = System.Threading.Thread;

namespace DebuggerUITests {
    public class MixedModeDebugProjectUITests {
        #region Test Cases

        /// <summary>
        /// Verify that debugging starts and a breakpoint is hit.
        /// </summary>
        public void DebugPurePythonProject(PythonVisualStudioApp app, string interpreter) {
            using (app.SelectDefaultInterpreter(FindInterpreter(interpreter))) {
                StartHelloWorldAndBreak(app);

                app.Dte.Debugger.Go(WaitForBreakOrEnd: true);
                Assert.AreEqual(dbgDebugMode.dbgDesignMode, app.Dte.Debugger.CurrentMode);
            }
        }

        #endregion

        #region Helpers

        private static void StartHelloWorldAndBreak(VisualStudioApp app) {
            DebugProjectUITests.OpenProjectAndBreak(app, @"TestData\MixedModeHelloWorld.sln", "Program.py", 1);
        }

        private static bool HasSymbols(PythonVersion interpreter) {
            if (interpreter == null) {
                return false;
            }

            // For simplicity, require the test machine to have the .pdb files
            // next to their binaries.
            return File.Exists(Path.ChangeExtension(interpreter.Configuration.InterpreterPath, "pdb"));
        }

        private static PythonVersion FindInterpreter(string pythonVersion) {
            // Examples of values for pythonVersion (match field names on PythonPaths):
            // "Python36"
            // "Python36|Python36_x64"
            var interpreter = pythonVersion.Split('|')
                .Select(v => typeof(PythonPaths).GetField(v, BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as PythonVersion)
                .Where(HasSymbols)
                .FirstOrDefault();

            if (interpreter == null) {
                Assert.Inconclusive($"Interpreter '{pythonVersion}' not installed.");
            }

            return interpreter;
        }

        #endregion
    }
}