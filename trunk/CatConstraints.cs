/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class Constraints
    {
        List<List<CatKind>> mConstraintListList = new List<List<CatKind>>();
        Dictionary<string, List<CatKind>> mConstraints = new Dictionary<string, List<CatKind>>();
        TypeVarList mUnifiers = new TypeVarList();
        Dictionary<CatSelfType, CatFxnType> mSelfTypes = new Dictionary<CatSelfType, CatFxnType>();

        public void AddVectorConstraint(CatTypeVector v1, CatTypeVector v2)
        {
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

        public void AddFxnConstraint(CatFxnType f1, CatFxnType f2)
        {
            if (f1 == f2)
                return;

            if (f1 is CatSelfType)
            {
                CatFxnType g = mSelfTypes[f1 as CatSelfType];
                AddFxnConstraint(g, f2);
            }
            else if (f2 is CatSelfType)
            {
                CatFxnType g = mSelfTypes[f2 as CatSelfType];
                AddFxnConstraint(f1, g);
            }
            else 
            {
                AddVectorConstraint(f1.GetCons(), f2.GetCons());
                AddVectorConstraint(f1.GetProd(), f2.GetProd());
            }
        }

        /// <summary>
        /// Merges constraints associated with "s" into the destination list 
        /// </summary>
        private void MergeConstraintListsIfNeccessary(string s, List<CatKind> dest)
        {
            if (!mConstraints.ContainsKey(s))
                return;

            List<CatKind> src = mConstraints[s];

            // This is a possibility. We want to make sure it doesn't happen
            if (dest == src)
                return;

            // Remove the source constraint list before we continue
            mConstraintListList.Remove(src);

            // Update all hash lists to point to the new destination 
            List<string> keys = new List<string>(mConstraints.Keys);
            foreach (string key in keys)
            {
                if (mConstraints[key] == src)
                    mConstraints[key] = dest;
            }

            // One by one add the elements from the source list to the destination
            while (src.Count > 0) 
            {
                AddConstraintToList(dest, src[0]);
                src.RemoveAt(0);
            }                    
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
                for (int i=0; i < list.Count; ++i)
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

        public void AddConstraint(string s, CatKind k)
        {
            // Don't add  
            if (k.ToString().Equals(s))
                return;

            // Are we adding a self-type? 
            if (k is CatFxnType)
            {
                CatFxnType ft = k as CatFxnType;
                if (FxnContainsVar(ft, s, new Stack<CatKind>()))
                    k = new CatSelfType();
            }

            // Check for single unit vectors 
            if (k is CatTypeVector)
            {
                CatTypeVector vec = k as CatTypeVector;
                if (vec.GetKinds().Count == 1)
                {
                    // vectors with only one thing, are really that thing. 
                    AddConstraint(s, vec.GetKinds()[0]);
                    return;
                }
            }

            // If a constraint list doesn't exist then create one
            if (!mConstraints.ContainsKey(s))
            {
                List<CatKind> list = new List<CatKind>();
                mConstraints.Add(s, list);
                mConstraintListList.Add(list);
            }

            // Check if we are constraining a variable to a variable.
            // If so, we need to share a constraint list 
            if (k.IsKindVar())
            {
                if (mConstraints.ContainsKey(k.ToString()))
                {
                    List<CatKind> c1 = mConstraints[s];
                    List<CatKind> c2 = mConstraints[k.ToString()];

                    // It is important to remember that c1 can be the same as c2
                    if (c1 != c2)
                    {
                        // Copy the contents first 
                        foreach (CatKind tmp in c2.ToArray())
                            AddConstraintToList(c1, tmp);
                    }
                }
                else
                {
                    mConstraints.Add(k.ToString(), mConstraints[s]);
                }

                // Link the other variable to the merged constraint list
                mConstraints[k.ToString()] = mConstraints[s];
            }

            AddConstraintToList(mConstraints[s], k);
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
                // TODO: check that they are both the same kind.
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
                // TODO: check that they are both the same kind
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

        public override string ToString()
        {
            string ret = "";
            foreach (KeyValuePair<string, List<CatKind>> kvp in mConstraints)
            {
                ret += kvp.Key + " = ";
                foreach (CatKind k in kvp.Value)
                    ret += k.ToString() + " | ";
                ret += '\n';
            }
            return ret;
        }

        private bool VectorContainsVar(CatTypeVector vec, string s, Stack<CatKind> visited)
        {
            if (visited.Contains(vec))
            {
                // Cycle detected
                return false;
            }
            visited.Push(vec);

            foreach (CatKind child in vec.GetKinds())
            {
                if (child.ToString().Equals(s)) 
                    return true;
                if (child.IsKindVar() && mConstraints.ContainsKey(child.ToString()))
                {
                    List<CatKind> list = mConstraints[child.ToString()];
                    foreach (CatKind k in list)
                    {
                        if (k.IsKindVar())
                        {
                            if (k.ToString().Equals(s))
                                return true;
                        }
                        else if (k is CatTypeVector)
                        {
                            if (VectorContainsVar(k as CatTypeVector, s, visited))
                                return true;
                        }
                        else if (k is CatFxnType)
                        {
                            if (FxnContainsVar(k as CatFxnType, s, visited))
                                return true;
                        }
                    }
                }
            }

            // TODO: detect deeper cycles, perhaps if any of the children contains the var, then 
            // we throw an exception? Could be an algorithm with an infinite loop.

            return false;
        }

        private bool FxnContainsVar(CatFxnType ft, string s, Stack<CatKind> visited)
        {
            if (visited.Contains(ft))
            {
                // Cycle detected 
                return false;
            }
                
            visited.Push(ft);
            return VectorContainsVar(ft.GetCons(), s, visited) || VectorContainsVar(ft.GetProd(), s, visited);
        }

        private bool IsSelfType(string s)
        {
            if (!mConstraints.ContainsKey(s))
                return false;

            foreach (CatKind child in mConstraints[s])
            {
                if (child is CatFxnType)
                {
                    if (FxnContainsVar(child as CatFxnType, s, new Stack<CatKind>()))
                        return true;
                }
                /*
                 * HACK: I am not sure that this is actually helpful
                else if (child is CatTypeVector)
                {
                    if (VectorContainsVar(child as CatTypeVector, s))
                        throw new Exception("illegal circular type vector reference found: " + s);
                }
                 */
            }

            return false;
        }

        private void DetectSelfTypes()
        {
            foreach (KeyValuePair<string, List<CatKind>> kvp in mConstraints)
            {
                if (IsSelfType(kvp.Key))
                    kvp.Value.Add(new CatSelfType());
            }
        }

        private void CreateUnifiers()
        {
            // Resolve circular reference:
            // If there is a function-type, does it contain any type-variable that resolves to it? 
            // This means traversing the graph. 
            // If that is indeed possible, add "self" to the constraint list. 

            foreach (KeyValuePair<string, List<CatKind>> kvp in mConstraints)
            {                
                List<CatKind> list = kvp.Value;

                // Empty lists are not supposed to be possible.
                Trace.Assert(list.Count > 0);

                while (list.Count > 1)
                {
                    if (Config.gbVerboseInference)
                    {
                        MainClass.Write("Merging constraints: ");
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (i > 0) MainClass.Write(" = ");
                            MainClass.Write(list[i].ToString());
                        }
                        MainClass.WriteLine("");
                    }

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
                    MainClass.WriteLine("Unified constraint = " + k.ToString());

                mUnifiers[kvp.Key] = ResolveKind(k);

                Trace.Assert(mUnifiers[kvp.Key] != null);
            }            
        }

        private CatKind ResolveKind(CatKind k)
        {
            if (k.IsKindVar())
            {
                string s = k.ToString();
                if (mUnifiers.ContainsKey(s))
                {
                    if (IsSelfType(s))
                    {
                        CatSelfType tmp = new CatSelfType();
                        CatFxnType ft = mUnifiers[s] as CatFxnType;
                        Trace.Assert(ft != null);
                        AddSelfType(tmp, ft);
                        return tmp;
                    }

                    return mUnifiers[s];
                }
                else
                    return k;
            }
            else if (k is CatTypeVector)
            {
                CatTypeVector v = k as CatTypeVector;
                CatTypeVector ret = new CatTypeVector();
                foreach (CatKind tmp in v.GetKinds())
                    ret.Add(ResolveKind(tmp));
                return ret;
            }
            else if (k is CatSelfType)
            {
                return k;
            }
            else if (k is CatFxnType)
            {
                CatFxnType ft = k as CatFxnType;
                CatTypeVector cons = ResolveKind(ft.GetCons()) as CatTypeVector;
                CatTypeVector prod = ResolveKind(ft.GetProd()) as CatTypeVector;
                CatFxnType ret = new CatFxnType(cons, prod, ft.HasSideEffects());
                return ret;
            }
            else
            {
                return k;
            }
        }

        private void ResolveUnifiers()
        {
            string[] a = new string[mUnifiers.Count];
            mUnifiers.Keys.CopyTo(a, 0);
            foreach (string s in a)
            {
                mUnifiers[s] = ResolveKind(mUnifiers[s]);
            }
        }

        public TypeVarList GetResolvedUnifiers()
        {
            CreateUnifiers();
            ResolveUnifiers();
            return mUnifiers;
        }

        public void Clear()
        {
            mConstraintListList.Clear();
            mConstraints.Clear();
            mUnifiers.Clear();
            mSelfTypes.Clear();
        }

        private void AddSelfType(CatSelfType t, CatFxnType ft)
        {
            CatFxnType tmp = ft.RemoveImplicitRhoVariables();
            if (!mSelfTypes.ContainsKey(t))
                mSelfTypes.Add(t, tmp);
        }

        private void AddSelfTypes(CatFxnType ft, CatTypeVector vec)
        {
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatSelfType)
                {
                    AddSelfType(k as CatSelfType, ft);
                }
                else if (k is CatFxnType)
                {
                    AddSelfTypes(k as CatFxnType);
                }
                else if (k is CatTypeVector)
                {
                    AddSelfTypes(ft, k as CatTypeVector);
                }
            }
        }
        
        /// <summary>
        /// Associates self-types with their parents. 
        /// This assures that proper constraint relations can be made with self types.
        /// </summary>
        public void AddSelfTypes(CatFxnType ft)
        {
            if (ft is CatSelfType)
                throw new Exception("Unexpected self type");
            AddSelfTypes(ft, ft.GetCons());
            AddSelfTypes(ft, ft.GetProd());
        }

        public Dictionary<CatSelfType, CatFxnType> GetSelfTypes()
        {
            return mSelfTypes;
        }
    }
}