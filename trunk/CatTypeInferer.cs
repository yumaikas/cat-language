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
        Constraints mConstraints = new Constraints();

        #region static fields and methods
        static TypeInferer gInferer = new TypeInferer();

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
                    OutputInferredType(x.GetFxnType());
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

                    // Object field functions (_def_, _get_, _set_) have to be 
                    // computed at the last minute, because their types are dependent on the previous types
                    if (y is ObjectFieldFxn)
                    {
                        (y as ObjectFieldFxn).ComputeType(ft);
                    }
                    
                    ft = TypeInferer.Infer(ft, y.GetFxnType(), bVerbose, bCheck);

                    if (ft == null)
                        return null;
                }
                return ft;
            }
        }

        public static CatFxnType Infer(CatFxnType f, CatFxnType g, bool bVerbose, bool bCheck)
        {
            if (f == null) return null;
            if (g == null) return null;
            return gInferer.InferType(f, g, bVerbose, bCheck);
        }

        public static void OutputInferredType(CatFxnType ft)
        {
            MainClass.WriteLine("After rewriting");
            MainClass.WriteLine("ML style type: " + ft.ToPrettyString(true));
            MainClass.WriteLine("Cat style:     " + ft.ToPrettyString(false));
            MainClass.WriteLine("");
        }
        #endregion

        /// <summary>
        /// This is a help function for ReplaceWithVars(CatFxnType ft)
        /// </summary>
        public CatTypeVector ReplaceWithVars(CatTypeVector vec)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatFxnType)
                    ret.Add(ReplaceWithVars(k as CatFxnType));
                else
                {
                    if (k.IsKindVar())
                        ret.Add(k);
                    else
                    {
                        CatTypeVar v = CatTypeVar.CreateUnique();
                        ret.Add(v);
                        mConstraints.AddConstraint(v.ToString(), k);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// This assures that any simple types, or custom types, will be replaced 
        /// with a type variable, and a cosntraint will be generated. 
        /// This simplifies things a bit, so that the algorithms don't have to 
        /// worry about edge cases.
        /// </summary>
        public CatFxnType ReplaceWithVars(CatFxnType ft)
        {
            if (ft is CatSelfType)
                return ft;
            return new CatFxnType(ReplaceWithVars(ft.GetCons()), ReplaceWithVars(ft.GetProd()), ft.HasSideEffects());
        }

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
            mConstraints.Clear();
            VarRenamer renamer = new VarRenamer();

            left = renamer.Rename(left.AddImplicitRhoVariables());
            renamer.ResetNames();
            right = renamer.Rename(right.AddImplicitRhoVariables());
            
            CatFxnType varOnlyLeft = ReplaceWithVars(left);
            CatFxnType varOnlyRight = ReplaceWithVars(right);

            if (bVerbose)
            {
                MainClass.WriteLine("Types renamed before composing");
                MainClass.WriteLine("left term : " + left.ToString());
                MainClass.WriteLine("right term : " + right.ToString());
            }

            mConstraints.AddSelfTypes(left);
            mConstraints.AddSelfTypes(right);

            // This recursively adds constraints as needed 
            mConstraints.AddVectorConstraint(left.GetProd(), right.GetCons());

            // Create a temporary function type showing the type before unfification
            CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());

            if (bVerbose)
            {
                MainClass.WriteLine("Self types");
                foreach (KeyValuePair<CatSelfType, CatFxnType> kvp in mConstraints.GetSelfTypes())
                    MainClass.Write("self = " + kvp.Value.ToPrettyString(false));

                MainClass.WriteLine("Composition before unification: ");
                MainClass.WriteLine(tmp.ToString());

                MainClass.WriteLine("Constraints:");
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());
                MainClass.WriteLine(mConstraints.ToString());
            }

            TypeVarList unifiers = mConstraints.GetResolvedUnifiers();
            
            if (bVerbose)
            {
                MainClass.WriteLine("Unifiers:");
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }                

            // Replace all vars with unifiers
            Stack<CatKind> visited = new Stack<CatKind>();
            UnificationApplier ua = new UnificationApplier(tmp, unifiers);
            CatFxnType ret = ua.ApplyUnifiers(tmp, visited) as CatFxnType;
            Trace.Assert(visited.Count == 0);

            if (bVerbose)
                MainClass.WriteLine("Inferred type (before renaming): " + ret.ToString());

            // And one last renaming for good measure:
            renamer = new VarRenamer();
            ret = renamer.Rename(ret);

            if (bVerbose)
                OutputInferredType(ret);

            return ret;
        }

        public void OutputUnifiers(TypeVarList unifiers)
        {
            MainClass.WriteLine("UnificationApplier:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
    }
}
