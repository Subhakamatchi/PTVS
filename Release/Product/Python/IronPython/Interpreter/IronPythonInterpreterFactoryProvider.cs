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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.PythonTools.Interpreter;

namespace Microsoft.IronPythonTools.Interpreter {
    [Export(typeof(IPythonInterpreterFactoryProvider))]
    class IronPythonInterpreterFactoryProvider : IPythonInterpreterFactoryProvider {
        private readonly IPythonInterpreterFactory _interpreter = new IronPythonInterpreterFactory();

        #region IPythonInterpreterProvider Members

        public IEnumerable<IPythonInterpreterFactory> GetInterpreterFactories() {
            if (IronPythonInterpreter.GetPythonInstallDir() != null) {
                yield return _interpreter;
            }
        }

        #endregion
    }
}
