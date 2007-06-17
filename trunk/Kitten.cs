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
                    if (TermEqualsOrContains(child, var))
                        return true;
                }
                return false;
            }
            else
            {
                return false;
            }
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
                throw new Exception("error abstraction elimination algorithm");

            AstQuoteNode q = new AstQuoteNode(terms.GetRange(0, i));
            terms.RemoveRange(0, i);
            terms.Insert(q);
            terms.Insert(1, new AstNewNode("dip"));

            if (TermEquals(terms[2], var))
            {
                return;
            }
            else if (TermContains(terms[2], var))
            {
                Trace.Assert(terms[2] is AstQuoteNode);
                AstQuoteNode q = terms[2] as AstQuoteNode;
                RemoveTerm(var, q.Terms);
                terms.Insert(3, new AstNameNode("papply"));
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
            if (args.Count == 0) 
                return;
            string var = vars[0];

            int nCnt = CountInstancesOf(var, terms);
            if (nCnt == 0)
            {
                // Remove unused arguments
                terms.Insert(0, new AstNameNode("pop"));
                args.RemoveAt(0);
            }
            else if (nCnt > 1)
            {
                // Create a new name for the used argument
                string sNewVar = GenerateUniqueName(var);
                RenameFirstInstance(var, sNewVar, terms);
                terms.Insert(0, new AstNameNode("dup"));
                args.Add(var);
            }
            else
            {
                RemoveTerm(var, terms);
                vars.RemoveAt(0);
            }

            // Recursive call
            ConvertTerms(args, terms);
            return;
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

            List<string> args = new List<string>();
            foreach (AstParamNode p in d.mParams)
                args.Add(p.ToString());

            ConvertTerms(args, d.mTerms);
            
            if (Config.gbShowPointFreeConversion)
            {
                Console.Write(d.mName + " == ");
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
