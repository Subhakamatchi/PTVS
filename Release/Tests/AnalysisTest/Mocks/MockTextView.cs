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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace AnalysisTest.Mocks {
    class MockTextView : ITextView {
        private readonly ITextBuffer _buffer;

        public MockTextView(ITextBuffer buffer) {
            _buffer = buffer;
        }

        public Microsoft.VisualStudio.Text.Projection.IBufferGraph BufferGraph {
            get { throw new NotImplementedException(); }
        }

        public ITextCaret Caret {
            get { throw new NotImplementedException(); }
        }

        public void Close() {
            throw new NotImplementedException();
        }

        public event EventHandler Closed {
            add {  }
            remove {  }
        }

        public void DisplayTextLineContainingBufferPosition(Microsoft.VisualStudio.Text.SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride) {
            throw new NotImplementedException();
        }

        public void DisplayTextLineContainingBufferPosition(Microsoft.VisualStudio.Text.SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo) {
            throw new NotImplementedException();
        }

        public Microsoft.VisualStudio.Text.SnapshotSpan GetTextElementSpan(Microsoft.VisualStudio.Text.SnapshotPoint point) {
            throw new NotImplementedException();
        }

        public Microsoft.VisualStudio.Text.Formatting.ITextViewLine GetTextViewLineContainingBufferPosition(Microsoft.VisualStudio.Text.SnapshotPoint bufferPosition) {
            throw new NotImplementedException();
        }

        public event EventHandler GotAggregateFocus {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public bool HasAggregateFocus {
            get { throw new NotImplementedException(); }
        }

        public bool InLayout {
            get { throw new NotImplementedException(); }
        }

        public bool IsClosed {
            get { throw new NotImplementedException(); }
        }

        public bool IsMouseOverViewOrAdornments {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public double LineHeight {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler LostAggregateFocus {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public double MaxTextRightCoordinate {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<MouseHoverEventArgs> MouseHover {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public IEditorOptions Options {
            get { throw new NotImplementedException(); }
        }

        public Microsoft.VisualStudio.Text.ITrackingSpan ProvisionalTextHighlight {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public void QueueSpaceReservationStackRefresh() {
            throw new NotImplementedException();
        }

        public ITextViewRoleSet Roles {
            get { throw new NotImplementedException(); }
        }

        public ITextSelection Selection {
            get { throw new NotImplementedException(); }
        }

        public ITextBuffer TextBuffer {
            get { return _buffer; }
        }

        public Microsoft.VisualStudio.Text.ITextDataModel TextDataModel {
            get { throw new NotImplementedException(); }
        }

        public Microsoft.VisualStudio.Text.ITextSnapshot TextSnapshot {
            get { throw new NotImplementedException(); }
        }

        public ITextViewLineCollection TextViewLines {
            get { throw new NotImplementedException(); }
        }

        public ITextViewModel TextViewModel {
            get { throw new NotImplementedException(); }
        }

        public IViewScroller ViewScroller {
            get { throw new NotImplementedException(); }
        }

        public double ViewportBottom {
            get { throw new NotImplementedException(); }
        }

        public double ViewportHeight {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler ViewportHeightChanged {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public double ViewportLeft {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public event EventHandler ViewportLeftChanged {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public double ViewportRight {
            get { throw new NotImplementedException(); }
        }

        public double ViewportTop {
            get { throw new NotImplementedException(); }
        }

        public double ViewportWidth {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler ViewportWidthChanged {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public Microsoft.VisualStudio.Text.ITextSnapshot VisualSnapshot {
            get { throw new NotImplementedException(); }
        }

        public Microsoft.VisualStudio.Utilities.PropertyCollection Properties {
            get { throw new NotImplementedException(); }
        }
    }
}
