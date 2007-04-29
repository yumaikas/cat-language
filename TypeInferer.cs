using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    class Constraints
    {
        Dictionary<string, List<CatKind>> mConstraints
            = new Dictionary<string, List<CatKind>>();

        public void AddStackConstraint(CatStackKind x, CatStackKind y)
        {
            AddNameConstraint(x.ToString(), y);
            AddNameConstraint(y.ToString(), x);

            if (x is CatSimpleStackKind && y is CatSimpleStackKind)
            {
                CatSimpleStackKind a = x as CatSimpleStackKind;
                CatSimpleStackKind b = y as CatSimpleStackKind;
                AddTypeConstraint(a.GetTop(), b.GetTop());
                AddStackConstraint(a.GetRest(), b.GetRest());
            }
        }

        private void AddNameConstraint(string s, CatKind k)
        {
            if (!mConstraints.ContainsKey(s))
                mConstraints.Add(s, new List<CatKind>());
            List<CatKind> list = mConstraints[s];
            if (!list.Contains(k))
                list.Add(k);
        }

        private void AddTypeConstraint(CatTypeKind x, CatTypeKind y)
        {
            AddNameConstraint(x.ToString(), y);
            AddNameConstraint(y.ToString(), x);

            if (x is CatFxnType && y is CatFxnType)
                AddFxnConstraint(x as CatFxnType, y as CatFxnType);
        }

        private void AddFxnConstraint(CatFxnType x, CatFxnType y)
        {
            AddStackConstraint(x.GetProd(), y.GetProd());
            AddStackConstraint(x.GetCons(), y.GetCons());
        }

        public void MergeConstraints(string sKey, List<CatKind> list)
        {
            List<CatKind> tmp;            

            if (mConstraints.TryGetValue(sKey, out tmp))
            {
                mConstraints.Remove(sKey);
                foreach (CatKind k in tmp)
                {
                    list.Add(k);
                    MergeConstraints(k.ToString(), list);
                }
            }
        }

        public CatFxnType ResolveConstraints()
        {
            List<List<CatKind>> pNewTypes = new List<List<CatKind>>();

            string[] keys = new string[mConstraints.Keys.Count];
            mConstraints.Keys.CopyTo(keys, 0);
            foreach (string sKey in keys)
            {
                List<CatKind> list = new List<CatKind>();
                MergeConstraints(sKey, list);
                pNewTypes.Add(list);
            }

            // TODO: create a reverse name lookup.
            // Dictionary<string, int> pNewTypeLookup = new Dictionary<string, List<CatKind>>();

            // There is also another interesting case. A B = C B then A = C  
            // This is an important extra reduction step.
            return null;
        }

        public void OutputConstraints()
        {
            MainClass.WriteLine("Constraints:");
            foreach (KeyValuePair<string, List<CatKind>> kvp in mConstraints)
            {
                foreach (CatKind k in kvp.Value)
                    MainClass.WriteLine(kvp.Key + "=" + k.ToString());
            }
        }
    }
}
