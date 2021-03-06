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
using System.Text;
using Microsoft.PythonTools.Interpreter;
using Microsoft.PythonTools.Parsing.Ast;

namespace Microsoft.PythonTools.Analysis.Values {
    internal class BuiltinMethodInfo : BuiltinNamespace<IPythonType> {
        private readonly IPythonFunction _function;
        private readonly PythonMemberType _memberType;
        private string _doc;
        private readonly ISet<Namespace> _returnTypes;
        private BoundBuiltinMethodInfo _boundMethod;
        private OverloadResult[] _overloads;

        public BuiltinMethodInfo(IPythonMethodDescriptor method, PythonAnalyzer projectState)
            : base(projectState.Types.BuiltinMethodDescriptor, projectState) {
            var function = method.Function;
            _memberType = method.MemberType;
            _function = function;
            _returnTypes = Utils.GetReturnTypes(function, projectState);
        }

        public BuiltinMethodInfo(IPythonFunction function, PythonMemberType memType, PythonAnalyzer projectState)
            : base(projectState.Types.BuiltinMethodDescriptor, projectState) {
            _memberType = memType;
            _function = function;
            _returnTypes = Utils.GetReturnTypes(function, projectState);            
        }

        public override IPythonType PythonType {
            get { return _type; }
        }

        public override ISet<Namespace> Call(Node node, Interpreter.AnalysisUnit unit, ISet<Namespace>[] args, string[] keywordArgNames) {
            return _returnTypes;
        }

        public override ISet<Namespace> GetDescriptor(Namespace instance, Interpreter.AnalysisUnit unit) {
            if (_boundMethod == null) {
                _boundMethod = new BoundBuiltinMethodInfo(this);
            }

            return _boundMethod.SelfSet;
        }

        public override string Description {
            get {
                if (_function.IsBuiltin) {
                    return "built-in method " + _function.Name;
                }
                return "method " + _function.Name;
            }
        }

        public ISet<Namespace> ReturnTypes {
            get {
                return _returnTypes;
            }
        }

        public IPythonFunction Function {
            get {
                return _function;
            }
        }

        public override ICollection<OverloadResult> Overloads {
            get {
                if (_overloads == null) {
                    var overloads = Function.Overloads;
                    var result = new OverloadResult[overloads.Count];
                    for (int i = 0; i < result.Length; i++) {
                        result[i] = new BuiltinFunctionOverloadResult(ProjectState, _function.Name, overloads[i], 0, new ParameterResult("self"));
                    }
                    _overloads = result;
                }
                return _overloads;
            }
        }

        public override string Documentation {
            get {
                if (_doc == null) {
                    var doc = new StringBuilder();
                    foreach (var overload in Function.Overloads) {
                        doc.Append(Utils.StripDocumentation(overload.Documentation));
                    }
                    _doc = doc.ToString();
                }
                return _doc;
            }
        }

        public override PythonMemberType ResultType {
            get {
                return _memberType;
            }
        }

        public string Name { get { return _function.Name; } }
    }
}
