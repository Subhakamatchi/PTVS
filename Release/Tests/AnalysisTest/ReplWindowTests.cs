﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using AnalysisTest.UI;
using Microsoft.PythonTools.Options;
using Microsoft.TC.TestHostAdapters;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Repl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using TestUtilities;
using Keyboard = AnalysisTest.UI.Keyboard;

namespace AnalysisTest {
    [TestClass]
    [DeploymentItem(@"Python.VS.TestData\TestImage.png")]
    [DeploymentItem(@"Python.VS.TestData\TestScript.txt")]
    public class ReplWindowTests {

        /// <summary>
        /// “def f(): pass” + 2 ENTERS
        /// f( should bring signature help up
        /// 
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void SimpleSignatureHelp() {
            var interactive = Prepare();

            Assert.AreNotEqual(null, interactive);

            const string code = "def f(): pass";
            Keyboard.Type(code + "\r\r");

            interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt);

            Keyboard.Type("f(");

            interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt + "f(");

            interactive.WaitForSession<ISignatureHelpSession>();

            Keyboard.PressAndRelease(Key.Escape);

            interactive.WaitForSessionDismissed();
        }

        /// <summary>
        /// “x = 42”
        /// “x.” should bring up intellisense completion
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void SimpleCompletion() {
            var interactive = Prepare();

            const string code = "x = 42";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);

            Keyboard.Type("x.");

            interactive.WaitForText(ReplPrompt + code,  ReplPrompt + "x.");

            var session = interactive.WaitForSession<ICompletionSession>();
            
            StringBuilder completions = new StringBuilder();
            completions.AppendLine(session.SelectedCompletionSet.DisplayName);
            
            foreach (var completion in session.SelectedCompletionSet.Completions) {
                completions.Append(completion.InsertionText);
            }

            string x = completions.ToString();

            // commit entry
            Keyboard.PressAndRelease(Key.Tab);
            interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.conjugate");
            interactive.WaitForSessionDismissed();

            // clear input at repl
            Keyboard.PressAndRelease(Key.Escape);

            // try it again, and dismiss the session
            Keyboard.Type("x.");
            interactive.WaitForSession<ICompletionSession>();

            Keyboard.PressAndRelease(Key.Escape);

            interactive.WaitForSessionDismissed();
        }

        /// <summary>
        /// “x = 42”
        /// “x “ should not being up any completions.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void SimpleCompletionSpaceNoCompletion() {
            var interactive = Prepare();

            const string code = "x = 42";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);

            // x<space> should not bring up a completion session
            Keyboard.Type("x ");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x ");

            interactive.WaitForSessionDismissed();
        }

        /// <summary>
        /// Pasting CSV data
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CsvPaste() {
            var interactive = Prepare();

            ((UIElement)interactive.TextView).Dispatcher.Invoke((Action)(() => {
                var dataObject = new System.Windows.DataObject();
                dataObject.SetText("foo");
                var stream = new MemoryStream(UTF8Encoding.Default.GetBytes("\"abc,\",\"foo\",\"\"\"foo,\"\"\",bar,baz\"x\"bar,\"baz,\"\"x,\"\"bar\",,    ,bar,\",\"\",\"\"\",baz\"x\"'bar,\"baz\"\"x\"\"',bar\",\"\"\"\",\"\"\",\"\"\",\",\",\\\r\n1,2,3,4,9,10,11,12,13,19,33,22,,,,,,\r\n4,5,6,5,2,3,4,3,1,20,44,33,,,,,,\r\n7,8,9,6,3,4,0,9,4,33,55,33,,,,,,"));
                dataObject.SetData(DataFormats.CommaSeparatedValue, stream);
                Clipboard.SetDataObject(dataObject, true);
            }));
            
            Keyboard.ControlV();

            string line1 = "[";
            string line2 = "  ['abc,', '\"foo\"', '\"foo,\"', 'bar', 'baz\"x\"bar', 'baz,\"x,\"bar', None, None, 'bar', ',\",\"', 'baz\"x\"\\'bar', 'baz\"x\"\\',bar', '\"\"\"\"', '\",\"', ',', '\\\\'],";
            string line3 = "  [1, 2, 3, 4, 9, 10, 11, 12, 13, 19, 33, 22, None, None, None, None, None, None],";
            string line4 = "  [4, 5, 6, 5, 2, 3, 4, 3, 1, 20, 44, 33, None, None, None, None, None, None],";
            string line5 = "  [7, 8, 9, 6, 3, 4, 0, 9, 4, 33, 55, 33, None, None, None, None, None, None],";
            string line6 = "]";
            
            interactive.WaitForText(
                ReplPrompt + line1, 
                SecondPrompt + line2,
                SecondPrompt + line3,
                SecondPrompt + line4,
                SecondPrompt + line5,
                SecondPrompt + line6,
                SecondPrompt);
        }

        /// <summary>
        /// x = 42; x.car[enter] – should type “car” not complete to “conjugate”
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CompletionWrongText() {
            var interactive = Prepare();

            const string code = "x = 42";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);

            // x<space> should not bring up a completion session
            Keyboard.Type("x.");
            interactive.WaitForSession<ICompletionSession>();
            Keyboard.Type("car");
            Keyboard.Type(Key.Enter);

            interactive.WaitForSessionDismissed();
            interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.car", "Traceback (most recent call last):", "  File \"<stdin>\", line 1, in <module>", "AttributeError: 'int' object has no attribute 'car'", ReplPrompt);
        }

        /// <summary>
        /// x = 42; x.conjugate[enter] – should respect enter completes option, and should respect enter at end of word 
        /// completes option.  When it does execute the text the output should be on the next line.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CompletionFullText() {
            var interactive = Prepare();
            var options = (IPythonOptions)VsIdeTestHostContext.Dte.GetObject("PythonOptions");
            options.Intellisense.AddNewLineAtEndOfFullyTypedWord = false;

            const string code = "x = 42";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);            

            // x<space> should not bring up a completion session
            Keyboard.Type("x.");
            interactive.WaitForSession<ICompletionSession>();
            Keyboard.Type("real");
            Keyboard.Type(Key.Enter);

            interactive.WaitForSessionDismissed();
            interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.real");

            // try again w/ option flipped
            interactive.ClearScreen();
            interactive.WaitForText(ReplPrompt);

            options.Intellisense.AddNewLineAtEndOfFullyTypedWord = true;

            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);

            Keyboard.Type("x.");
            interactive.WaitForSession<ICompletionSession>();
            Keyboard.Type("real");
            Keyboard.Type(Key.Enter);

            interactive.WaitForSessionDismissed();
            interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.real", "42", ReplPrompt);
        }


        /// <summary>
        /// Enter in a middle of a line should insert new line
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EnterInMiddleOfLine() {
            var interactive = Prepare();

            const string code = "def f(): #foo";
            Keyboard.Type(code);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Enter);
            Keyboard.Type(Key.Tab);
            Keyboard.Type("pass");
            Keyboard.PressAndRelease(Key.Enter, Key.LeftCtrl);
            interactive.WaitForText(ReplPrompt + "def f(): ", SecondPrompt + "    pass#foo", ReplPrompt);
        }

        /// <summary>
        /// Escape should clear both lines
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EscapeClearsMultipleLines() {
            var interactive = Prepare();

            const string code = "def f(): #foo";
            Keyboard.Type(code);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Enter);
            Keyboard.Type(Key.Tab);
            Keyboard.Type("pass");
            Keyboard.Type(Key.Escape);
            interactive.WaitForText(ReplPrompt);
        }

        /// <summary>
        /// “x=42” left left ctrl-enter should commit assignment
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlEnterCommits() {
            var interactive = Prepare();

            const string code = "x = 42";
            Keyboard.Type(code);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.PressAndRelease(Key.Enter, Key.LeftCtrl);
            interactive.WaitForText(ReplPrompt + "x = 42", ReplPrompt);
        }

        /// <summary>
        /// while True: pass / Right Click -> Break Execution (or Ctrl-Break) should break execution
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlBreakInterrupts() {
            var interactive = Prepare();

            const string code = "while True: pass\r\n";
            Keyboard.Type(code);
            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "");

            interactive.CancelExecution();

            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "Traceback (most recent call last):", "  File \"<stdin>\", line 1, in <module>", "KeyboardInterrupt", ReplPrompt);
        }

        /// <summary>
        /// Ctrl-Break while running should result in a new prompt
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlBreakNotRunning() {
            var interactive = Prepare();

            interactive.WaitForText(ReplPrompt);

            interactive.CancelExecution();

            interactive.WaitForText(ReplPrompt, ReplPrompt);
        }

        /// <summary>
        /// while True: pass / Right Click -> Break Execution (or Ctrl-Break) should break execution
        /// 
        /// This version runs for 1/2 second which kicks in the running UI.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlBreakInterruptsLongRunning() {
            var interactive = Prepare();

            const string code = "while True: pass\r\n";
            Keyboard.Type(code);
            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "");

            System.Threading.Thread.Sleep(500);

            interactive.CancelExecution();

            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "Traceback (most recent call last):", "  File \"<stdin>\", line 1, in <module>", "KeyboardInterrupt", ReplPrompt);
        }

        /// <summary>
        /// Ctrl-Enter on previous input should paste input to end of buffer (doing it again should paste again – appending onto previous input)
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlEnterOnPreviousInput() {
            var interactive = Prepare();

            const string code = "def f(): pass";
            Keyboard.Type(code + "\r\r");

            interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt);

            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Right);
            Keyboard.PressAndRelease(Key.Enter, Key.LeftCtrl);

            interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt + code);

            Keyboard.PressAndRelease(Key.Escape);

            interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt);
        }

        /// <summary>
        /// Type some text, hit Ctrl-Enter, should execute current line
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlEnterForceCommit() {
            var interactive = Prepare();

            const string code = "def f(): pass";
            Keyboard.Type(code);

            interactive.WaitForText(ReplPrompt + code);

            Keyboard.PressAndRelease(Key.Enter, Key.LeftCtrl);

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);
        }

        /// <summary>
        /// Type a function definition, go to next line, type pass, navigate left, hit ctrl-enter, should immediately execute func def.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CtrlEnterMultiLineForceCommit() {
            var interactive = Prepare();

            const string code = "def f():";
            Keyboard.Type(code);
            Keyboard.Type(Key.Enter);
            Keyboard.Type("pass");

            interactive.WaitForText(ReplPrompt + code, SecondPrompt + "    pass");

            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Left);
            Keyboard.PressAndRelease(Key.Enter, Key.LeftCtrl);

            interactive.WaitForText(ReplPrompt + code, SecondPrompt + "    pass", ReplPrompt);
        }

        /// <summary>
        /// Define function “def f():\r\n    print ‘hi’”, scroll back up to history, add print “hello” to 2nd line, enter, 
        /// scroll back through both function definitions
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void HistoryUpdateDef() {
            var interactive = Prepare();

            string hiCode = "def f():\r\n" + SecondPrompt + "    print 'hi'\r\n" + SecondPrompt + "    ";
            string helloCode = "def f():\r\n" + SecondPrompt + "    print 'hello'\r\n" + SecondPrompt + "    ";
            string helloCodeNotCommitted = "def f():\r\n" + SecondPrompt + "    print 'hello'";
            string hiCodeNotCommitted = "def f():\r\n" + SecondPrompt + "    print 'hi'";
            Keyboard.Type("def f():\r");
            Keyboard.Type("print 'hi'\r\r");

            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt);

            Keyboard.Type(Key.Up);
            // delete 'hi'
            Keyboard.Type(Key.Back);
            Keyboard.Type(Key.Back);
            Keyboard.Type(Key.Back);
            Keyboard.Type(Key.Back);

            Keyboard.Type("'hello'");
            Keyboard.Type(Key.Enter);
            Keyboard.Type(Key.Enter);

            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt + helloCode, ReplPrompt);

            Keyboard.Type(Key.Up);
            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt + helloCode, ReplPrompt + helloCodeNotCommitted);

            Keyboard.Type(Key.Up);
            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt + helloCode, ReplPrompt + hiCodeNotCommitted);
        }

        /// <summary>
        /// Define function “def f():\r\n    print ‘hi’”, scroll back up to history, add print “hello” to 2nd line, enter, 
        /// scroll back through both function definitions
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void HistoryAppendDef() {
            var interactive = Prepare();

            string hiCode = "def f():\r\n" + SecondPrompt + "    print 'hi'\r\n" + SecondPrompt + "    ";
            string finalCode = hiCode + "print 'hello'\r\n" + SecondPrompt + "    ";
            Keyboard.Type("def f():\r");
            Keyboard.Type("print 'hi'\r\r");

            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt);

            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Enter);
            Keyboard.Type("print 'hello'\r\r");

            interactive.WaitForText(ReplPrompt + hiCode, ReplPrompt + finalCode, ReplPrompt);
        }

        /// <summary>
        /// Define function “def f():\r\n    print ‘hi’”, scroll back up to history, add print “hello” to 2nd line, enter, 
        /// scroll back through both function definitions
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void HistoryBackForward() {
            var interactive = Prepare();

            const string code1 = "x = 23";
            const string code2 = "y = 5";
            Keyboard.Type(code1 + "\r");

            interactive.WaitForText(ReplPrompt + code1, ReplPrompt);

            Keyboard.Type(code2 + "\r");
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2, ReplPrompt);

            Keyboard.Type(Key.Up);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2, ReplPrompt + code2);

            Keyboard.Type(Key.Up);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2, ReplPrompt + code1);

            Keyboard.Type(Key.Down);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2, ReplPrompt + code2);
        }

        /// <summary>
        /// Test that maximum length of history is enforced and stores correct items.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void HistoryMaximumLength() {
            var interactive = Prepare();

            const int historyMax = 50;

            List<string> expected = new List<string>();
            for (int i = 0; i < historyMax + 1; i++) {
                string cmd = "x = " + i;
                expected.Add(ReplPrompt + cmd);
                Keyboard.Type(cmd + "\r");

                // add the empty prompt, check, then remove it
                expected.Add(ReplPrompt);
                interactive.WaitForText(expected.ToArray());
                expected.RemoveAt(expected.Count - 1);
            }

            // add an extra item for the current input which we'll update as we go through the history
            expected.Add(ReplPrompt);
            for (int i = 0; i < historyMax; i++) {
                Keyboard.Type(Key.Up);


                expected[expected.Count - 1] = expected[expected.Count - i - 2]; 
                interactive.WaitForText(expected.ToArray());
            }
            // end of history, one more up shouldn't do anything
            Keyboard.Type(Key.Up);
            interactive.WaitForText(expected.ToArray());
        }

        /// <summary>
        /// Test that we remember a partially typed input when we move to the history.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void HistoryUncommittedInput() {
            var interactive = Prepare();

            const string code1 = "x = 42", code2 = "y = 100";
            Keyboard.Type(code1 + "\r");

            interactive.WaitForText(ReplPrompt + code1, ReplPrompt);

            // type, don't commit
            Keyboard.Type(code2);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2);

            // move away from the input
            Keyboard.Type(Key.Up);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code1);

            // move back to the input
            Keyboard.Type(Key.Down);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt + code2);

            Keyboard.Type(Key.Escape);
            interactive.WaitForText(ReplPrompt + code1, ReplPrompt);
        }
        
        /// <summary>
        /// Define function “def f():\r\n    print ‘hi’”, scroll back up to history, add print “hello” to 2nd line, enter, 
        /// scroll back through both function definitions
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void RegressionImportSysBackspace() {
            var item = (IPythonOptions)VsIdeTestHostContext.Dte.GetObject("PythonOptions");
            item.Intellisense.AddNewLineAtEndOfFullyTypedWord = true;

            var interactive = Prepare();

            const string importCode = "import sys";
            Keyboard.Type(importCode + "\r");

            interactive.WaitForText(ReplPrompt + importCode, ReplPrompt);

            Keyboard.Type("sys");
            
            interactive.WaitForText(ReplPrompt + importCode, ReplPrompt + "sys");

            Keyboard.Type(Key.Back);
            Keyboard.Type(Key.Back);

            interactive.WaitForText(ReplPrompt + importCode, ReplPrompt + "s");
            Keyboard.Type(Key.Back);

            interactive.WaitForText(ReplPrompt + importCode, ReplPrompt);
        }

        /// <summary>
        /// Enter “while True: pass”, then hit up/down arrow, should move the caret in the edit buffer
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CursorWhileCodeIsRunning() {
            var interactive = Prepare();

            const string code = "while True: pass\r\n";
            Keyboard.Type(code);
            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "");

            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Up);
            for (int i = 0; i < ReplPrompt.Length; i++) {
                Keyboard.Type(Key.Right);
            }
            
            Keyboard.PressAndRelease(Key.End, Key.LeftShift);
            Keyboard.PressAndRelease(Key.C, Key.LeftCtrl);

            System.Threading.Thread.Sleep(100);

            interactive.CancelExecution();

            interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "Traceback (most recent call last):", "  File \"<stdin>\", line 1, in <module>", "KeyboardInterrupt", ReplPrompt);

            interactive.ClearScreen();
            interactive.WaitForText(ReplPrompt);
            Keyboard.ControlV();

            interactive.WaitForText(ReplPrompt + "while True: pass");
        }

        /// <summary>
        /// Type “raise Exception()”, hit enter, raise Exception() should have appropriate syntax color highlighting.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void SyntaxHighlightingRaiseException() {
            var interactive = Prepare();

            const string code = "raise Exception()";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, "Traceback (most recent call last):", "  File \"<stdin>\", line 1, in <module>", "Exception", ReplPrompt);

            var snapshot = interactive.ReplWindow.TextView.TextBuffer.CurrentSnapshot;
            var span = new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
            var classifications = interactive.Classifier.GetClassificationSpans(span);

            Assert.AreEqual(classifications[0].ClassificationType.Classification, PredefinedClassificationTypeNames.Keyword);
            Assert.AreEqual(classifications[1].ClassificationType.Classification, PredefinedClassificationTypeNames.Identifier);
            Assert.AreEqual(classifications[2].ClassificationType.Classification, "Python open grouping");
            Assert.AreEqual(classifications[3].ClassificationType.Classification, "Python close grouping");

            Assert.AreEqual(classifications[0].Span.GetText(), "raise");
            Assert.AreEqual(classifications[1].Span.GetText(), "Exception");
            Assert.AreEqual(classifications[2].Span.GetText(), "(");
            Assert.AreEqual(classifications[3].Span.GetText(), ")");
        }

        /// <summary>
        /// Tests entering an unknown repl commmand
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void ReplCommandUnknown() {
            var interactive = Prepare();

            const string code = "$unknown";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, "Unknown command 'unknown', use \"$help\" for help", ReplPrompt);
        }


        /// <summary>
        /// Tests entering an unknown repl commmand
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void ReplCommandComment() {
            var interactive = Prepare();

            const string code = "$$ foo bar baz";
            Keyboard.Type(code + "\r");

            interactive.WaitForText(ReplPrompt + code, ReplPrompt);
        }

        /// <summary>
        /// Tests entering an unknown repl commmand
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void BackspacePrompt() {
            var interactive = Prepare();
            
            Keyboard.Type("def f():\rpass");

            interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    pass");

            for (int i = 0; i < 6; i++) {
                Keyboard.Type(Key.Back);
            }

            interactive.WaitForText(ReplPrompt + "def f():");

            Keyboard.Type("abc");

            interactive.WaitForText(ReplPrompt + "def f():abc");

            for (int i = 0; i < 3; i++) {
                Keyboard.Type(Key.Back);
            }

            interactive.WaitForText(ReplPrompt + "def f():");

            Keyboard.Type("\rpass");

            interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    pass");
        }

        /// <summary>
        /// Tests REPL command help
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void ReplCommandHelp() {
            var interactive = Prepare();

            const string code = "$help";
            Keyboard.Type(code + "\r");

            interactive.WaitForTextStart(ReplPrompt + code, String.Format("  {0,-16}  {1}", "help", "Show a list of REPL commands"));
        }

        /// <summary>
        /// Tests REPL command help
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void CommandsLoadScript() {
            var interactive = Prepare();

            string code = "$load " + Path.GetFullPath("TestScript.txt");
            Keyboard.Type(code + "\r");

            interactive.WaitForTextStart(ReplPrompt + code, 
                ReplPrompt + "print 'hello world'",
                "hello world",
                ReplPrompt
            );
        }

        /// <summary>
        /// Tests inline images
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void InlineImage() {
            var interactive = Prepare();

            const string importSys = "import sys";
            const string getReplModule = "repl = sys.modules['visualstudio_py_repl'].BACKEND";
            Keyboard.Type(importSys + "\r");
            interactive.WaitForText(ReplPrompt + importSys, ReplPrompt);

            Keyboard.Type(getReplModule + "\r");

            interactive.WaitForText(ReplPrompt + importSys, ReplPrompt + getReplModule, ReplPrompt);
            
            interactive.ClearScreen();
            interactive.WaitForText(ReplPrompt);

            string loadImage = String.Format("repl.send_image(\"{0}\")", Path.GetFullPath("TestImage.png").Replace("\\", "\\\\"));
            Keyboard.Type(loadImage + "\r");
            interactive.WaitForText(ReplPrompt + loadImage, "", "", ReplPrompt);

            // check that we got a tag inserted
            var compModel = (IComponentModel)VsIdeTestHostContext.ServiceProvider.GetService(typeof(SComponentModel));
            var aggFact = compModel.GetService<IViewTagAggregatorFactoryService>();
            var textview = interactive.ReplWindow.TextView;
            var aggregator = aggFact.CreateTagAggregator<IntraTextAdornmentTag>(textview);
            var snapshot = textview.TextBuffer.CurrentSnapshot;
            var tags = WaitForTags(textview, aggregator, snapshot);
            Assert.AreEqual(1, tags.Length);

            var size = tags[0].Tag.Adornment.RenderSize;

            // now add some more code to cause the image to minimize
            const string nopCode = "x = 2";
            Keyboard.Type(nopCode);            
            interactive.WaitForText(ReplPrompt + loadImage, "", "", ReplPrompt + nopCode);

            Keyboard.Type(Key.Enter);
            interactive.WaitForText(ReplPrompt + loadImage, "", "", ReplPrompt + nopCode, ReplPrompt);

            // let image minimize...
            System.Threading.Thread.Sleep(200);
            for (int i = 0; i < 10; i++) {
                tags = WaitForTags(textview, aggregator, snapshot); 
                Assert.AreEqual(1, tags.Length);

                var sizeTmp = tags[0].Tag.Adornment.RenderSize;
                if (sizeTmp.Height < size.Height && sizeTmp.Width < size.Width) {
                    break;
                }
                System.Threading.Thread.Sleep(200);
            }

            // make sure it's minimized
            var size2 = tags[0].Tag.Adornment.RenderSize;
            Assert.IsTrue(size2.Height < size.Height);
            Assert.IsTrue(size2.Width < size.Width);
            /*
            Point screenPoint = new Point(0, 0);
            ((UIElement)textview).Dispatcher.Invoke((Action)(() => {
                screenPoint = tags[0].Tag.Adornment.PointToScreen(new Point(10, 10));
            }));
            Mouse.MoveTo(screenPoint);

            Mouse.Click(MouseButton.Left);

            Keyboard.PressAndRelease(Key.OemPlus, Key.LeftCtrl);*/
            //Keyboard.Type(Key.Escape);
        }

        private static IMappingTagSpan<IntraTextAdornmentTag>[] WaitForTags(ITextView textView, ITagAggregator<IntraTextAdornmentTag> aggregator, ITextSnapshot snapshot) {
            IMappingTagSpan<IntraTextAdornmentTag>[] tags = null;
            ((UIElement)textView).Dispatcher.Invoke((Action)(() => {
                for (int i = 0; i < 100; i++) {
                    tags = aggregator.GetTags(new SnapshotSpan(snapshot, new Span(0, snapshot.Length))).ToArray();
                    if (tags.Length > 0) {
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }                
            }));

            return tags;
        }

        /// <summary>
        /// Tests pressing back space when to the left of the caret we have the secondary prompt.  The secondary prompt
        /// should be removed and the lines should be joined.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditDeleteSecondPrompt() {
            var interactive = Prepare();

            Keyboard.Type("def f():\rx = 42\ry = 100");

            interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    x = 42", SecondPrompt + "    y = 100");

            Keyboard.Type(Key.Home);
            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Back);

            interactive.WaitForText(ReplPrompt + "def f():    x = 42", SecondPrompt + "    y = 100");

            Keyboard.PressAndRelease(Key.Escape);

            interactive.WaitForText(ReplPrompt);
        }

        /// <summary>
        /// Tests entering a single line of text, moving to the middle, and pressing enter.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditInsertInMiddleOfLine() {
            var interactive = Prepare();

            Keyboard.Type("def f(): print 'hello'");
            interactive.WaitForText(ReplPrompt + "def f(): print 'hello'");

            // move to left of print
            for (int i = 0; i < 13; i++) {
                Keyboard.Type(Key.Left);
            }

            Keyboard.Type(Key.Enter);

            interactive.WaitForText(ReplPrompt + "def f(): ", SecondPrompt + "    print 'hello'");

            Keyboard.PressAndRelease(Key.Escape);

            interactive.WaitForText(ReplPrompt);
        }
        
        /// <summary>
        /// Tests using the $cls clear screen command
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void ClearScreenCommand() {
            var interactive = Prepare();

            Keyboard.Type("$cls");
            interactive.WaitForText(ReplPrompt + "$cls");

            Keyboard.Type(Key.Enter);

            interactive.WaitForText(ReplPrompt);
        }

        /// <summary>
        /// Tests deleting when the secondary prompt is highlighted as part of the selection
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditDeleteSecondaryPromptSelected() {
            if (SecondPrompt.Length > 0) {
                for (int i = 0; i < 2; i++) {
                    var interactive = Prepare();

                    Keyboard.Type("def f():\rprint 'hi'");
                    interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    print 'hi'");

                    Keyboard.Type(Key.Home);
                    Keyboard.Type(Key.Left);
                    Keyboard.PressAndRelease(Key.End, Key.LeftShift);
                    if (i == 1) {
                        Keyboard.Type(Key.Back);
                    } else {
                        Keyboard.Type(Key.Delete);
                    }

                    interactive.WaitForText(ReplPrompt + "def f():");

                    Keyboard.PressAndRelease(Key.Escape);

                    interactive.WaitForText(ReplPrompt);
                }
            }
        }

        /// <summary>
        /// Tests typing when the secondary prompt is highlighted as part of the selection
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditTypeSecondaryPromptSelected() {
            if (SecondPrompt.Length > 0) {
                var interactive = Prepare();

                Keyboard.Type("def f():\rprint 'hi'");
                interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    print 'hi'");

                Keyboard.Type(Key.Home);
                Keyboard.Type(Key.Left);
                Keyboard.PressAndRelease(Key.End, Key.LeftShift);
                Keyboard.Type("pass");

                interactive.WaitForText(ReplPrompt + "def f():pass");

                Keyboard.PressAndRelease(Key.Escape);

                interactive.WaitForText(ReplPrompt);
            }
        }

        /// <summary>
        /// Tests typing when the secondary prompt is highlighted as part of the selection
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditCutIncludingPrompt() {
            if (SecondPrompt.Length > 0) {
                var interactive = Prepare();

                Keyboard.Type("def f():\rprint 'hi'");
                interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    print 'hi'");

                Keyboard.Type(Key.Home);
                Keyboard.Type(Key.Left);
                Keyboard.PressAndRelease(Key.End, Key.LeftShift);
                Keyboard.PressAndRelease(Key.X, Key.LeftCtrl);

                interactive.WaitForText(ReplPrompt + "def f():");

                Keyboard.ControlV();

                interactive.WaitForText(ReplPrompt + "def f():    print 'hi'");

                Keyboard.PressAndRelease(Key.Escape);

                interactive.WaitForText(ReplPrompt);
            }
        }
        
        /// <summary>
        /// Tests pasting when the secondary prompt is highlighted as part of the selection
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void EditPasteSecondaryPromptSelected() {
            if (SecondPrompt.Length > 0) {
                var interactive = Prepare();

                var textview = interactive.ReplWindow.TextView;

                ((UIElement)textview).Dispatcher.Invoke((Action)(() => {
                    Clipboard.SetText("pass", TextDataFormat.Text);
                }));

                Keyboard.Type("def f():\rprint 'hi'");
                interactive.WaitForText(ReplPrompt + "def f():", SecondPrompt + "    print 'hi'");

                Keyboard.Type(Key.Home);
                Keyboard.Type(Key.Left);
                Keyboard.PressAndRelease(Key.End, Key.LeftShift);
                Keyboard.ControlV();

                interactive.WaitForText(ReplPrompt + "def f():pass");

                Keyboard.PressAndRelease(Key.Escape);

                interactive.WaitForText(ReplPrompt);
            }
        }

        /// <summary>
        /// Tests getting/setting the repl window options.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void ReplWindowOptions() {
            if (SecondPrompt.Length > 0) {
                var interactive = Prepare();
                var window = interactive.ReplWindow;

                window.SetOptionValue(ReplOptions.CommandPrefix, "%");
                Assert.AreEqual(window.GetOptionValue(ReplOptions.CommandPrefix), "%");
                window.SetOptionValue(ReplOptions.CommandPrefix, "$");
                Assert.AreEqual(window.GetOptionValue(ReplOptions.CommandPrefix), "$");

                Assert.AreEqual(window.GetOptionValue(ReplOptions.PrimaryPrompt), ReplPrompt);
                Assert.AreEqual(window.GetOptionValue(ReplOptions.SecondaryPrompt), SecondPrompt);

                Assert.AreEqual(window.GetOptionValue(ReplOptions.DisplayPromptInMargin), false);
                Assert.AreEqual(window.GetOptionValue(ReplOptions.ShowOutput), true);

                Assert.AreEqual(window.GetOptionValue(ReplOptions.UseSmartUpDown), true);

                AssertUtil.Throws<InvalidOperationException>(
                    () => window.SetOptionValue(ReplOptions.PrimaryPrompt, 42)
                );
                AssertUtil.Throws<InvalidOperationException>(
                    () => window.SetOptionValue(ReplOptions.PrimaryPrompt, null)
                );
                AssertUtil.Throws<InvalidOperationException>(
                    () => window.SetOptionValue((ReplOptions)(-1), null)
                );
            }
        }

        /// <summary>
        /// Calling input while executing user code.  This should let the user start typing.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestRawInput() {
            var interactive = Prepare();

            string inputCode = "x = raw_input()";
            string text = "hello";
            string printCode = "print x";
            Keyboard.Type(inputCode + "\r");
            interactive.WaitForText(ReplPrompt + inputCode, "");

            Keyboard.Type(text + "\r");
            interactive.WaitForText(ReplPrompt + inputCode, text, ReplPrompt);

            Keyboard.Type(printCode + "\r");

            interactive.WaitForText(ReplPrompt + inputCode, text, ReplPrompt + printCode, text, ReplPrompt);
        }

        /// <summary>
        /// Pressing delete with no text selected, it should delete the proceeding character.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestDelNoTextSelected() {
            var interactive = Prepare();

            string inputCode = "abc";
            Keyboard.Type(inputCode);
            interactive.WaitForText(ReplPrompt + inputCode);

            Keyboard.Type(Key.Home);
            Keyboard.Type(Key.Delete);

            interactive.WaitForText(ReplPrompt + "bc");
        }

        /// <summary>
        /// Pressing delete with no text selected, it should delete the proceeding character.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestDelAtEndOfLine() {
            var interactive = Prepare();

            string inputCode = "def f():";
            const string autoIndent = "    ";
            Keyboard.Type(inputCode);
            interactive.WaitForText(ReplPrompt + inputCode);
            Keyboard.Type("\r");
            interactive.WaitForText(ReplPrompt + inputCode, SecondPrompt + autoIndent);
            string printCode = "print 'hello'";
            Keyboard.Type(printCode);
            interactive.WaitForText(ReplPrompt + inputCode, SecondPrompt + autoIndent + printCode);

            // go to end of 1st line
            Keyboard.Type(Key.Home);
            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.End);

            // press delete
            Keyboard.Type(Key.Delete);

            interactive.WaitForText(ReplPrompt + inputCode + autoIndent + printCode);
        }

        /// <summary>
        /// Pressing delete with no text selected, it should delete the proceeding character.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestDelAtEndOfBuffer() {
            var interactive = Prepare();

            string inputCode = "def f():";
            const string autoIndent = "    ";
            Keyboard.Type(inputCode);
            interactive.WaitForText(ReplPrompt + inputCode);
            Keyboard.Type("\r");
            interactive.WaitForText(ReplPrompt + inputCode, SecondPrompt + autoIndent);
            string printCode = "print 'hello'";
            Keyboard.Type(printCode);

            interactive.WaitForText(ReplPrompt + inputCode, SecondPrompt + autoIndent + printCode);

            Keyboard.Type(Key.Delete);
            interactive.WaitForText(ReplPrompt + inputCode, SecondPrompt + autoIndent + printCode);
        }

        /// <summary>
        /// Pressing delete with no text selected, it should delete the proceeding character.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestDelInOutput() {
            var interactive = Prepare();

            string inputCode = "print 'abc'";            
            Keyboard.Type(inputCode);
            interactive.WaitForText(ReplPrompt + inputCode);
            Keyboard.Type("\r");
            interactive.WaitForText(ReplPrompt + inputCode, "abc", ReplPrompt);

            Keyboard.Type(Key.Left);
            Keyboard.Type(Key.Up);
            Keyboard.Type(Key.Delete);

            interactive.WaitForText(ReplPrompt + inputCode, "abc", ReplPrompt);
        }

        /// <summary>
        /// Calling ReadInput while no code is running - this should remove the prompt and let the user type input
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestIndirectInput() {
            var interactive = Prepare();

            string text = null;
            ThreadPool.QueueUserWorkItem(
                x => {
                    text = interactive.ReplWindow.ReadInput();
                });


            // prompt should disappear
            interactive.WaitForText();

            Keyboard.Type("abc");
            interactive.WaitForText("abc");
            Keyboard.Type("\r");
            interactive.WaitForText("abc", ReplPrompt);

            Assert.AreEqual(text, "abc\r\n");
        }

        /// <summary>
        /// Simple test case of Ipython execution mode.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestIPythonMode() {
            GetInteractiveOptions().ExecutionMode = "IPython";
            var interactive = Prepare(true);
            try {
                string assignCode = "x = 42";
                string inspectCode = "?x";
                Keyboard.Type(assignCode + "\r");
                interactive.WaitForText(ReplPrompt + assignCode, ReplPrompt);

                Keyboard.Type(inspectCode + "\r");
                interactive.WaitForText(ReplPrompt + assignCode, ReplPrompt + inspectCode, 
                    "Type:		int",
                    "Base Class:	<type 'int'>",
                    "String Form:	42",
                    "Namespace:	Interactive",
                    "Docstring:",
                    "    int(x[, base]) -> integer",
                    "",
                    "Convert a string or number to an integer, if possible.  A floating point",
                    "argument will be truncated towards zero (this does not include a string",
                    "representation of a floating point number!)  When converting a string, use",
                    "the optional base.  It is an error to supply a base when converting a",
                    "non-string.  If base is zero, the proper base is guessed based on the",
                    "string content.  If the argument is outside the integer range a",
                    "long object will be returned instead.",                    
                ReplPrompt);
            } finally {
                GetInteractiveOptions().ExecutionMode = "Standard";
                interactive.Reset();
            }
        }

        /// <summary>
        /// Simple test case of Ipython execution mode.
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void TestIPythonCtrlBreakAborts() {
            GetInteractiveOptions().ExecutionMode = "IPython";
            var interactive = Prepare(true);
            try {
                const string code = "while True: pass\r\n";
                Keyboard.Type(code);
                interactive.WaitForText(ReplPrompt + "while True: pass", SecondPrompt, "");

                System.Threading.Thread.Sleep(1000);

                interactive.CancelExecution();
                
                interactive.WaitForTextStart(ReplPrompt + "while True: pass", SecondPrompt,
                    "---------------------------------------------------------------------------",
                    "KeyboardInterrupt                         Traceback (most recent call last)");

            } finally {
                GetInteractiveOptions().ExecutionMode = "Standard";
                interactive.Reset();
            }
        }

        /// <summary>
        /// “x = 42”
        /// “x.” should bring up intellisense completion
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void IPythonSimpleCompletion() {
            GetInteractiveOptions().ExecutionMode = "IPython";
            var interactive = Prepare(true);
            try {
                const string code = "x = 42";
                Keyboard.Type(code + "\r");

                interactive.WaitForText(ReplPrompt + code, ReplPrompt);

                Keyboard.Type("x.");

                interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.");

                var session = interactive.WaitForSession<ICompletionSession>();

                StringBuilder completions = new StringBuilder();
                completions.AppendLine(session.SelectedCompletionSet.DisplayName);

                foreach (var completion in session.SelectedCompletionSet.Completions) {
                    completions.Append(completion.InsertionText);
                }

                string x = completions.ToString();

                // commit entry
                Keyboard.PressAndRelease(Key.Tab);
                interactive.WaitForText(ReplPrompt + code, ReplPrompt + "x.conjugate");
                interactive.WaitForSessionDismissed();

                // clear input at repl
                Keyboard.PressAndRelease(Key.Escape);

                // try it again, and dismiss the session
                Keyboard.Type("x.");
                interactive.WaitForSession<ICompletionSession>();

                Keyboard.PressAndRelease(Key.Escape);

                interactive.WaitForSessionDismissed();
            } finally {
                GetInteractiveOptions().ExecutionMode = "Standard";
                interactive.Reset();
            }
        }

        /// <summary>
        /// “def f(): pass” + 2 ENTERS
        /// f( should bring signature help up
        /// 
        /// </summary>
        [TestMethod, Priority(2), TestCategory("Core")]
        [HostType("TC Dynamic"), DynamicHostType(typeof(VsIdeHostAdapter))]
        public void IPythonSimpleSignatureHelp() {
            GetInteractiveOptions().ExecutionMode = "IPython";
            var interactive = Prepare(true);
            try {
                

                Assert.AreNotEqual(null, interactive);

                const string code = "def f(): pass";
                Keyboard.Type(code + "\r\r");

                interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt);

                System.Threading.Thread.Sleep(1000);

                Keyboard.Type("f(");

                interactive.WaitForText(ReplPrompt + code, SecondPrompt, ReplPrompt + "f(");

                var helpSession = interactive.WaitForSession<ISignatureHelpSession>();
                Assert.AreEqual(helpSession.SelectedSignature.Documentation, "<no docstring>");

                Keyboard.PressAndRelease(Key.Escape);

                interactive.WaitForSessionDismissed();
            } finally {
                GetInteractiveOptions().ExecutionMode = "Standard";
                interactive.Reset();
            }
        }

        /// <summary>
        /// Opens the interactive window, clears the screen.
        /// </summary>
        private InteractiveWindow Prepare(bool reset = false) {
            var app = new VisualStudioApp(VsIdeTestHostContext.Dte);

            ConfigurePrompts();
            VsIdeTestHostContext.Dte.ExecuteCommand("View.PythonInteractiveDefault");
            var interactive = app.GetInteractiveWindow("Python 2.6 Interactive");
            if (reset) {
                interactive.Reset();
                System.Threading.Thread.Sleep(1000);
            }
            app.Element.SetFocus();
            interactive.Element.SetFocus();
            interactive.ClearScreen();
            interactive.WaitForText(ReplPrompt);
            return interactive;
        }

        protected virtual string ReplPrompt {
            get {
                return ">>> ";
            }
        }
        protected virtual string SecondPrompt {
            get {
                return "... ";
            }
        }
        protected virtual void ConfigurePrompts() {
            var options = GetInteractiveOptions();

            options.InlinePrompts = true;
            options.UseInterpreterPrompts = false;
            options.PrimaryPrompt = ReplPrompt;
            options.SecondaryPrompt = SecondPrompt;
        }

        protected static IPythonInteractiveOptions GetInteractiveOptions() {
            return ((IPythonOptions)VsIdeTestHostContext.Dte.GetObject("PythonOptions")).GetInteractiveOptions("Python 2.6");
        }
    }

    [TestClass]
    public class PrimaryPromptOnlyReplWindowTests : ReplWindowTests {
        protected override string ReplPrompt {
            get {
                return "> ";
            }
        }

        protected override string SecondPrompt {
            get {
                return "";
            }
        }

        protected override void ConfigurePrompts() {
            var options = GetInteractiveOptions();
            options.InlinePrompts = true;
            options.UseInterpreterPrompts = false;
            options.PrimaryPrompt = ReplPrompt;
            options.SecondaryPrompt = SecondPrompt;
        }
    }

    [TestClass]
    public class GlyphPromptReplWindowTests : ReplWindowTests {
        protected override string ReplPrompt {
            get {
                return "";
            }
        }

        protected override string SecondPrompt {
            get {
                return "";
            }
        }

        protected override void ConfigurePrompts() {
            var options = GetInteractiveOptions();
            options.InlinePrompts = false;
            options.UseInterpreterPrompts = false;
            options.PrimaryPrompt = ">";
            options.SecondaryPrompt = "*";
        }
    }
}

