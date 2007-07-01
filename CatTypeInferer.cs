/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class TypeInferer
    {
        Unifiers mUnifiers = new Unifiers();

        static TypeInferer gInferer = new TypeInferer();

        public static CatFxnType Infer(CatFxnType f, CatFxnType g, bool bVerbose, bool bCheck)
        {
            if (f == null) return null;
            if (g == null) return null;
            return gInferer.InferType(f, g, bVerbose, bCheck);
        }

        public static CatFxnType Infer(List<Function> f, bool bVerbose, bool bCheck)
        {
            if (f.Count == 0)
            {
                if (bVerbose)
                    MainClass.WriteLine("type is ( -> )");
                return CatFxnType.Create("( -> )");
            }
            else if (f.Count == 1)
            {
                Function x = f[0];

                if (bVerbose)
                    MainClass.WriteLine("type of " + x.msName + " is " + x.GetTypeString());
                return x.GetFxnType();
            }
            else
            {
                Function x = f[0];
                CatFxnType ft = x.GetFxnType();
                if (bVerbose)
                    MainClass.WriteLine("initial term = " + x.GetName() + " : " + x.GetTypeString());

                for (int i = 1; i < f.Count; ++i)
                {
                    if (ft == null)
                        return ft;
                    Function y = f[i];
                    if (bVerbose)
                    {
                        MainClass.WriteLine("Composing accumulated terms with next term");
                        MainClass.Write("previous terms = { ");
                        for (int j = 0; j < i; ++j)
                            MainClass.Write(f[j].GetName() + " ");
                        MainClass.WriteLine("} : " + ft.ToString());
                        MainClass.WriteLine("next term = " + y.GetName() + " : " + y.GetTypeString());
                    }

                    ft = TypeInferer.Infer(ft, y.GetFxnType(), bVerbose, bCheck);
                    
                    if (ft == null)
                        return null;
                }
                return ft;
            }
        }

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

        CatTypeVector UnifyTypeVector(CatTypeVector vec, Dictionary<string, CatKind> u, Dictionary<string, CatKind> gen, Stack<CatKind> visited)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
                ret.Add(Unify(k, u, visited));            
            return ret;
        }

        /*
        void GetFreeVars(CatFxnType dest, CatTypeVector src, Dictionary<string, CatKind> vars, Stack<CatKind> visited)
        {
            foreach (CatKind k in src.GetKinds())
            {
                if (k is CatFxnType && !(k is CatSelfType))
                    GetFreeVars(k as CatFxnType, dest, vars);
                else if (k is CatKind)
                    vars.Remove(k.ToString());
            }                
        }

        void GetFreeVars(CatFxnType dest, CatFxnType src, Dictionary<string, CatKind> vars)
        {
            if (src == dest) 
                return;
            GetFreeVars(dest, src.GetCons(), vars);
            GetFreeVars(dest, src.GetProd(), vars);
        }

        CatFxnType RenameFreeVars(CatFxnType env1, CatFxnType env2, CatFxnType ft)
        {
            Dictionary<string, CatKind> free = new Dictionary<string, CatKind>();
            ft.GetAllVars(free);
            GetFreeVars(ft, env1, free);
            GetFreeVars(ft, env2, free);

            string[] keys = new string[free.Count];
            free.Keys.CopyTo(keys, 0);
            foreach (string s in keys)
            {
                CatKind k = free[s];
                Trace.Assert(k.IsKindVar());
                if (k is CatTypeVar)
                    free[s] = CatTypeVar.CreateUnique();
                else 
                    free[s] = CatTypeVar.CreateUnique();
            }

            return RenameVars(ft, free);
        }

        CatTypeVector RenameVars(CatTypeVector vec, Dictionary<string, CatKind> vars)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
            {
                if (k.IsKindVar() && vars.ContainsKey(k.ToString()))
                    ret.Add(vars[k.ToString()]);
                else if (k is CatSelfType)
                    ret.Add(k);
                else if (k is CatFxnType)
                    ret.Add(RenameVars(ret, vars));
                else if (k is CatTypeVector)
                    throw new Exception("unexpected type vector in function during renaming");
                else
                    ret.Add(k);
            }
            return ret;
        }

        CatFxnType RenameVars(CatFxnType ft, Dictionary<string, CatKind> vars)
        {
            return new CatFxnType(RenameVars(ft.GetCons(), vars), RenameVars(ft.GetProd(), vars), ft.HasSideEffects());
        }
        */

        /// <summary>
        /// A composed function satisfy the type equation 
        /// 
        ///   ('A -> 'B) ('C -> 'D) compose : ('A -> 'D) with constraints ('B == 'C)
        /// 
        /// This makes the raw type trivial to determine, but the result isn't helpful 
        /// because 'D is not expressed in terms of the variables of 'A. The goal of 
        /// type inference is to find new variables that unify 'A and 'C based on the 
        /// observation that the production of the left function must be equal to the 
        /// consumption of the second function
        /// </summary>
        private CatFxnType InferType(CatFxnType left, CatFxnType right, bool bVerbose, bool bCheck)
        {
            mUnifiers.Clear();
            VarRenamer renamer = new VarRenamer();

            left = renamer.Rename(left.AddImplicitRhoVariables());
            renamer.ResetNames();
            right = renamer.Rename(right.AddImplicitRhoVariables());

            if (bVerbose)
            {
                MainClass.WriteLine("Types renamed before composing");
                MainClass.WriteLine("left term : " + left.ToString());
                MainClass.WriteLine("right term : " + right.ToString());
            }

            // This recursively adds constraints as needed 
            mUnifiers.AddVectorConstraint(left.GetProd(), right.GetCons());

            // Create a temporary function type showing the type before unfification
            CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());

            if (bVerbose)
            {
                MainClass.WriteLine("Result of composition before unification: ");
                MainClass.WriteLine(tmp.ToString());

                MainClass.WriteLine("Constraints:");
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());
                MainClass.WriteLine(mUnifiers.ToString());
            }

            Dictionary<string, CatKind> unifiers = mUnifiers.GetResolvedUnifiers();
            
            if (bVerbose)
            {
                MainClass.WriteLine("Unifiers:");
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }                

            // Replace all vars with unifiers
            Stack<CatKind> visited = new Stack<CatKind>();
            CatFxnType ret = Unify(tmp, unifiers, visited) as CatFxnType;
            Trace.Assert(visited.Count == 0);

            if (bVerbose)
                MainClass.WriteLine("Inferred type (before renaming): " + ret.ToString());

            // And one last renaming for good measure:
                renamer = new VarRenamer();
            ret = renamer.Rename(ret);

            if (bVerbose)
            {
                MainClass.WriteLine("Inferred type: " + ret.ToString());
                MainClass.WriteLine("");
            }

            if (bCheck)
                ret.CheckIfWellTyped();

            return ret;
        }

        public void OutputUnifiers(Dictionary<string, CatKind> unifiers)
        {
            MainClass.WriteLine("Unifiers:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
    }
}
