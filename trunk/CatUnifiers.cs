using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    public class Unifiers
    {
        // TODO: generate new names each time the unifier is forced into a constraining a function. 
        // Use an object state.


        CatFxnType UnifyFxnType(CatFxnType ft, Dictionary<string, CatKind> u, Stack<CatKind> visited)
        {
            CatTypeVector cons = UnifyTypeVector(ft.GetCons(), u, visited);
            CatTypeVector prod = UnifyTypeVector(ft.GetProd(), u, visited);
            return new CatFxnType(cons, prod, ft.HasSideEffects());
        }

        CatKind Unify(CatKind k, Dictionary<string, CatKind> u, Stack<CatKind> visited)
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
                ret = UnifyFxnType(k as CatFxnType, u, visited);
            }
            else if (k is CatTypeVar)
            {
                if (u.ContainsKey(k.ToString()))
                    ret = Unify(u[k.ToString()], u, visited);
                else
                    ret = CatTypeVar.CreateUnique();
            }
            else if (k is CatStackVar)
            {
                if (u.ContainsKey(k.ToString()))
                    ret = Unify(u[k.ToString()], u, visited);
                else
                {
                    ret = CatStackVar.CreateUnique();
                }
            }
            else if (k is CatTypeVector)
            {
                throw new Exception("Unexpected type vector during unification");
            }
            else
            {
                ret = k;
            }

            visited.Pop();
            return ret;
        }

        CatTypeVector UnifyTypeVector(CatTypeVector vec, Dictionary<string, CatKind> u, Stack<CatKind> visited)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
                ret.Add(Unify(k, u, visited));
            return ret;
        }
    }
}
