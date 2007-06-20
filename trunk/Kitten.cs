/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    /// <summary>
    /// Point-free form is a fancy way of saying: no named parameters.
    /// This class can be used to verify if a Cat program is in point-free
    /// form or to convert it to point-free form.
    /// </summary>
    public static class CatPointFreeForm
    {
        public static int gnId = 0;

        public static void Convert(AstProgram p)
        {
            foreach (AstDefNode x in p.Defs)
                Convert(x);
        }

        public static string GenerateUniqueName(string sArg)
        {
            return sArg + "$" + gnId++.ToString();
        }

        public static void RenameFirstInstance(string sOld, string sNew, List<AstExprNode> terms)
        {
            for (int i = 0; i < terms.Count; ++i)
            {
                if (TermEquals(terms[i], sOld))
                {
                    terms[i] = new AstNameNode(sNew);
                    return;
                }
            }
            throw new Exception(sOld + " was not found in the list of terms");
        }
        
        public static bool TermEquals(AstExprNode term, string var)
        {
            return term.ToString().Equals(var);
        }

        public static bool TermContains(AstExprNode term, string var)
        {
            if (term is AstQuoteNode)
            {
                AstQuoteNode q = term as AstQuoteNode;
                foreach (AstExprNode child in q.Terms)
                {
                    if (TermContainsOrEquals(child, var))
                        return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static int CountInstancesOf(string var, List<AstExprNode> terms)
        {
            int ret = 0;
            foreach (AstExprNode term in terms)
            {
                if (term is AstQuoteNode)
                {
                    AstQuoteNode q = term as AstQuoteNode;
                    ret += CountInstancesOf(var, q.Terms);
                }
                else
                {
                    if (TermEquals(term, var))
                        ret += 1;
                }
            }
            return ret;
        }

        public static bool TermContainsOrEquals(AstExprNode term, string var)
        {
            return TermEquals(term, var) || TermContains(term, var);
        }

        public static void RemoveTerm(string var, List<AstExprNode> terms)
        {
            // Find the first term that either contains, or is equal to the 
            // free variable
            int i = 0;
            while (i < terms.Count)
            {
                if (TermContainsOrEquals(terms[i], var))
                    break;
                ++i;
            }

            if (i == terms.Count)
                throw new Exception("error in abstraction elimination algorithm");

            if (i > 0)
            {
                AstQuoteNode q = new AstQuoteNode(terms.GetRange(0, i));
                terms.RemoveRange(0, i);
                terms.Insert(0, q);
                terms.Insert(1, new AstNameNode("dip"));
                i = 2;
            }
            else
            {
                i = 0;
            }

            if (TermEquals(terms[i], var))
            {
                terms.RemoveAt(i);
                return;
            }
            else if (TermContains(terms[i], var))
            {
                Trace.Assert(terms[i] is AstQuoteNode);
                AstQuoteNode subExpr = terms[i] as AstQuoteNode;
                RemoveTerm(var, subExpr.Terms);
                terms.Insert(i + 1, new AstNameNode("bind"));
                return;
            }
            else
            {
                throw new Exception("error in abstraction elimination algorithm");
            }
        }

        /// <summary>
        /// Converts a list of terms to point-free form. 
        /// </summary>
        public static void ConvertTerms(List<string> vars, List<AstExprNode> terms)
        {
            if (vars.Count == 0) 
                return;

            List<AstExprNode> prolog = new List<AstExprNode>();

            for (int i = 0; i < vars.Count; ++i)
            {
                if (Config.gbShowPointFreeConversion)
                {
                    foreach (AstExprNode expr in prolog)
                        Console.Write(expr.ToString() + " ");
                    Console.Write(" ");
                    foreach (AstExprNode expr in terms)
                        Console.Write(expr.ToString() + " ");
                    Console.WriteLine();
                }

                string var = vars[i];

                if (CountInstancesOf(var, terms) == 0)
                {
                    // Remove unused arguments
                    prolog.Add(new AstNameNode("pop"));
                }
                else
                {
                    while (CountInstancesOf(var, terms) > 1)
                    {
                        // Create a new name for the used argument
                        string sNewVar = GenerateUniqueName(var);
                        RenameFirstInstance(var, sNewVar, terms);
                        prolog.Add(new AstNameNode("dup"));
                        vars.Add(sNewVar);
                    }

                    RemoveTerm(var, terms);
                }
            }
            terms.InsertRange(0, prolog);
        }    

        /// <summary>
        /// This is known as an abstraction algorithm. It converts from 
        /// a form with named parameters to point-free form.
        /// </summary>
        /// <param name="sRightConsequent"></param>
        public static void Convert(AstDefNode d)
        {
            if (IsPointFree(d)) 
                return;

            if (Config.gbShowPointFreeConversion)
            {
                Console.WriteLine();
                Console.WriteLine("Removing free variables");
                Console.Write(d.mName + " = ");
                foreach (AstExprNode expr in d.mTerms)
                    Console.Write(expr.ToString() + " ");
                Console.WriteLine();
            }

            List<string> args = new List<string>();
            foreach (AstParamNode p in d.mParams)
                args.Add(p.ToString());

            ConvertTerms(args, d.mTerms);
            
            if (Config.gbShowPointFreeConversion)
            {
                Console.Write(d.mName + " = ");
                foreach (AstExprNode expr in d.mTerms)
                    Console.Write(expr.ToString() + " ");
                Console.WriteLine();
            }
        }

        public static bool IsPointFree(AstProgram p)
        {
            foreach (AstDefNode d in p.Defs)
                if (!IsPointFree(d))
                    return false;
            return true;
        }

        public static bool IsPointFree(AstDefNode d)
        {
            return d.mParams.Count == 0;
        }
    }
}
