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

        public CatFxnType ResolveConstraints(CatFxnType t)
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
    }

    /// <summary>
    /// All CatKinds should be immutable. This avoids a lot of problems and confusion.
    /// </summary>
    class CatKind : IEqualityComparer<CatKind>
    {
        // This is used for different function types.
        public static int id = 0;
        
        public CatKind()
        {
        }

        public override string ToString()
        {
            return "???";
        }

        #region IEqualityComparer<CatKind> Members

        public bool Equals(CatKind x, CatKind y)
        {
            return x.ToString().Equals(y.ToString());
        }

        public int GetHashCode(CatKind obj)
        {
            return obj.ToString().GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Base class for the different Cat types
    /// </summary>
    class CatTypeKind : CatKind
    {
        public CatTypeKind()
        { }
    }

    class CatStackKind : CatKind
    {
        public CatStackKind Push(CatKind x)
        {
            if (x is CatStackKind)
                return new CatStackPairKind(this, x as CatStackKind);
            else if (x is CatTypeKind)
                return new CatSimpleStackKind(this, x as CatTypeKind);
            else
                throw new Exception("unhandled kind " + x.ToString());
        }
    }

    class CatSimpleStackKind : CatStackKind
    {
        public CatSimpleStackKind(CatStackKind r, CatTypeKind t)
        {
            top = t;
            rest = r;
        }

        CatTypeKind top;
        CatStackKind rest;

        public CatTypeKind GetTop()
        {
            return top;
        }

        public CatStackKind GetRest()
        {
            return rest;
        }

        public override string ToString()
        {
            return (rest.ToString() + "." + top.ToString());
        }
    }

    /// <summary>
    /// Represents a stack with a stack on top.
    /// </summary>
    class CatStackPairKind : CatStackKind
    {
        CatStackKind top;
        CatStackKind rest;

        public CatStackPairKind(CatStackKind pRest, CatStackKind pTop)
        {
            top = pTop;
            rest = pRest;
        }

        public CatStackKind GetTop()
        {
            return top;
        }

        public CatStackKind GetRest()
        {
            return rest;
        }

        public override string ToString()
        {
            return GetRest().ToString() + " " + GetTop().ToString();
        }
    }


    class CatStackVar : CatStackKind
    {
        string msName;

        public CatStackVar(string s)
        {
            msName = s;
        }

        public override string ToString()
        {            
            return msName;
        }
    }

    class CatFxnType : CatTypeKind
    {
        CatStackKind rho;
        CatStackKind prod;
        CatStackKind cons;
        
        public CatFxnType(string sType) 
            : this()
        {
            // TODO: create a function type from the type.
        }

        public CatFxnType()
        {
            rho = new CatStackVar("$rho" + (id++).ToString());
            prod = rho;
            cons = rho;
        }

        public void AddToProduction(CatKind x)
        {
            prod = prod.Push(x);
        }

        public void AddToConsumption(CatKind x)
        {
            cons = cons.Push(x);
        }

        public CatStackKind GetProd()
        {
            return prod;
        }

        public CatStackKind GetCons()
        {
            return cons;
        }

        public override string ToString()
        {
            return "(" + GetProd().ToString() + " -> " + GetCons().ToString() + ")";
        }
    }

    class CatComposedFxn : CatFxnType
    {
        public CatComposedFxn(CatFxnType x, CatFxnType y, Constraints c)
        {
            AddToConsumption(x.GetCons());
            AddToProduction(y.GetProd());
            c.AddStackConstraint(x.GetProd(), y.GetCons());
        }
    }

    class CatQuotedFxn : CatFxnType
    {
        public CatQuotedFxn(CatFxnType f)
        {
            AddToProduction(f.GetProd());
        }
    }
  
    class CatTypeVar : CatTypeKind
    {
        string msName;

        public CatTypeVar(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }
    }

    class CatPrimitiveType : CatTypeKind
    {
        string msName;

        public CatPrimitiveType(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }
    }

    class CatParameterizedType : CatTypeKind
    {
        CatTypeKind mType;
        string msName;

        public CatParameterizedType(string s, CatTypeKind t)
        {
            msName = s;
            mType = t;
        }

        public override string ToString()
        {
            return msName + "(" + mType.ToString() + ")";
        }
    }

    class CatAlgebraicType : CatTypeKind
    {
        public CatTypeKind first;
        public CatTypeKind second;
    }

    class CatUnionType : CatAlgebraicType 
    {
        public CatUnionType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "union(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    class CatSumType : CatAlgebraicType 
    {
        public CatSumType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "sum(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    class CatProductType : CatAlgebraicType 
    {
        public CatProductType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "product(" + first.ToString() + "," + second.ToString() + ")";
        }
    }
}
