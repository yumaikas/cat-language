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

        public static CatFxnType Infer(CatFxnType f, CatFxnType g, bool bVerbose)
        {
            if (f == null) return null;
            if (g == null) return null;

            try
            {
                return gInferer.InferType(f, g, bVerbose);
            }
            catch (Exception e)
            {
                MainClass.WriteLine("Type inference error: " + e.Message);
                return null;
            }
        }

        public static CatFxnType Infer(List<Function> f, bool bVerbose)
        {
            try
            {
                if (f.Count == 0)
                {
                    if (bVerbose)
                        Console.WriteLine("inferred type is ( -> )");
                    return CatFxnType.Create("( -> )");
                }
                else if (f.Count == 1)
                {
                    Function x = f[0];

                    if (bVerbose)
                        Console.WriteLine("inferred type is " + x.GetFxnType().ToString());
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

                        ft = TypeInferer.Infer(ft, y.GetFxnType(), bVerbose);
                        
                        if (ft == null)
                            return null;
                    }
                    return ft;
                }
            }
            catch (Exception e)
            {
                MainClass.WriteLine("Type inference error: " + e.Message);
                return null;
            }
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
        private CatFxnType InferType(CatFxnType left, CatFxnType right, bool bVerbose)
        {
            mUnifiers.Clear();
            VarRenamer renamer = new VarRenamer();
            left = renamer.Rename(left);
            renamer.ResetNames(); 
            right = renamer.Rename(right);

            if (bVerbose)
            {
                MainClass.WriteLine("Types renamed before composing");
                MainClass.WriteLine("left term : " + left.ToString());
                MainClass.WriteLine("right term : " + right.ToString());
            }

            mUnifiers.AddVectorConstraint(left.GetProd(), right.GetCons());

            if (bVerbose)
            {
                // Create a temporary function type showing the type before unfification
                CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());
                MainClass.WriteLine("Result of composition before unification: ");
                MainClass.WriteLine(tmp.ToString());

                MainClass.WriteLine("Unresolved unifiers:");
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());
                MainClass.Write(mUnifiers.ToString());

                MainClass.WriteLine("Resolved unifiers:");
            }

            Dictionary<string, CatKind> unifiers = mUnifiers.GetResolvedUnifiers();
            renamer = new VarRenamer(unifiers);

            if (bVerbose)
            {                
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }                

            // The left consumption and right production make up the result type.
            CatTypeVector stkLeftCons = renamer.Rename(left.GetCons());
            CatTypeVector stkRightProd = renamer.Rename(right.GetProd());
            
            // Finally create and return the result type
            CatFxnType ret = new CatFxnType(stkLeftCons, stkRightProd, left.HasSideEffects() || right.HasSideEffects());

            if (bVerbose)
            {
                MainClass.WriteLine("Composed type: " + ret.ToString());
            }

            // And one last renaming for good measure:
            renamer = new VarRenamer();
            return renamer.Rename(ret);
        }

        public void OutputUnifiers(Dictionary<string, CatKind> unifiers)
        {
            MainClass.WriteLine("Unifiers:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
    }
}
