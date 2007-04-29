/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

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
        public static void Convert(AstProgram p)
        {
            foreach (AstDefNode x in p.Defs)
                Convert(x);
        }

        /// <summary>
        /// Converts a list of terms to point-free form. An argument list is implicit at the top of the stack, 
        /// and is maintained there. 
        /// </summary>
        /// <param name="terms"></param>
        public static void ConvertTerms(List<string> args, List<AstExprNode> terms)
        {
            // Convert terms so that they make sense. 
            int i = 0;
            while (i < terms.Count)
            {
                AstExprNode x = terms[i];
                if (x is AstQuoteNode)
                {
                    AstQuoteNode q = x as AstQuoteNode;

                    // Recursively convert all terms in the quotation to use the argument list as well.
                    ConvertTerms(args, q.Terms);
                    i++;

                    // compose the new quotation with function generated from the argument list
                    terms.Insert(i, new AstNameNode("embed_args", "appends argument list to the the front of the quotation, and pops it afterwards"));
                    i++;                    
                }
                else if (x.ToString() == "args")
                {
                    terms[i] = new AstNameNode("dup", "access argument list");
                    i++;
                }
                else if (args.Contains(x.ToString()))
                {
                    int n = args.IndexOf(x.ToString());
                    if (n > 9) throw new Exception("Currently only up to 9 parameters are supported");
                    terms[i] = new AstNameNode("arg" + n.ToString(), "access argument '" + x.ToString() + "'");
                    i++;
                }
                else
                {
                    // This says ... execute the function, below the argument list.
                    terms[i] = new AstQuoteNode(x);
                    i++;
                    terms.Insert(i, new AstNameNode("dip", "execute operation below argument list"));
                    i++;
                }
            }
            terms.Add(new AstNameNode("pop", "remove argument list"));
        }    

        /// <summary>
        /// This is known as an abstraction algorithm. It converts from 
        /// a form with named parameters to point-free form.
        /// </summary>
        /// <param name="d"></param>
        public static void Convert(AstDefNode d)
        {
            if (IsPointFree(d)) 
                return;

            List<string> args = new List<string>();
            foreach (AstParamNode p in d.mParams)
                args.Add(p.ToString());

            // Recursively convert the terms in the function, and in all anonymous 
            // functions.
            ConvertTerms(args, d.mTerms);

            // Add instructions to create a list from the arguments
            // Must be done after the conversion of other terms
            for (int i = 0; i < d.mParams.Count; ++i)
            {
                d.mTerms.Insert(0, new AstNameNode("swons", "append argument '" + d.mParams[i].ToString() + "' to argument list"));
            }

            d.mTerms.Insert(0, new AstNameNode("arg_list", "create an argument list"));

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
