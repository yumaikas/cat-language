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
            AddNameConstraint(x.GetName(), y);
            AddNameConstraint(y.GetName(), x);

            if (x is CatSimpleStackKind && y is CatSimpleStackKind)
            {
                CatSimpleStackKind a = x as CatSimpleStackKind;
                CatSimpleStackKind b = y as CatSimpleStackKind;
                AddConstraint(a.GetTop(), b.GetTop());
                AddConstraint(a.GetRest(), b.GetRest());
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
            AddNameConstraint(x.GetName(), y);
            AddNameConstraint(y.GetName(), x);

            if (x is CatFxnType && y is CatFxnType)
                AddFxnConstraint(x as CatFxnType, y as CatFxnType);
        }

        private void AddFxnConstraint(CatFxnType x, CatFxnType y)
        {
            AddConstraint(x.GetProd(), y.GetProd());
            AddConstraint(x.GetCons(), y.GetCons());
        }

        // I know that type kinds and stack kinds are created in response 
        // to the constraints given, the question simply is to get them.
        
        // The problem I want to avoid is aliasing. This bit me on the ass last time.
        // How do I know that two types are the same? I'd better using names.
        // How do I name production and consumption? Well everythign will have a 
        // unique name assigned automatically. 

        // So looking at constraints, they are going to be two directions.
        //  

        /// <summary>
        /// Will find a replacement for the stack if it exists.
        /// Otherwise it will create a new stack, by looking up the top item,
        /// and looking for a replacement stack. 
        /// In the degenerate case
        /// </summary>
        public CatStackKind GetNewStack(CatStackKind x)
        {
            CatStackKind ret = null;
            if (!stkConstraints.ContainsKey(x.GetName()))
            {
                if (x.HasTop())
                {
                    CatTypeKind t = GetNewType(x.GetTop());
                    CatStackKind r = GetNewStack(x.GetRest());
                    ret = new CatStackKind(t, r);
                }
                else
                {
                    ret = x;
                }
            }
            else
            {
                List<CatStackKind> list = stkConstraints[x.GetName()];
                Trace.Assert(list.count > 0);
                CatStackKind y = list[0];
                for (int i = 1; i < list.Count; ++i)
                {
                    CatStackKind z = list[i];
                    if (z.Depth() > y.Depth())
                        y = z;
                }
                CatTypeKind t = GetNewType(y.GetTop());
                CatStackKind r = GetNewStack(r.GetRest());
            }
            return ret;
        }

        public void MergeConstraints(string sKey, List<CatKind> list)
        {
            List<CatKind> tmp;
            if (mConstraints.TryGetValue(sKey, tmp))
            {
                mConstraints.Remove(sKey);
                foreach (CatKind k in tmp)
                {
                    list.Add(k);
                    MergeConstraints(k.GetName(), list);
                }
            }
        }

        public FxnType ResolveConstraints(FxnType t)
        {
            int n = 0;
            List<List<CatKind>> pNewTypes = new List<List<CatKind>>();
            
            while (mConstraints.Count > 0)
            {
                string sKey = mConstraints.Keys[0];
                List<CatKind> list = new List<CatKind>();
                MergeConstraints(sKey, list);
                pNewTypes.Add(list);
            }

            // TODO: create a reverse name lookup.
            // Dictionary<string, int> pNewTypeLookup = new Dictionary<string, List<CatKind>>();

            // There is also another interesting case. A B = C B then A = C  
            // This is an important extra reduction step.


            /*
            CatStackPairKind x;
            CatStackKind y;

            {
                // We may have a stack on stack condition, 
                if (x is CatStackPairKind)
                {
                    AddStackPairConstraints(x as CatStackPairKind, y);                    
                }
                else if (y is CatStackPairKind)
                {
                    AddStackPairConstraints(y as CatStackPairKind, x);
                }
            }
            if (y is CatStackPairKind)
            {
                // Look at the tops of both 
                // if the same, 
                CatStackPairKind tmp = y as CatStackPairKind;
                 * This should be a job for the resolution engine I think.
                 * 
                if (x.GetTop().Equals(tmp.GetTop()))
                {
                    return AddStackConstraint(x.GetRest(), tmp.GetRest());
                }
                else
                {
                    // possiblye look for something equal deeper down (A,B,C) = (D,B,E) well 
                    // does that say anything? 
                }
            }
            else if (y is CatSimpleStackKind)
            {
                CatSimpleStackKind tmp = y as CatSimpleStackKind;
                // if tmp.Rest() == x.Rest()
            }
            else
            {
                throw new Exception("unrecognized stack kind " + y.ToString());
            }
                 */
        }
    }

    /// <summary>
    /// All CatKinds should be immutable. This avoids a lot of problems and confusion.
    /// </summary>
    class CatKind : IEqualityComparer<CatKind>
    {
        // This is used for different function types.
        public static int id = 0;
        
        string msName;

        public CatKind(string s)
        {
            msName = s;
        }

        public string GetName()
        {
            return msName;
        }

        #region IEqualityComparer<CatKind> Members

        public bool Equals(CatKind x, CatKind y)
        {
            return x.GetName().Equals(y.GetName());
        }

        public int GetHashCode(CatKind obj)
        {
            return obj.GetName().GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Base class for the different Cat types
    /// </summary>
    class CatTypeKind : CatKind
    {
    }

    class CatStackKind : CatKind
    {
        public CatStackKind Push(CatKind x)
        {
            if (x is CatStackKind)
                return new CatStackPairKind(this, x as CatStackKind);
            else if (x is CatTypeKind)
                return new CatStackKind(this, x as CatTypeKind);
            else
                throw new Exception("unhandled kind " + x.ToString());
        }
    }

    class CatSimpleStackKind : CatStackKind
    {
        public CatSimpleStackKind(CatStackKind r, CatTypeKind t)
            : base(t.GetName() + "." + r.GetName())
        {
            top = t;
            rest = r;
        }

        CatTypeKind top;
        CatStackKind rest;

        public int Depth()
        {
            return 1 + rest.Depth();
        }

        public CatTypeKind GetTop()
        {
            return top;
        }

        public CatStackKind GetRest()
        {
            return rest;
        }
        
        public bool HasTop()
        {
            return GetTop() != null;
        }
    }

    /// <summary>
    /// Represents a stack with a stack on top.
    /// </summary>
    class CatStackPairKind : CatKind
    {
        CatStackKind top;
        CatStackKind rest;

        public CatStackPairKind(string sName, CatStackKind pRest, CatStackKind pTop)
            : base(sName)
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

        override public int Depth()
        {
            return top.Depth() + rest.Depth();
        }
    }


    class CatStackVar : CatStackKind
    {
        override public CatTypeKind GetTop()
        {
            return null;
        }
        override public CatStackKind GetRest()
        {
            return null;
        }
        override public int Depth()
        {
            return 1;
        }

        public CatStackVar(string s)
            : base(s)
        {
        }
    }

    class CatFxnType : CatTypeKind
    {
        CatStackKind prod;
        CatStackKind cons;
        CatStackKind rho;
        
        Dictionary<string, CatKind> names = new List<CatKind>();

        public CatFxnType(string sName, string sType) 
            : base("unnamed")
        {
            // TODO: create a function type 
        }

        public CatFxnType()
            : base("unnamed")
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
    }

    class CatComposedFxn : CatFxnType
    {
        public CatComposedFxn(CatFxnType x, CatFxnType y, Constraints c)
        {
            AddToConsumption(x.GetCons());
            AddToProduction(y.GetProd());
            c.AddConstraint(x.GetProd(), y.GetCons());
            ResolveConstraints(c);
        }
    }

    class CatQuotedFxn : CatFxnType
    {
        public CatQuotedFxn(CatFxnType f)
        {
            AddToProduction(y.GetProd());
        }
    }
  
    class CatTypeVar : CatTypeKind
    {
        public CatTypeVar(string s)
            : base(s)
        { }
    }

    class CatPrimitiveType : CatTypeKind
    {
        public CatPrimitiveType(string s)
            : base(s)
        { }
    }

    class CatParameterizedType : CatTypeKind
    {
    }

    class CatAlgebraicType : CatTypeKind
    {
        public CatTypeKind first;
        public CatTypeKind second;
    }

    class CatUnionType : CatAlgebraicType 
    {
        public CatUnionType(CatTypeKind x, CatTypeKind y)
            : base("union(" + x.GetName() + "," + y.GetName() + ")")
        {
            first = x;
            second = y;
        }
    }

    class CatSumType : CatAlgebraicType 
    {
        public CatSumType(CatTypeKind x, CatTypeKind y)
            : base("sum(" + x.GetName() + "," + y.GetName() + ")")
        {
            first = x;
            second = y;
        }
    }

    class CatProductType : CatAlgebraicType 
    {
        public CatProductType(CatTypeKind x, CatTypeKind y)
            : base("product(" + x.GetName() + "," + y.GetName() + ")")
        {
            first = x;
            second = y;
        }
    }
}
