using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

/*
 * The big challenge is going to be: 
 * eval eval 
 * where eval : (R A (S A -> S B) -> R B)
 * so eval eval : 
 * (R A (S A -> S B) -> R B) (R0 A0 (S0 A0 -> S0 B0) -> R0 B0)
 * => R B  R0 A0 (S0 A0 -> S0 B0) 
 * => B = (S0 A0 -> S0 B0) || R = (S0 A0 -> S0 B0) && B = empty
 *
 * Alternatively: 
 * (A (A -> B) -> B) (A0 (A0 -> B0) -> B0)
 * B = A0 (A0 -> B0)
 * if (B = empty) A = (A0 -> B0) C 
 * 
 * Consider: 
 * [] [] (eval eval)
 * Should work shouldn't it?
 * 
 * The only requirement is that (r B) = (s (A0 -> B0))
 * 
 * The type is: 
 * ee : (A (A -> C (B -> D)) -> D)
 * 
 * What happens when I write: 
 * [] ee ? 
 * 
 * (r -> r (s -> s)) (A (A -> C (B -> D)) -> D)
 * result = (r -> D)
 * axiom s -> s = (A -> C (B -> D)) 
 * axiom s = A 
 * axiom s = C (B -> D)
 * axiom r = A 
 * s = r because s = A and r = A
 * r = C (B -> D)
 * unified result = (C (B -> D) -> D)
 * 
 * forall qualifiers are magical. 
 * perhaps they can be merged.
 * (r 'A (s 'A -> s 'B) -> r 'B)
 * is the same as: 
 * (r 'A (r 'A -> r 'B) -> r 'B)
 * Well it means that forall 
 
*/

namespace Cat
{
    class TypeInferer
    {
    }

    class Constraints
    {
        Dictionary<string, List<CatTypeKind>> typeConstraints 
            = new Dictionary<string, List<CatTypeKind>>();

        Dictionary<string, List<CatStackKind>> stkConstraints
            = new Dictionary<string, List<CatStackKind>>();

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

        public void AddConstraint(CatTypeKind x, CatTypeKind y)
        {
            typeConstraints.Add(x.GetName(), y);
            typeConstraints.Add(y.GetName(), x);

            // Don't forget, constraints are commutative:
            // x = y, y = z => x = z;
            // Deal with that!
        }

        public void AddConstraint(CatStackKind x, CatStackKind y)
        {
            stkConstraints.Add(x.GetName(), y);
            stkConstraints.Add(y.GetName(), x);
        }

        public void AddConstraint(CatFxnType x, CatFxnType y)
        {
            stkConstraints.Add(x.GetName(), y);
            stkConstraints.Add(y.GetName(), x);
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

    class CatKind
    {
        static int id = 0;
        string msName;
        int mnId = id++;

        public CatKind(string s)
        {
            msName = s;
        }

        public string GetName()
        {
            return msName;
        }
    }

    class CatTypeKind : CatKind
    {
    }

    class CatStackKind : CatKind
    {
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

        public CatStackKind(CatTypeKind t, CatStackKind r)
            : base(t.GetName() + "." + r.GetName())
        {
            top = t;
            rest = r;
        }
    }

    class CatStackPairKind : CatKind
    {
        CatStackKind top;
        CatStackKind rest;

        // TODO: finish.
        public CatStackPairKind(CatStackKind x, CatStackKind y)
            : base(x.GetName() + ":" + y.GetName())
        {
            top = x;
            rest = y;
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
        
        Dictionary<string, CatKind> names = new List<CatKind>();

        public CatFxnType(string sType) 
            : base("not named yet")
        {
            CatStackKind cons = new CatStackVar(sName + "$rho");
            CatStackKind prod = cons;

            // Now add 
            cons = cons.Push("a");
            prod = prod.Push("b");
        }

        public CatFxnType(CatFxnType t)
        {
        }

        public void ComposeWith(CatFxnType x)
        {
            // Set consumption to the consumption of x
            // Add constraints to the type variables
            // Add constraints to the stack variables 
            // Update the internal name list 
            // Remove no longer needed names 
            // Expand stack variables where neccessary.
            // Look for constraint cycles
        }

        public CatStackKind GetProd()
        {
            return prod;
        }

        public CatStackKind GetCons()
        {
            return cons;
        }

        public bool IsValid()
        {
            // Make sure that there is always the same kind for both 
        }
    }    
  
    class CatTypeVar : CatTypeKind
    {
    }

    class CatPrimitiveType : CatTypeKind
    {
    }

    class CatParameterizedType : CatTypeKind
    {
    }

    class CatUnionType : CatTypeKind
    {
    }

    class CatSumType : CatTypeKind
    {
    }

    class CatRecordType : CatTypeKind
    {
    }

    class CatProductType : CatTypeKind
    {
    }
}
