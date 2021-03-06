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
using Microsoft.PythonTools.Analysis.Interpreter;
using Microsoft.PythonTools.Parsing.Ast;

namespace Microsoft.PythonTools.Analysis.Values {
    /// <summary>
    /// Specialized built-in instance for sequences (lists, tuples)
    /// </summary>
    internal class SequenceInfo : BuiltinInstanceInfo {
        private ISet<Namespace> _unionType;        // all types that have been seen
        private ISet<Namespace>[] _indexTypes;     // types for known indices
        private DependentData _returnValue;

        public SequenceInfo(ISet<Namespace>[] indexTypes, BuiltinClassInfo seqType)
            : base(seqType) {
            _indexTypes = indexTypes;
        }

        public ISet<Namespace>[] IndexTypes {
            get { return _indexTypes; }
            protected set { _indexTypes = value; }
        }

        public ISet<Namespace> UnionType {
            get {
                EnsureUnionType();
                return _unionType; 
            }
            protected set { _unionType = value; }
        }

        public override int? GetLength() {
            return _indexTypes.Length;
        }

        public override ISet<Namespace> GetEnumeratorTypes(Node node, AnalysisUnit unit) {
            ReturnValue.AddDependency(unit);

            // TODO: This should be a union of the index types
            if (_indexTypes.Length == 0) {
                return EmptySet<Namespace>.Instance;
            }

            return _indexTypes[0];
        }

        public DependentData ReturnValue {
            get {
                if (_returnValue == null) {
                    _returnValue = new DependentData();
                }
                return _returnValue;
            }
        }

        public override ISet<Namespace> GetIndex(Node node, AnalysisUnit unit, ISet<Namespace> index) {
            ReturnValue.AddDependency(unit);
            int? constIndex = GetConstantIndex(index);

            if (constIndex != null && constIndex.Value < _indexTypes.Length) {
                // TODO: Warn if outside known index and no appends?
                return _indexTypes[constIndex.Value];
            }

            SliceInfo sliceInfo = GetSliceIndex(index);
            if (sliceInfo != null) {
                return this.SelfSet;
            }

            EnsureUnionType();
            return _unionType;
        }

        internal void AddTypes(ISet<Namespace>[] types) {
            if (_indexTypes.Length < types.Length) {
                ISet<Namespace>[] newTypes = new ISet<Namespace>[types.Length];
                for (int i = 0; i < _indexTypes.Length; i++) {
                    newTypes[i] = _indexTypes[i];
                }
                for (int i = _indexTypes.Length; i < types.Length; i++) {
                    newTypes[i] = EmptySet<Namespace>.Instance;
                }
                _indexTypes = newTypes;
            }

            bool added = false;
            for (int i = 0; i < types.Length; i++) {
                bool madeSet = false;
                int oldCount = _indexTypes[i].Count;
                _indexTypes[i] = _indexTypes[i].Union(types[i], ref madeSet);

                if (_indexTypes[i].Count != oldCount) {
                    added = true;
                }
            }

            if (added) {
                ReturnValue.EnqueueDependents();
                _unionType = null;
            }
        }


        private SliceInfo GetSliceIndex(ISet<Namespace> index) {
            foreach (var type in index) {
                if (type is SliceInfo) {
                    return type as SliceInfo;
                }
            }
            return null;
        }

        private void EnsureUnionType() {
            if (_unionType == null) {
                ISet<Namespace> unionType = EmptySet<Namespace>.Instance;
                bool setMade = false;
                foreach (var set in _indexTypes) {
                    unionType = unionType.Union(set, ref setMade);
                }
                _unionType = unionType;
            }
        }

        internal static int? GetConstantIndex(ISet<Namespace> index) {
            int? constIndex = null;
            int typeCount = 0;
            foreach (var type in index) {
                object constValue = type.GetConstantValue();
                if (constValue != null && constValue is int) {
                    constIndex = (int)constValue;
                }

                typeCount++;
            }
            if (typeCount != 1) {
                constIndex = null;
            }
            return constIndex;
        }

        public override string ShortDescription {
            get {
                return _type.Name;
            }
        }

        public override string Description {
            get {
                EnsureUnionType();
                StringBuilder result = new StringBuilder(_type.Name);
                var unionType = _unionType.GetUnionType();
                if (unionType != null) {
                    result.Append(" of " + unionType.ShortDescription);
                } else {
                    result.Append("()");
                }

                return result.ToString();
            }
        }

        public override bool UnionEquals(Namespace ns) {
            SequenceInfo si = ns as SequenceInfo;
            if (si == null) {
                return false;
            }

            return si._indexTypes.Length == _indexTypes.Length;
        }

        public override int UnionHashCode() {
            return _indexTypes.Length;
        }
    }
}
