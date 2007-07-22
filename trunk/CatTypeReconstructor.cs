using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    // TODO: embed CatVarRenamer in this class
    // TODO: handle recursive types properly
    class CatTypeReconstructor
    {
        #region static fields
        static CatTypeReconstructor gpTypeReconstructor = new CatTypeReconstructor();
        #endregion

        #region member fields
        ConstraintList mConstraints = new ConstraintList();
        UnifierList mUnifiers = new UnifierList();
        CatTypeVarList mVars = new CatTypeVarList();
        CatVarScopes mScopes = new CatVarScopes();
        #endregion 

        #region constructor
        CatTypeReconstructor()
        {
            // do nothing
        }
        #endregion

        #region static functions
        public static void OutputInferredType(CatFxnType ft)
        {
            MainClass.WriteLine("After rewriting");
            //MainClass.WriteLine("ML style type: " + ft.ToPrettyString(true));
            //MainClass.WriteLine("Cat style:     " + ft.ToPrettyString(false));
            MainClass.WriteLine(ft.ToPrettyString(false));
            MainClass.WriteLine("");
        }

        public static CatFxnType Infer(List<Function> f)
        {
            if (!Config.gbTypeChecking)
                return null;

            if (f.Count == 0)
            {
                if (Config.gbVerboseInference)
                    WriteLine("type is ( -> )");
                return CatFxnType.Create("( -> )");
            }
            else if (f.Count == 1)
            {
                Function x = f[0];
                if (Config.gbVerboseInference)
                    OutputInferredType(x.GetFxnType());
                return x.GetFxnType();
            }
            else
            {
                Function x = f[0];
                CatFxnType ft = x.GetFxnType();
                if (Config.gbVerboseInference)
                    WriteLine("initial term = " + x.GetName() + " : " + x.GetFxnTypeString());

                for (int i = 1; i < f.Count; ++i)
                {
                    if (ft == null)
                        return ft;
                    Function y = f[i];
                    if (Config.gbVerboseInference)
                    {
                        WriteLine("Composing accumulated terms with next term");
                        Write("previous terms = { ");
                        for (int j = 0; j < i; ++j)
                            Write(f[j].GetName() + " ");
                        WriteLine("} : " + ft.ToString());
                        WriteLine("next term = " + y.GetName() + " : " + y.GetFxnTypeString());
                    }

                    // Object field functions (_def_, _get_, _set_) have to be 
                    // computed at the last minute, because their types are dependent on the previous types
                    if (y is ObjectFieldFxn)
                    {
                        (y as ObjectFieldFxn).ComputeType(ft);
                    }

                    ft = ComposeTypes(ft, y.GetFxnType());

                    if (ft == null)
                        return null;
                }
                return ft;
            }
        }

        public static CatFxnType ComposeTypes(CatFxnType left, CatFxnType right)
        {
            if (!Config.gbTypeChecking)
                return null;

            return gpTypeReconstructor.LocalComposeTypes(left, right);
        }

        public static void WriteLine(string s)
        {
            MainClass.WriteLine(s);
        }

        public static void Write(string s)
        {
            MainClass.Write(s);
        }
        #endregion 

        #region top-level functions
        CatFxnType LocalComposeTypes(CatFxnType left, CatFxnType right)
        {
            if (Config.gbVerboseInference)
            {
                WriteLine("");
                WriteLine("Composing Types");
                WriteLine("left  = " + left.ToString());
                WriteLine("right = " + right.ToString());
            }

            // Reset all of the lists
            mConstraints.Clear();
            mUnifiers.Clear();
            mVars.Clear();
            mScopes.Clear();

            // Make sure that the variables on the left function and the variables
            // on the right function are different
            CatVarRenamer renamer = new CatVarRenamer();
            left = renamer.Rename(left.AddImplicitRhoVariables());
            renamer.ResetNames();
            right = renamer.Rename(right.AddImplicitRhoVariables());

            CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());

            if (Config.gbVerboseInference)
            {
                WriteLine("after renaming");
                WriteLine("left  = " + left.ToString());
                WriteLine("right = " + right.ToString());
                WriteLine("---");
                WriteLine("unresolved inferred type");
                WriteLine(tmp.ToString());
                WriteLine("---");
            }

            // Create a list of all variables used including the left-most and right-most
            ComputeVars(left);
            ComputeVars(right);

            if (Config.gbVerboseInference)
            {
                WriteLine("all variables");
                WriteLine(mVars.ToString());

                WriteLine("equality");
                WriteLine(left.GetProd() + " = " + right.GetCons());
            }

            mConstraints.AddVectorConstraint(left.GetProd(), right.GetCons());

            if (Config.gbVerboseInference)
            {
                WriteLine("constraints");
                Write(mConstraints.ToString());
            }

            mConstraints.RemoveUnusedConstraints(tmp);

            /*
            if (Config.gbVerboseInference)
            {
                WriteLine("used constraints");
                Write(mConstraints.ToString());
            }
             */

            mConstraints.ComputeScopes(tmp, mScopes);

            if (Config.gbVerboseInference)
            {
                WriteLine("variable scopes");
                Write(mScopes.ToString());
            }

            ComputeUnifiers();

            if (Config.gbVerboseInference)
            {
                WriteLine("unresolved unifiers");
                Write(mUnifiers.ToString());
            }
            
            ResolveSelfTypes();

            if (Config.gbVerboseInference)
            {
                WriteLine("resolved self-types in unifiers");
                Write(mUnifiers.ToString());
            }

            ResolveUnifiers();

            if (Config.gbVerboseInference)
            {
                WriteLine("resolved unifiers");
                Write(mUnifiers.ToString());
            }

            mScopes.Clear();
            mConstraints.ComputeScopes(tmp, mScopes);

            if (Config.gbVerboseInference)
            {
                WriteLine("resolved variable scopes");
                Write(mScopes.ToString());
            }

            CatFxnType ret = CreateNewType(left, right);

            if (Config.gbVerboseInference)
            {
                WriteLine("reconstructed type");
                WriteLine(ret.ToIdString());
                WriteLine("ML style  : " + ret.ToPrettyString(true));
                WriteLine("Cat style : " + ret.ToPrettyString(false));
                WriteLine("");
            }

            return ret;
        }

        private void ComputeUnifiers()
        {
            foreach (KeyValuePair<string, List<CatKind>> kvp in mConstraints)
                mUnifiers.AddUnifier(kvp.Key, kvp.Value);
        }

        private void ResolveUnifiers()
        {
            Stack<CatKind> visited = new Stack<CatKind>();
            foreach (string s in new List<String>(mUnifiers.Keys))
            {
                Trace.Assert(visited.Count == 0);
                mUnifiers[s] = ResolveVars(mUnifiers[s], visited);
            }
        }

        private void ResolveSelfTypes()
        {
            foreach (string s in new List<string>(mUnifiers.Keys))
            {
                if (IsSelfType(s, mUnifiers[s]))
                {
                    mUnifiers[s] = new CatSelfType();
                }
            }
        }

        private CatFxnType CreateNewType(CatFxnType left, CatFxnType right)
        {
            CatFxnType ft = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());
            return ResolveVars(ft, new Stack<CatKind>());
        }

        private void ComputeVars(CatFxnType ft)
        {
            foreach (CatKind k in ft.GetDescendantKinds())
                if (k.IsKindVar())
                    mVars.Add(k);
        }

        #endregion

        #region variable resolution functions
        public CatTypeVector ResolveVars(CatTypeVector vec, Stack<CatKind> visited)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
                ret.Add(ResolveVars(k, visited));
            return ret;
        }

        public CatFxnType ResolveVars(CatFxnType ft, Stack<CatKind> visited)
        {
            if (ft is CatSelfType)
                return ft;
            CatFxnType ret = RenameFreeVars(ft);
            CatTypeVector cons = ResolveVars(ret.GetCons(), visited);
            CatTypeVector prod = ResolveVars(ret.GetProd(), visited);
            ret = new CatFxnType(cons, prod, ft.HasSideEffects());
            return ret;
        }

        public CatKind ResolveVars(CatKind k, Stack<CatKind> visited)
        {
            if (visited.Contains(k))
               return k;
            visited.Push(k);

            CatKind ret = k;
            if (k is CatFxnType)
                ret = ResolveVars(k as CatFxnType, visited);
            else if (k is CatTypeVector)
                ret = ResolveVars(k as CatTypeVector, visited);
            else if (k.IsKindVar())
            {
                CatKind u = ResolveVars(mUnifiers.GetUnifier(k), visited);
                if (u is CatFxnType)
                    ret = RenameFreeVars(u as CatFxnType);
                else
                    ret = u;
            }
            visited.Pop();
            return ret;
        }
        #endregion

        #region free variable renaming 
        CatKind GenerateVar(CatKind k, CatTypeVarList gen)
        {
            string s = k.ToString();
            if (gen.ContainsKey(s))
            {
                return gen[s];
            }
            else
            {
                CatKind ret;
                if (k is CatStackVar)
                    ret = CatStackVar.CreateUnique(); else
                    ret = CatTypeVar.CreateUnique();

                gen.Add(s, ret);
                return ret;
            }
        }

        bool IsFreeVar(CatFxnType ft, CatKind k)
        {
            if (!mScopes.IsFreeVar(ft, k))
                return false;
            string s = k.ToString();
            if (mUnifiers.ContainsKey(s))
                if (mUnifiers[s] is CatTypeVar)
                    return IsFreeVar(ft, mUnifiers[s]); else
                    return false;
            return true;
        }

        CatTypeVector RenameFreeVars(CatFxnType context, CatTypeVector vec, CatTypeVarList gen)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatSelfType)
                    ret.Add(k);
                else if (k is CatFxnType)
                    ret.Add(RenameFreeVars(context, k as CatFxnType, gen));
                else if (k is CatTypeVector)
                    ret.Add(RenameFreeVars(context, k as CatTypeVector, gen));
                else if (k.IsKindVar())
                {
                    if (IsFreeVar(context, k))
                        ret.Add(GenerateVar(k, gen));
                    else
                        ret.Add(k);
                }
                else
                {
                    ret.Add(k);
                }
            }
            return ret;
        }

        CatFxnType RenameFreeVars(CatFxnType context, CatFxnType ft, CatTypeVarList gen)
        {
            if (ft is CatSelfType)
                return ft;
            CatTypeVector cons = RenameFreeVars(context, ft.GetCons(), gen);
            CatTypeVector prod = RenameFreeVars(context, ft.GetProd(), gen);
            CatFxnType ret = new CatFxnType(cons, prod, ft.HasSideEffects());
            return ret;
        }

        CatFxnType RenameFreeVars(CatFxnType ft)
        {
            CatTypeVarList gen = new CatTypeVarList();
            CatFxnType ret = RenameFreeVars(ft, ft, gen);            
            return ret;
        }
        #endregion

        #region self function resolution
        /// <summary>
        /// Used for the identification of "self" types, so it does not 
        /// recurse into functions
        /// </summary>
        /// <returns></returns>
        bool DoesKindContain(CatKind k, string s, Stack<CatKind> visited)
        {
            if (visited.Contains(k))
                return false;

            visited.Push(k);
            if (k.IsKindVar())
            {
                if (k.ToString().Equals(s))
                {
                    visited.Pop();
                    return true;
                }
                else
                {
                    if (mConstraints.ContainsKey(k.ToString()))
                    {
                        foreach (CatKind j in mConstraints[k.ToString()])
                        {
                            if (DoesKindContain(j, s, visited))
                            {
                                visited.Pop();
                                return true;
                            }
                        }
                    }
                }
            }
            else if (k is CatTypeVector)
            {
                CatTypeVector vec = k as CatTypeVector;
                foreach (CatKind j in vec.GetKinds())
                {
                    if (DoesKindContain(j, s, visited))
                    {
                        visited.Pop();
                        return true;
                    }
                }
            }
            else if (k is CatFxnType)
            {
                // do nothing
                // This algorithm does not go into functions. It is only used to look one level deep 
                // in order to identify "self" types.
            }

            visited.Pop();
            return false;
        }

        /// <summary>
        /// Used to identify self types, which are defined as function types which refer to themselves
        /// at the top-level of the consumption, or production. In other words: 
        /// ( -> self) is a self type but, ( -> ( -> self)) is not a self type. The term "self" always 
        /// refers to the enclosing scope.
        /// </summary>
        private bool FxnContainsVar(CatFxnType ft, string s)
        {
            Stack<CatKind> visited = new Stack<CatKind>();
            foreach (CatKind tmp in ft.GetChildKinds())
            {
                if (tmp.IsKindVar())
                    if (DoesKindContain(tmp, s, visited))
                        return true;                    
            }
            return false;
        }

        private bool IsSelfType(string s, CatKind k)
        {
            if (k is CatFxnType)
                return FxnContainsVar(k as CatFxnType, s);
            return false;
        }

        #endregion

        #region member classes

        /// <summary>
        /// A constraint is a relationship between a variable (identified by name)
        /// and a particular kind.
        /// </summary>
        class ConstraintList : Dictionary<string, List<CatKind>>
        {
            List<CatKindPair> mProcessedList = new List<CatKindPair>();
            public static List<CatKind> gEmptyConstraintList = new List<CatKind>();

            struct CatKindPair
            {
                CatKind mFirst;
                CatKind mSecond;
                public CatKindPair(CatKind x, CatKind y)
                {
                    mFirst = x;
                    mSecond = y;
                }
                public bool Equals(CatKindPair that)
                {
                    return (this.mFirst == that.mFirst && this.mSecond == that.mSecond);
                }
            }

            public void AddConstraint(string s, CatKind k)
            {
                if (k.ToString().Equals(s)) return;
                if (ConstraintExists(s, k)) return;
                
                // Are we adding a self-type? 
                // TODO: check for self-types and handle it 

                // Check for single unit vectors 
                if (k is CatTypeVector)
                {
                    CatTypeVector vec = k as CatTypeVector;
                    if (vec.GetKinds().Count == 1)
                    {                        
                        // vectors with only one thing, are really that thing. 
                        k = vec.GetKinds()[0];
                    }
                }
               
                // add the constraint to this list 
                if (!(ContainsKey(s)))
                    Add(s, new List<CatKind>());

                AddConstraintToList(this[s], k);
            }

            private bool ConstraintExists(string s, CatKind k)
            {
                return (ContainsKey(s) && this[s].Contains(k));
            }

            private void AddConstraintToList(List<CatKind> list, CatKind k)
            {
                if (k is CatTypeVector)
                {
                    CatTypeVector v = (k as CatTypeVector);
                    Trace.Assert(v.GetKinds().Count > 1);
                }

                if (list.Contains(k))
                    return;

                if (k is CatFxnType)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i] is CatFxnType)
                        {
                            CatFxnType ft = list[i] as CatFxnType;
                            AddFxnConstraint(k as CatFxnType, list[i] as CatFxnType);
                        }
                    }
                }
                else if (k is CatTypeVector)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (list[i] is CatTypeVector)
                        {
                            AddVectorConstraint(k as CatTypeVector, list[i] as CatTypeVector);
                        }
                    }
                }

                list.Add(k);

                if (k is CatKind)
                {
                    // We may have to merge two constraint lists
                    MergeConstraintListsIfNeccessary(k.ToString(), list);
                }
            }

            /// <summary>
            /// Merges constraints associated with "s" into the destination list 
            /// </summary>
            private void MergeConstraintListsIfNeccessary(string s, List<CatKind> dest)
            {
                if (!ContainsKey(s))
                    return;

                List<CatKind> src = this[s];

                // This is a possibility. We want to make sure it doesn't happen
                if (dest == src)
                    return;

                // Update all hash lists to point to the new destination 
                List<string> keys = new List<string>(Keys);
                foreach (string key in keys)
                {
                    if (this[key] == src)
                        this[key] = dest;
                }

                // One by one add the elements from the source list to the destination
                while (src.Count > 0)
                {
                    AddConstraintToList(dest, src[0]);
                    src.RemoveAt(0);
                }
            }

            IEnumerable<CatKind> GetConstraintsFor(string sVar)
            {
                if (ContainsKey(sVar))
                    return this[sVar]; else
                    return gEmptyConstraintList;
            }

            bool HasConstraintBeenProcessed(CatKind x, CatKind y)
            {
                CatKindPair tmp = new CatKindPair(x, y);
                foreach (CatKindPair kp in mProcessedList)
                {
                    if (kp.Equals(tmp))
                        return true;
                }
                return false;
            }
            void AddConstraintToProcessedList(CatKind x, CatKind y)
            {
                CatKindPair tmp = new CatKindPair(x, y);
                mProcessedList.Add(tmp);
            }

            public void AddVectorConstraint(CatTypeVector v1, CatTypeVector v2)
            {
                if (HasConstraintBeenProcessed(v1, v2))
                    return;
                AddConstraintToProcessedList(v1, v2);

                while (!v1.IsEmpty() && !v2.IsEmpty())
                {
                    CatKind k1 = v1.GetTop();
                    CatKind k2 = v2.GetTop();

                    if (k1 is CatStackVar)
                    {
                        AddConstraint(k1.ToString(), v2);
                        if (k2 is CatStackVar)
                            AddConstraint(k2.ToString(), v1);
                        return;
                    }
                    else if (k2 is CatStackVar)
                    {
                        AddConstraint(k2.ToString(), v1);
                        return;
                    }

                    if (k1 is CatTypeVar)
                    {
                        AddConstraint(k1.ToString(), k2);
                    }
                    if (k2 is CatTypeVar)
                    {
                        AddConstraint(k2.ToString(), k1);
                    }
                    if ((k1 is CatFxnType) && (k2 is CatFxnType))
                    {
                        AddFxnConstraint(k1 as CatFxnType, k2 as CatFxnType);
                    }

                    v1 = v1.GetRest();
                    v2 = v2.GetRest();
                }
            }

            public void AddSelfTypeConstraint(CatSelfType st, CatFxnType ft)
            {
                if (ft is CatSelfType)
                {
                    ft = ft.GetParent();
                    if (ft == null)
                        throw new Exception("self-type missing parent");
                }

                CatFxnType parent = st.GetParent();
                if (parent == null)
                    throw new Exception("self-type missing parent");

                AddFxnConstraint(parent, ft);
            }

            public void AddFxnConstraint(CatFxnType f1, CatFxnType f2)
            {
                if (f1 == f2)
                    return;
                if (HasConstraintBeenProcessed(f1, f2))
                    return;
                AddConstraintToProcessedList(f1, f2);

                
                if (f1 is CatSelfType)
                {
                    AddSelfTypeConstraint(f1 as CatSelfType, f2);
                }
                else if (f2 is CatSelfType)
                {
                    AddSelfTypeConstraint(f2 as CatSelfType, f1);
                }
                else
                {
                    AddVectorConstraint(f1.GetCons(), f2.GetCons());
                    AddVectorConstraint(f1.GetProd(), f2.GetProd());
                }
            }

            public override string ToString()
            {
                string ret = "";
                foreach (KeyValuePair<string, List<CatKind>> kvp in this)
                {
                    ret += kvp.Key + " = ";
                    foreach (CatKind k in kvp.Value)
                        ret += k.ToIdString() + "; ";
                    ret += "\n";
                }
                return ret;
            }

            private void GetUsedConstraints(CatKind k, List<string> used, Stack<CatKind> visited)
            {
                if (visited.Contains(k))
                    return;
                visited.Push(k);                
                
                if (k.IsKindVar())
                {
                    string s = k.ToString();
                    if (ContainsKey(s))
                    {
                        if (!used.Contains(s))
                            used.Add(s);
                        foreach (CatKind j in this[s])
                            GetUsedConstraints(j, used, visited);
                    }
                }
                else if (k is CatFxnType)
                {
                    foreach (CatKind j in (k as CatFxnType).GetChildKinds())
                        GetUsedConstraints(j, used, visited);
                }
                else if (k is CatTypeVector)
                {
                    foreach (CatKind j in (k as CatTypeVector).GetKinds())
                        GetUsedConstraints(j, used, visited);
                }
                    
                visited.Pop();
            }

            public void RemoveUnusedConstraints(CatFxnType ft)
            {
                List<string> used = new List<string>();
                GetUsedConstraints(ft, used, new Stack<CatKind>());
                foreach (string s in new List<string>(Keys))
                    if (!used.Contains(s))
                        Remove(s);
            }

            public void AssignVarsToScope(CatFxnType context, CatVarScopes scopes, CatKind k, Stack<CatKind> visited)
            {
                if (visited.Contains(k))
                    return;
                visited.Push(k);

                if (k.IsKindVar())
                {
                    string s = k.ToString();
                    scopes.Add(s, context);
                    if (ContainsKey(s))
                    {
                        List<CatKind> tmp = this[s];
                        foreach (CatKind j in tmp)
                            AssignVarsToScope(context, scopes, j, visited);
                    }
                }
                else if (k is CatFxnType)
                {
                    CatFxnType ft = k as CatFxnType;
                    foreach (CatKind j in ft.GetChildKinds())
                        AssignVarsToScope(ft, scopes, j, visited);
                }
                else if (k is CatTypeVector)
                {
                    foreach (CatKind j in (k as CatTypeVector).GetKinds())
                        AssignVarsToScope(context, scopes, j, visited);
                }

                visited.Pop();
            }

            /// <summary>
            /// Provides a list of lists of kinds without repetition. 
            /// </summary>
            public List<List<CatKind>> GetListOfLists()
            {
                List<List<CatKind>> listOfLists = new List<List<CatKind>>();
                foreach (List<CatKind> list in Values)
                {
                    if (!listOfLists.Contains(list))
                        listOfLists.Add(list);
                }
                return listOfLists;
            }

            public IEnumerable<CatKind> GetKinds()
            {
                foreach (List<CatKind> list in GetListOfLists())
                    foreach (CatKind k in list)
                        yield return k;
            }

            public void ComputeScopes(CatFxnType ft, CatVarScopes scopes)
            {
                foreach (CatKind k in GetKinds())
                    AssignVarsToScope(ft, scopes, k, new Stack<CatKind>());
                
                // NOTE: this might not work, I might have to instead 
                // add everything that is in the function as a new "constraint".
                // even if unused? I am not sure. 
                // My big concern is that the sub-functions in "tmp" are not the 
                // same as in constraints.
                foreach (CatKind k in ft.GetDescendantKinds())
                    AssignVarsToScope(ft, scopes, k, new Stack<CatKind>());
            }
        }
        
        class UnifierList : Dictionary<string, CatKind>
        {
            public override string ToString()
            {
                string ret = "";
                foreach (KeyValuePair<string, CatKind> kvp in this)
                    ret += kvp.Key + " = " + kvp.Value.ToIdString() + "\n";
                return ret;
            }

            public CatKind GetUnifier(CatKind k)
            {
                Trace.Assert(k.IsKindVar());
                if (!ContainsKey(k.ToString())) 
                    return k;
                return this[k.ToString()];
            }

            private CatKind CreateFxnUnifier(CatFxnType ft, CatKind k)
            {
                if (!(k is CatFxnType))
                {
                    if (k.IsAny() || k.IsDynFxn())
                    {
                        if (!ft.IsRuntimePolymorphic())
                            throw new Exception("Function " + ft.ToPrettyString(false) + " is not runtime polymorphic");
                        return ft;
                    }
                    if (!k.IsKindVar())
                        throw new KindException(ft, k);

                    // Q: this is the opposite of what I expected. 
                    return ft;
                }
                else
                {
                    CatFxnType ft2 = k as CatFxnType;
                    if (ft.GetCons().GetKinds().Count >= ft2.GetCons().GetKinds().Count)
                        return ft;
                    else
                        return ft2;
                }
            }

            private CatKind CreateVectorUnifier(CatTypeVector vec, CatKind k)
            {
                if (!(k is CatTypeVector))
                {
                    if (k.IsAny() || k.IsKindVar())
                        return vec;
                    throw new KindException(vec, k);
                }
                else
                {
                    CatTypeVector vec2 = k as CatTypeVector;
                    if (vec.GetKinds().Count >= vec2.GetKinds().Count)
                        return vec;
                    else
                        return vec2;
                }
            }

            private CatKind CreateUnifier(CatKind k1, CatKind k2)
            {
                if (k1 == null)
                    return k2;
                if (k2 == null)
                    return k1;

                if (k1 is CatSelfType)
                {
                    if (!(k2 is CatFxnType) && !k2.IsKindVar())
                        throw new KindException(k1, k2);

                    return k1;
                }
                else if (k2 is CatSelfType)
                {
                    if (!(k1 is CatFxnType) && !k1.IsKindVar())
                        throw new KindException(k1, k2);

                    return k2;
                }
                else if (k1 is CatFxnType)
                {
                    return CreateFxnUnifier(k1 as CatFxnType, k2);
                }
                else if (k2 is CatFxnType)
                {
                    return CreateFxnUnifier(k2 as CatFxnType, k1);
                }
                else if (k1 is CatTypeVector)
                {
                    return CreateVectorUnifier(k1 as CatTypeVector, k2);
                }
                else if (k1.IsKindVar())
                {
                    if (k2.IsKindVar())
                    {
                        if (k1.ToString().CompareTo(k2.ToString()) <= 0)
                            return k1;
                        else
                            return k2;
                    }
                    else
                    {
                        return k2;
                    }
                }
                else if (k2.IsKindVar())
                {
                    return k1;
                }
                else if (k1.IsSubtypeOf(k2))
                {
                    return k1;
                }
                else if (k2.IsSubtypeOf(k1))
                {
                    return k2;
                }
                else
                {
                    throw new Exception("Unsupported kinds " + k1.ToString() + ":" + k1.GetType().ToString()
                        + " and " + k2.ToString() + ":" + k2.GetType().ToString());
                }
            }

            public void AddUnifier(string s, List<CatKind> list)
            {
                // Empty lists are not supposed to be possible.
                Trace.Assert(list.Count > 0);

                if (Config.gbVerboseInference)
                {
                    string tmp = "";
                    for (int i = 0; i < list.Count; ++i)
                        tmp += list[i].ToString();
                    MainClass.WriteLine("Merging constraints for " + s + " : " + tmp);
                }

                Add(s, null);
                while (list.Count > 1)
                {

                    CatKind k1 = list[0];
                    CatKind k2 = list[1];

                    // Unify both types.
                    CatKind u = CreateUnifier(k1, k2);

                    list.RemoveAt(0);
                    list[0] = u;
                }

                CatKind k = list[0];
                Trace.Assert(k != null);

                if (Config.gbVerboseInference)
                    WriteLine("Unifier = " + k.ToString());

                this[s] = k;
            }
        }
        #endregion
    }
}
