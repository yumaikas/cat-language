/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    /// <summary>
    /// Unifiers are types associated with variables. These types may be other variables. So 
    /// it can be considered a directed graph. It is important to assure that no cycles ever exist.
    /// </summary>
    public class Unifiers
    {
        int mnId = 0;
        Dictionary<string, CatKind> mUnifiers = new Dictionary<string, CatKind>();

        public override string ToString()
        {
            string ret = "";
            foreach (KeyValuePair<string, CatKind> kvp in mUnifiers)
                ret += kvp.Key + " = " + kvp.Value.ToString() + "\n";
            return ret;
        }

        public void AddVectorConstraint(CatTypeVector v1, CatTypeVector v2)
        {
            while (!v1.IsEmpty() && !v2.IsEmpty())
            {
                CatKind k1 = v1.GetTop();
                CatKind k2 = v2.GetTop();

                // TODO: this might not cover all typing possibilities.
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

                // The problem here is that I need to replace a CatSimpleTypeKind 
                // with a variable, and unify it. Complicated isn't it! 

                if (k1 is CatSimpleTypeKind)
                {
                    if (!k2.IsSubtypeOf(k1) || !k1.IsSubtypeOf(k2))
                        throw new KindException(k1, k2);     
                   
                    // TODO: create unifiers for simple types 
                }
                if (k2 is CatSimpleTypeKind)
                {
                    if (!k2.IsSubtypeOf(k1) || !k1.IsSubtypeOf(k2))
                        throw new KindException(k1, k2);

                    // TODO: create unifiers for simple types 
                }

                v1 = v1.GetRest();
                v2 = v2.GetRest();
            }
        }

        public void AddFxnConstraint(CatFxnType f1, CatFxnType f2)
        {
            AddVectorConstraint(f1.GetCons(), f2.GetCons());
            AddVectorConstraint(f1.GetProd(), f2.GetProd());
        }

        private void ReplaceUnifiers(CatKind kOld, CatKind kNew)
        {
            // HACK: this could be improved 
            Dictionary<string, CatKind> dict = new Dictionary<string, CatKind>();
            foreach (KeyValuePair<string, CatKind> kvp in mUnifiers)
            {
                if (kvp.Value == kOld)
                {
                    dict.Add(kvp.Key, kNew);
                }
                else
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }
            mUnifiers = dict;
        }

        public void AddConstraint(string s, CatKind k)
        {
            // Don't add self-referential variables 
            if (k.ToString().CompareTo(s) == 0)
                return;

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

            if (mUnifiers.ContainsKey(s))
            {
                CatKind u = CreateUnifier(k, mUnifiers[s]);
                ReplaceUnifiers(mUnifiers[s], u);
            }
            else
            {
                if (k.IsKindVar() && mUnifiers.ContainsKey(k.ToString()))
                {
                    CatKind u = CreateUnifier(k, mUnifiers[k.ToString()]);
                    ReplaceUnifiers(mUnifiers[k.ToString()], u);
                    mUnifiers.Add(s, u);
                }
                else
                {
                    mUnifiers.Add(s, CreateUnifier(k, k));
                }
            }
        }

        private CatKind CreateUnifier(CatKind k1, CatKind k2)
        {
            if ((k1 is CatFxnType) || (k2 is CatFxnType))
            {
                if (!(k1 is CatFxnType)) return k2;
                if (!(k2 is CatFxnType)) return k1;
                CatFxnType ft1 = k1 as CatFxnType;
                CatFxnType ft2 = k2 as CatFxnType;
                if (ft1.GetCons().GetKinds().Count >= ft2.GetCons().GetKinds().Count) return ft1;
                return ft2;
            }
            else if ((k1 is CatTypeVector) || (k2 is CatTypeVector))
            {
                if (!(k1 is CatTypeVector)) return k2;
                if (!(k2 is CatTypeVector)) return k1;
                CatTypeVector vec1 = k1 as CatTypeVector;
                CatTypeVector vec2 = k2 as CatTypeVector;
                // TODO: check if they are all types in each vector. If so and 
                // the number of types differs we have a type error
                if (vec1.GetKinds().Count >= vec2.GetKinds().Count) return vec1;
                return vec2;
            }
            else if (k1 is CatTypeVar)
            {
                if (k2 is CatStackKind)
                    throw new Exception(k1.ToString() + " is a type and is not compatible with the type vector " + k2.ToString());
                return new CatTypeVar("u" + mnId++.ToString());
            }
            else if (k1 is CatStackVar)
            {
                if (!(k2 is CatStackVar))
                    throw new Exception("Stack variable " + k1.ToString() + " is not compatible with " + k2.ToString());
                return new CatStackVar("U" + mnId++.ToString());
            }
            else if (k1 is CatSimpleTypeKind)
            {
                string s1 = k1.ToString();
                string s2 = k2.ToString();
                if (!(k2 is CatSimpleTypeKind))
                    throw new Exception(s1 + " is not compatible with " + s2);

                if (k1.IsSubtypeOf(k2))
                {
                    return k1;
                }
                else if (k2.IsSubtypeOf(k1))
                {
                    return k2;
                }
                else
                {
                    throw new Exception(s1 + " is not compatible with " + s2);
                }
            }
            else
            {
                throw new Exception("Unsupported kinds " + k1.ToString() + ":" + k1.GetType().ToString()
                    + " and " + k2.ToString() + ":" + k2.GetType().ToString());
            }
        }

        public Dictionary<string, CatKind> GetResolvedUnifiers()
        {
            Dictionary<string, CatKind> ret = new Dictionary<string, CatKind>();

            Stack<CatKind> stk = new Stack<CatKind>();
            foreach (string s in mUnifiers.Keys)
            {
                ret.Add(s, ResolveKind(s, mUnifiers[s], stk));
                Trace.Assert(stk.Count == 0);
            }

            return ret;
        }

        private CatKind ResolveKind(string s, CatKind k, Stack<CatKind> visited)
        {
            Trace.Assert(k != null);
            Trace.Assert(visited != null);

            if (visited.Contains(k))
            {
                // TODO: if it is simply self-referential use "self"
                throw new Exception("Circular type reference");
            }

            if (k.IsKindVar())
            {
                if (mUnifiers.ContainsKey(k.ToString()))
                {
                    visited.Push(k);
                    CatKind ret = ResolveKind(s, mUnifiers[k.ToString()], visited);
                    visited.Pop();
                    return ret;
                }
                return k;
            }

            if (k is CatFxnType)
            {
                visited.Push(k);
                CatTypeVector cons = new CatTypeVector();
                CatTypeVector prod = new CatTypeVector();

                CatFxnType ft = k as CatFxnType;
                foreach (CatKind tmp in ft.GetCons().GetKinds())
                    cons.PushKind(ResolveKind(s, tmp, visited));
                foreach (CatKind tmp in ft.GetProd().GetKinds())
                    prod.PushKind(ResolveKind(s, tmp, visited));

                CatFxnType ret = new CatFxnType(cons, prod, ft.HasSideEffects());
                visited.Pop();
                return ret;
            }
            else if (k is CatTypeVector)
            {
                visited.Push(k);
                CatTypeVector vec = k as CatTypeVector;
                CatTypeVector ret = new CatTypeVector();
                foreach (CatKind tmp in vec.GetKinds())
                    ret.PushKind(ResolveKind(s, tmp, visited));
                visited.Pop();
                return ret;
            }
            else
            {
                return k;
            }
        }

        public void Clear()
        {
            mnId = 0;
            mUnifiers.Clear();
        }
    }

}