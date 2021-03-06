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
using System.Windows.Forms;
using Microsoft.PythonTools.Repl;

namespace Microsoft.PythonTools.Options {
    public partial class PythonAdvancedEditorOptionsControl : UserControl {        
        public PythonAdvancedEditorOptionsControl() {
            InitializeComponent();
            _enterCommits.Checked = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.EnterCommitsIntellisense;
            _intersectMembers.Checked = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.IntersectMembers;
            _completionCommitedBy.Text = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.CompletionCommittedBy;
            _newLineAfterCompleteCompletion.Checked = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.AddNewLineAtEndOfFullyTypedWord;
            _outliningOnOpen.Checked = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.EnterOutliningModeOnOpen;
            _fillParagraphText.Text = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.FillParagraphColumns.ToString();
            _pasteRemovesReplPrompts.Checked = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.PasteRemovesReplPrompts;
        }

        private void _enterCommits_CheckedChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.EnterCommitsIntellisense = _enterCommits.Checked;
        }

        private void _intersectMembers_CheckedChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.IntersectMembers = _intersectMembers.Checked;
        }

        private void _completionCommitedBy_TextChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.CompletionCommittedBy = _completionCommitedBy.Text;
        }

        private void _newLineAfterCompleteCompletion_CheckedChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.AddNewLineAtEndOfFullyTypedWord = _newLineAfterCompleteCompletion.Checked;
        }

        private void _outliningOnOpen_CheckedChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.EnterOutliningModeOnOpen = _outliningOnOpen.Checked;
        }

        private void _fillParagraphText_TextChanged(object sender, EventArgs e) {
            for (int i = 0; i < _fillParagraphText.Text.Length; i++) {
                if (!Char.IsDigit(_fillParagraphText.Text[i])) {
                    _fillParagraphText.Text = PythonToolsPackage.Instance.AdvancedEditorOptionsPage.FillParagraphColumns.ToString();
                    return;
                }
            }
            if (_fillParagraphText.Text.Length != 0) {
                PythonToolsPackage.Instance.AdvancedEditorOptionsPage.FillParagraphColumns = Convert.ToInt32(_fillParagraphText.Text);
            }
        }

        private void _pasteRemovesReplPrompts_CheckedChanged(object sender, EventArgs e) {
            PythonToolsPackage.Instance.AdvancedEditorOptionsPage.PasteRemovesReplPrompts = _pasteRemovesReplPrompts.Checked;
        }
    }
}
