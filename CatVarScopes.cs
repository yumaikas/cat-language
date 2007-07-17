using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public struct VarScope
    {
        public string mName;
        public CatFxnType mScope;
        public VarScope(string s, CatFxnType ft)
        {
            mScope = ft;
            mName = s;
        }
    }

    public class VarScopes : List<VarScope>
    {
        public void Add(CatKind k, CatFxnType ft)
        {
            Trace.Assert(k.IsKindVar());
            Add(k.ToString(), ft);
        }

        public void Add(string s, CatFxnType ft)
        {
            if (HasScope(s, ft))
                return;
            base.Add(new VarScope(s, ft));
        }

        public IEnumerable<CatFxnType> GetAssociatedScopes(CatKind k)
        {
            Trace.Assert(k.IsKindVar());
            return GetAssociatedScopes(k.ToString());
        }

        public IEnumerable<CatFxnType> GetAssociatedScopes(string s)
        {
            foreach (VarScope vs in this)
                if (vs.mName == s)
                    yield return vs.mScope;
        }

        public bool HasScope(string s, CatFxnType ft)
        {
            foreach (VarScope vs in this)
                if (vs.mName.Equals(s) && vs.mScope == ft)
                    return true;
            return false;
        }

        public bool IsFreeVar(CatFxnType context, CatKind k)
        {
            // Is it contained in a function outside of the context? 
            // Find all scopes associated with the kind
            if (!k.IsKindVar())
                return false;
            foreach (CatFxnType tmp in GetAssociatedScopes(k))
            {
                if (!tmp.DescendentOf(context))
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            string ret = "";
            foreach (VarScope vs in this)
                if (vs.mScope == null)
                    ret += vs.mName + " in __top__\n"; else                
                    ret += vs.mName + " in " + vs.mScope.ToIdString() + "\n";
            return ret;
        }
    }
}
