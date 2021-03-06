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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Input;
using EnvDTE;
using Microsoft.TC.TestHostAdapters;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnalysisTest.UI {
    /// <summary>
    /// Provides wrappers for automating the VisualStudio UI.
    /// </summary>
    class VisualStudioApp : AutomationWrapper {
        private SolutionExplorerTree _solutionExplorerTreeView;
        private ObjectBrowser _objectBrowser;
        private readonly DTE _dte;

        public VisualStudioApp(DTE dte)
            : this(new IntPtr(dte.MainWindow.HWnd)) {
            _dte = dte;
        }

        private VisualStudioApp(IntPtr windowHandle)
            : base(AutomationElement.FromHandle(windowHandle)) {
        }

        /// <summary>
        /// File->Save
        /// </summary>
        public void SaveSelection() {
            Dte.ExecuteCommand("File.SaveSelectedItems");
        }

        /// <summary>
        /// Opens and activates the solution explorer window.
        /// </summary>
        public void OpenSolutionExplorer() {
            Dte.ExecuteCommand("View.SolutionExplorer");            
        }

        /// <summary>
        /// Opens and activates the object browser window.
        /// </summary>
        public void OpenObjectBrowser()
        {
            Dte.ExecuteCommand("View.ObjectBrowser");
        }

        public SaveDialog SaveAs() {
            ThreadPool.QueueUserWorkItem(x => Dte.ExecuteCommand("File.SaveSelectedItemsAs"));
            return new SaveDialog(WaitForDialog());
        }

        /// <summary>
        /// Gets the specified document.  Filename should be fully qualified filename.
        /// </summary>
        public EditorWindow GetDocument(string filename) {
            Debug.Assert(Path.IsPathRooted(filename));

            string windowName = Path.GetFileName(filename);
            var elem = Element.FindFirst(TreeScope.Descendants, 
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.ClassNameProperty,
                        "TabItem"
                    ),
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        windowName
                    )
                )
            );

            elem = elem.FindFirst(TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ClassNameProperty,
                    "WpfTextView"
                )
            );

            return new EditorWindow(filename, elem);
        }

        /// <summary>
        /// Selects the given source control provider.  Name merely needs to be enough text to disambiguate from other source control providers.
        /// </summary>
        public void SelectSourceControlProvider(string providerName) {
            Element.SetFocus();

            // bring up Tools->Options
            ThreadPool.QueueUserWorkItem(x => Dte.ExecuteCommand("Tools.Options"));

            // wait for it...
            IntPtr dialog = WaitForDialog();

            // go to the tree view which lets us select a set of options...
            var treeView = new TreeView(AutomationElement.FromHandle(dialog).FindFirst(TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ClassNameProperty,
                    "SysTreeView32")
                ));
            
            treeView.FindItem("Source Control", "Plug-in Selection").SetFocus();

            var currentSourceControl = new ComboBox(
                AutomationElement.FromHandle(dialog).FindFirst(
                    TreeScope.Descendants,
                    new AndCondition(
                       new PropertyCondition(
                           AutomationElement.NameProperty,
                           "Current source control plug-in:"
                       ),
                       new PropertyCondition(
                           AutomationElement.ClassNameProperty,
                           "ComboBox"
                       )
                    )
                )
            );

            currentSourceControl.SelectItem(providerName);

            Keyboard.PressAndRelease(Key.Enter);
            WaitForDialogDismissed();
        }

        public NewProjectDialog FileNewProject() {
            ThreadPool.QueueUserWorkItem(x => Dte.ExecuteCommand("File.NewProject"));
            IntPtr dialog = WaitForDialog();
            return new NewProjectDialog(AutomationElement.FromHandle(dialog));
        }

        /// <summary>
        /// Waits for a modal dialog to take over VS's main window and returns the HWND for the dialog.
        /// </summary>
        /// <returns></returns>
        public IntPtr WaitForDialog() {
            IVsUIShell uiShell = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
            IntPtr hwnd;
            uiShell.GetDialogOwnerHwnd(out hwnd);

            for (int i = 0; i < 100 && hwnd.ToInt32() == Dte.MainWindow.HWnd; i++) {
                System.Threading.Thread.Sleep(100);
                uiShell.GetDialogOwnerHwnd(out hwnd);
            }

            Assert.AreNotEqual(hwnd.ToInt32(), Dte.MainWindow.HWnd);
            return hwnd;
        }

        /// <summary>
        /// Waits for a modal dialog to take over VS's main window and returns the HWND for the dialog.
        /// </summary>
        /// <returns></returns>
        public IntPtr WaitForDialogDismissed() {
            IVsUIShell uiShell = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
            IntPtr hwnd;
            uiShell.GetDialogOwnerHwnd(out hwnd);

            for (int i = 0; i < 100 && hwnd.ToInt32() != Dte.MainWindow.HWnd; i++) {
                System.Threading.Thread.Sleep(100);
                uiShell.GetDialogOwnerHwnd(out hwnd);
            }

            Assert.AreEqual(hwnd.ToInt32(), Dte.MainWindow.HWnd);
            return hwnd;
        }

        public static void HandleException(ExceptionButton button, params string[] text) {
            CheckAndDismissDialog(text,
                NativeMethods.IDC_EXCEPTION_TEXT,
                button == ExceptionButton.Break ? NativeMethods.IDC_BREAK : NativeMethods.IDC_CONTINUE);
        }

        internal static void CheckMessageBox(params string[] text) {
            CheckMessageBox(MessageBoxButton.Cancel, text);
        }

        internal static void CheckMessageBox(MessageBoxButton button, params string[] text) {
            CheckAndDismissDialog(text, 65535, new IntPtr((int)button));
        }

        /// <summary>
        /// Checks the text of a dialog and dismisses it.
        /// 
        /// dlgField is the field to check the text of.
        /// buttonId is the button to press to dismiss.
        /// </summary>
        private static void CheckAndDismissDialog(string[] text, int dlgField, IntPtr buttonId) {
            IVsUIShell uiShell = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
            IntPtr hwnd;
            uiShell.GetDialogOwnerHwnd(out hwnd);

            for (int i = 0; i < 100 && hwnd.ToInt32() == VsIdeTestHostContext.Dte.MainWindow.HWnd; i++) {
                System.Threading.Thread.Sleep(100);
                uiShell.GetDialogOwnerHwnd(out hwnd);
            }

            Assert.IsTrue(hwnd.ToInt32() != VsIdeTestHostContext.Dte.MainWindow.HWnd && hwnd != IntPtr.Zero);
            StringBuilder title = new StringBuilder(4096);
            Assert.AreNotEqual(NativeMethods.GetDlgItemText(hwnd, dlgField, title, title.Capacity), (uint)0);

            string t = title.ToString();
            foreach (string expected in text) {
                Assert.IsTrue(t.Contains(expected));
            }
            NativeMethods.EndDialog(hwnd, buttonId);
        }

        /// <summary>
        /// Provides access to Visual Studio's solution explorer tree view.
        /// </summary>
        public SolutionExplorerTree SolutionExplorerTreeView {
            get {
                if (_solutionExplorerTreeView == null) {
                    AutomationElement element = null;
                    for (int i = 0; i < 10 && element == null; i++) {
                        element = Element.FindFirst(TreeScope.Descendants,
                            new AndCondition(
                                new PropertyCondition(
                                    AutomationElement.ClassNameProperty,
                                    "SysTreeView32"
                                ),
                                new PropertyCondition(
                                    AutomationElement.NameProperty,
                                    "Solution Explorer"
                                )
                            )
                        );
                        if (element == null) {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                    _solutionExplorerTreeView = new SolutionExplorerTree(element);
                }
                return _solutionExplorerTreeView;
            }
        }

        /// <summary>
        /// Provides access to Visual Studio's object browser.
        /// </summary>
        public ObjectBrowser ObjectBrowser
        {
            get
            {
                if (_objectBrowser == null)
                {
                    AutomationElement element = null;
                    for (int i = 0; i < 10 && element == null; i++)
                    {
                        element = Element.FindFirst(TreeScope.Descendants,
                            new AndCondition(
                                new PropertyCondition(
                                    AutomationElement.ClassNameProperty,
                                    "ViewPresenter"
                                ),
                                new PropertyCondition(
                                    AutomationElement.NameProperty,
                                    "Object Browser"
                                )
                            )
                        );
                        if (element == null)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                    _objectBrowser = new ObjectBrowser(element);
                }
                return _objectBrowser;
            }
        }
        
        public InteractiveWindow GetInteractiveWindow(string title) {
            string autoId = GetName(title);
            AutomationElement element = null;
            for (int i = 0; i < 5 && element == null; i++) {
                element = Element.FindFirst(TreeScope.Descendants,
                        new AndCondition(
                            new PropertyCondition(
                                AutomationElement.AutomationIdProperty,
                                autoId
                            ),
                            new PropertyCondition(
                                AutomationElement.ClassNameProperty,
                                ""
                            )
                        )
                    );
                if (element == null) {
                    System.Threading.Thread.Sleep(100);
                }
            }

            return new InteractiveWindow(
                title,
                element.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(
                        AutomationElement.AutomationIdProperty,
                        "WpfTextView"
                    )
                )
            );
            
        }

        /// <summary>
        /// Produces a name which is compatible with x:Name requirements (starts with a letter/underscore, contains
        /// only letter, numbers, or underscores).
        /// </summary>
        private static string GetName(string title) {
            if (title.Length == 0) {
                return "InteractiveWindowHost";
            }

            StringBuilder res = new StringBuilder();
            if (!Char.IsLetter(title[0])) {
                res.Append('_');
            }

            foreach (char c in title) {
                if (Char.IsLetter(c) || Char.IsDigit(c) || c == '_') {
                    res.Append(c);
                }
            }
            res.Append("Host");
            return res.ToString();
        }

        public void MoveCurrentFileToProject(string projectName) {
            ThreadPool.QueueUserWorkItem((x) => Dte.ExecuteCommand("file.ProjectPickerMoveInto"));
            IntPtr dialog = WaitForDialog();

            var chooseDialog = new ChooseLocationDialog(dialog);
            chooseDialog.FindProject(projectName);
            chooseDialog.ClickOK();

            WaitForDialogDismissed();
        }

        public DTE Dte {
            get {
                return _dte;
            }
        }
    }
}
