using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

/*
 * Some challenges 
 * - coming up with good names for things. 
 * - figuring out if two things have the same name
 * - dealing with stack pairs. 
 * - dealing with recursive functions (e.g. fib or fact)
 * 
*/

namespace Cat
{
    class TypeInferer
    {
    }

    class Constraints
    {
        Dictionary<string, List<CatKind>> mConstraints
            = new Dictionary<string, List<CatKind>>();
        /*
        Dictionary<string, List<CatTypeKind>> typeConstraints 
            = new Dictionary<string, List<CatTypeKind>>();

        Dictionary<string, List<CatStackKind>> stkConstraints
            = new Dictionary<string, List<CatStackKind>>();
         */

        // Is this going to work? 
        // We have issues of StackKinds containing StackKinds. 
        // That is a bit confusing to be honest. I am not sure it is a good idea.

        // There is going to be a problem because functions can refer to 
        // type variables that exist in the top-level. 

        // the resolution process involves creating new types that resolve the 
        // constraints
        // we are done when no constraints exist. 

        // I believe stack constraints are one-way? Or are they. 
        // Probably not. How do I choose? 
        // Are bigger stacks always correct?

        // One thing to note: 

        // Will everything just work, if I assure that all constraints are two directional.

        // What about type ('A ('A -> 'B) -> 'B) is there an implicit rho in these variables? 
        // This reveals something interesting, stacks can have stacks on top.
        // I have t o make sure I handle this case.

        // Perhaps nested functions don't have implicit "rho" variables? Well they do. 
        // Consider: (('A -> 'B) ('C -> 'D) -> ('A 'C -> 'B 'D))
        // Now I can create a new kind of stack kind, a stack with a stack on top.
        // I am not very happy about that. 
        // The concern becomes how do I equate constraints. I can say things about the top 
        // but not the botoom. 

        // naming is going to be a problem. 
        // 

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

        /*
        private void AddStackPairConstraints(CatStackPairKind x, CatStackKind y)
        {
            else
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
                /*
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
        }
        */

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
            {
                AddFxnConstraint(x as CatFxnType, y as CatFxnType);
            }

            // Don't forget, constraints are commutative:
            // x = y, y = z => x = z;
            // Deal with that as well. This can lead to constraint explosion, possibly. 
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

        public void ResolveConstraints(Constraints c)
        {
            // TODO: print out constraints for now.
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
