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
using Microsoft.PythonTools.Analysis.Interpreter;
using Microsoft.PythonTools.Parsing;
using Microsoft.PythonTools.Parsing.Ast;

namespace Microsoft.PythonTools.Analysis.Values {
    sealed class MultipleMemberInfo : Namespace {
        private readonly Namespace[] _members;

        public MultipleMemberInfo(Namespace[] members) {
            _members = members;
        }

        public Namespace[] Members {
            get {
                return _members;
            }
        }

        public override ISet<Namespace> GetMember(Node node, AnalysisUnit unit, string name) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.GetMember(node, unit, name), ref madeSet);
            }
            return res;
        }

        public override void AugmentAssign(AugmentedAssignStatement node, AnalysisUnit unit, ISet<Namespace> value) {
            foreach (var member in _members) {
                member.AugmentAssign(node, unit, value);
            } 
        }

        public override ISet<Namespace> BinaryOperation(Node node, AnalysisUnit unit, PythonOperator operation, ISet<Namespace> rhs) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.BinaryOperation(node, unit, operation, rhs), ref madeSet);
            }
            return res;
        }

        public override ISet<Namespace> Call(Node node, AnalysisUnit unit, ISet<Namespace>[] args, string[] keywordArgNames) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.Call(node, unit, args, keywordArgNames), ref madeSet);
            }
            return res;
        }

        public override void DeleteMember(Node node, AnalysisUnit unit, string name) {
            foreach (var member in _members) {
                member.DeleteMember(node, unit, name);
            } 
        }

        public override IDictionary<string, ISet<Namespace>> GetAllMembers(PythonTools.Interpreter.IModuleContext moduleContext) {
            return base.GetAllMembers(moduleContext);
        }

        public override ISet<Namespace> GetDescriptor(Namespace instance, AnalysisUnit unit) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.GetDescriptor(instance, unit), ref madeSet);
            }
            return res;
        }

        public override ISet<Namespace> GetIndex(Node node, AnalysisUnit unit, ISet<Namespace> index) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.GetIndex(node, unit, index), ref madeSet);
            }
            return res;
        }

        public override void SetIndex(Node node, AnalysisUnit unit, ISet<Namespace> index, ISet<Namespace> value) {
            foreach (var member in _members) {
                member.SetIndex(node, unit, index, value);
            }            
        }

        public override ISet<Namespace> GetEnumeratorTypes(Node node, AnalysisUnit unit) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.GetEnumeratorTypes(node, unit), ref madeSet);
            }
            return res;
        }

        public override object GetConstantValue() {
            foreach (var member in _members) {
                object res = member.GetConstantValue();
                if (res != Type.Missing) {
                    return res;
                }
            }
            return base.GetConstantValue();
        }

        public override ISet<Namespace> GetStaticDescriptor(AnalysisUnit unit) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.GetStaticDescriptor(unit), ref madeSet);
            }
            return res;
        }

        public override int? GetLength() {
            foreach (var member in _members) {
                var res = member.GetLength();
                if (res != null) {
                    return res;
                }
            }
            return null;
        }

        public override void SetMember(Node node, AnalysisUnit unit, string name, ISet<Namespace> value) {
            foreach (var member in _members) {
                member.SetMember(node, unit, name, value);
            } 
        }

        public override ISet<Namespace> UnaryOperation(Node node, AnalysisUnit unit, PythonOperator operation) {
            ISet<Namespace> res = EmptySet<Namespace>.Instance;
            bool madeSet = false;
            foreach (var member in _members) {
                res.Union(member.UnaryOperation(node, unit, operation), ref madeSet);
            }
            return res;
        }
    }
}
