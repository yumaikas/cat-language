using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class UnificationApplier
    {        
        public UnificationApplier(CatFxnType context, CatTypeVarList u)
        {
            mUnifiers = u;
            mContextVars = context.GetAllVars();
            foreach (string s in u.Keys)
                AddBoundVars(s, u[s]);
        }

        CatTypeVarList mUnifiers;
        CatTypeVarList mContextVars;
        CatTypeVarList mGeneratedVars = new CatTypeVarList();
        Dictionary<string, string> mAllVars = new Dictionary<string, string>();
        List<string> mBoundVars = new List<string>();

        private void AddBoundVars(string s, CatKind k)
        {
            if (k is CatSelfType)
                return;
            else if (k is CatFxnType)
            {
                AddBoundVars(s, (k as CatFxnType).GetCons());
                AddBoundVars(s, (k as CatFxnType).GetProd());
            }
            else if (k is CatTypeVector)
            {
                foreach (CatKind tmp in (k as CatTypeVector).GetKinds())
                    AddBoundVars(s, tmp);
            }
            else if (k.IsKindVar())
            {
                if (!mAllVars.ContainsKey(k.ToString()))
                {
                    mAllVars.Add(k.ToString(), s);
                }
                else
                {
                    // If the variable occurs in another context, then it is bound
                    if (!mAllVars[k.ToString()].Equals(s))
                        mBoundVars.Add(k.ToString());
                }
            }
        }

        bool IsFreeVar(CatKind var)
        {
            if (!var.IsKindVar())
                return false;
            if (mUnifiers.ContainsKey(var.ToString()))
                return false;
            if (mContextVars.ContainsKey(var.ToString()))
                return false;
            return !mBoundVars.Contains(var.ToString());
        }

        CatKind GenerateVar(CatKind k)
        {
            string s = k.ToString();
            if (mGeneratedVars.ContainsKey(s))
            {
                return mGeneratedVars[s];
            }
            else
            {
                CatKind ret;
                if (k is CatStackVar)
                    ret = CatStackVar.CreateUnique(); else
                    ret = CatTypeVar.CreateUnique();

                mGeneratedVars.Add(s, ret);
                return ret;
            }
        }

        CatTypeVector RenameFreeVars(CatFxnType context, CatTypeVector vec)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatSelfType)
                    ret.Add(k);
                else if (k is CatFxnType)
                    ret.Add(RenameFreeVars(context, k as CatFxnType));
                else if (k is CatTypeVector)
                    ret.Add(RenameFreeVars(context, k as CatTypeVector));
                else if (k.IsKindVar())
                {
                    if (IsFreeVar(k))
                        ret.Add(GenerateVar(k)); else
                        ret.Add(k);
                }
                else
                {
                    ret.Add(k);
                }
            }
            return ret;
        }

        CatFxnType RenameFreeVars(CatFxnType context, CatFxnType ft)
        {
            CatTypeVector cons = RenameFreeVars(context, ft.GetCons());
            CatTypeVector prod = RenameFreeVars(context, ft.GetProd());
            return new CatFxnType(cons, prod, ft.HasSideEffects());
        }

        CatFxnType RenameFreeVars(CatFxnType ft)
        {
            CatTypeVarList old = mGeneratedVars;
            mGeneratedVars = new CatTypeVarList();
            mGeneratedVars.Clear();
            CatFxnType ret = RenameFreeVars(ft, ft);
            mGeneratedVars = old;
            return ret;
        }

        CatFxnType ApplyToFxnType(CatFxnType ft, Stack<CatKind> visited)
        {
            CatTypeVector cons = ApplyToTypeVector(ft.GetCons(), visited);
            CatTypeVector prod = ApplyToTypeVector(ft.GetProd(), visited);
            CatFxnType ret = new CatFxnType(cons, prod, ft.HasSideEffects());
            return ret;
        }

        /// <summary>
        /// Applies unifiers to construct a new CatKind
        /// </summary>
        public CatKind ApplyUnifiers(CatKind k, Stack<CatKind> visited)
        {
            if (visited.Contains(k))
                return k;

            visited.Push(k);

            CatKind ret = k;

            if (k is CatSelfType)
            {
                ret = k;
            }
            else if (k is CatFxnType)
            {
                ret = ApplyToFxnType(k as CatFxnType, visited);
            }
            else if (k.IsKindVar())
            {
                if (mUnifiers.ContainsKey(k.ToString()))
                {
                    CatKind unifier = mUnifiers[k.ToString()];

                    if (unifier is CatFxnType)
                        unifier = RenameFreeVars(unifier as CatFxnType);

                    ret = ApplyUnifiers(unifier, visited);
                }
                else
                    ret = k;
            }
            else if (k is CatTypeVector)
            {
                ret = ApplyToTypeVector(k as CatTypeVector, visited);
            }
            else
            {
                ret = k;
            }

            visited.Pop();
            return ret;
        }

        CatTypeVector ApplyToTypeVector(CatTypeVector vec, Stack<CatKind> visited)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
                ret.Add(ApplyUnifiers(k, visited));
            return ret;
        }
    }
}
