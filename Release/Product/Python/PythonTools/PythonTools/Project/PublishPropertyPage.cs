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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.PythonTools.Project {
    [Guid(PythonConstants.PublishPropertyPageGuid)]
    public sealed class PublishPropertyPage : CommonPropertyPage {
        private readonly PublishPropertyControl _control;

        public PublishPropertyPage() {            
            _control = new PublishPropertyControl(this);
        }

        public override Control Control {
            get { return _control; }
        }

        public override void Apply() {
            Project.SetProjectProperty(CommonConstants.PublishUrl, _control.PublishUrl);
            IsDirty = false;
        }

        public override void LoadSettings() {
            _control.PublishUrl = Project.GetProjectProperty(CommonConstants.PublishUrl);
        }

        public override string Name {
            get { return "Publish"; }
        }
    }
}
