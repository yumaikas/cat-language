/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class Optimizer
    {
        /// <summary>
        /// This is a simple yet effective combination of optimizations.
        /// </summary>
        static public QuotedFunction Optimize(QuotedFunction qf)
        {
            qf = ApplyMacros(qf);
            qf = Expand(qf, 1);
            //qf = ApplyMacros(qf);
            //qf = Expand(qf);
            //qf = ApplyMacros(qf);
            qf = PartialEval(qf);
            //qf = Expand(qf);
            //qf = ApplyMacros(qf);

            // TODO: 
            // identify common expressions 

            return new QuotedFunction(qf);
        }

        #region partial evaluation
        public static Function ValueToFunction(Object o)
        {
            if (o is Int32)
            {
                return new PushValue<int>((int)o);
            }
            else if (o is Double)
            {
                return new PushValue<double>((double)o);
            }
            else if (o is String)
            {
                return new PushValue<string>((string)o);
            }
            else if (o is Boolean)
            {
                bool b = (bool)o;
                if (b)
                    return new Primitives.True();
                else
                    return new Primitives.False();
            }
            else if (o is FList)
            {
                return new PushValue<FList>(o as FList);
            }
            else if (o is QuotedFunction)
            {
                QuotedFunction qf = o as QuotedFunction;
                List<Function> fxns = qf.GetChildren();
                Quotation q = new Quotation(fxns);
                return q;
            }
            else
            {
                throw new Exception("Partial evaluator does not yet handle objects of type " + o);
            }
        }

        /// <summary>
        /// This will reduce an expression by evaluating as much at compile-time as possible.
        /// </summary>
        public static QuotedFunction PartialEval(QuotedFunction qf)
        {
            Executor exec = new Executor();
            return new QuotedFunction(PartialEval(exec, qf.GetChildren()));
        }

        /// <summary>
        /// We attempt to execute an expression (list of functions) on an empty stack. 
        /// When no exception is raised we know that the subexpression can be replaced with anything 
        /// that generates the values. 
        /// </summary>
        static List<Function> PartialEval(Executor exec, List<Function> fxns)
        {
            // Recursively partially evaluate all quotations
            for (int i=0; i < fxns.Count; ++i)
            {
                Function f = fxns[i];
                if (f is Quotation)
                {   
                    Quotation q = f as Quotation;
                    List<Function> tmp = PartialEval(new Executor(), q.GetChildren());
                    fxns[i] = new Quotation(tmp);
                }
            }

            List<Function> ret = new List<Function>();
            object[] values = null;

            int j = 0;
            while (j < fxns.Count)
            {
                try
                {
                    Function f = fxns[j];
                    
                    if (f is DefinedFunction)
                    {
                        f.Eval(exec);
                    }
                    else
                    {
                        if (f.GetFxnType() == null)
                            throw new Exception("no type availables");

                        if (f.GetFxnType().HasSideEffects())
                            throw new Exception("can't perform partial execution when side-effects come into play");

                        f.Eval(exec);
                    }
                    
                    // at each step, we have to get the values stored so far
                    // since they could keep changing and any exception
                    // will obliterate the old values.
                    values = exec.GetStack().ToArray();
                }
                catch 
                {
                    if (values != null)
                    {
                        // Copy all of the values from the previous good execution 
                        for (int k = values.Length - 1; k >= 0; --k)
                            ret.Add(ValueToFunction(values[k]));
                    }
                    ret.Add(fxns[j]);
                    exec.GetStack().Clear();
                    values = null;
                }
                j++;
            }

            if (values != null)
                for (int l = values.Length - 1; l >= 0; --l)
                    ret.Add(ValueToFunction(values[l]));
            
            return ret;
        }
        #endregion

        #region inline expansion
        static public QuotedFunction Expand(QuotedFunction f, int nMaxDepth)
        {
            List<Function> ret = new List<Function>();
            Expand(ret, f, nMaxDepth);
            return new QuotedFunction(ret);
        }

        static void Expand(List<Function> list, Function f, int nMaxDepth)
        {
            if (f is Quotation)
            {
                Expand(list, f as Quotation, nMaxDepth);
            }
            else if (f is QuotedFunction)
            {
                Expand(list, f as QuotedFunction, nMaxDepth);
            }
            else if (f is DefinedFunction)
            {
                Expand(list, f as DefinedFunction, nMaxDepth);
            }
            else
            {
                list.Add(f);
            }
        }

        static void Expand(List<Function> fxns, Quotation q, int nMaxDepth)
        {
            Trace.Assert(nMaxDepth > 0);
            List<Function> tmp = new List<Function>();
            foreach (Function f in q.GetChildren())
                Expand(tmp, f, nMaxDepth - 1);
            fxns.Add(new Quotation(tmp));
        }

        static void Expand(List<Function> fxns, QuotedFunction q, int nMaxDepth)
        {
            foreach (Function f in q.GetChildren())
                Expand(fxns, f, nMaxDepth - 1);
        }

        static void Expand(List<Function> fxns, DefinedFunction d, int nMaxDepth)
        {
            foreach (Function f in d.GetChildren())
                Expand(fxns, f, nMaxDepth - 1);
        }
        #endregion

        #region apply macros
        static public QuotedFunction ApplyMacros(QuotedFunction f)
        {
            List<Function> list = new List<Function>(f.GetChildren().ToArray());
            Macros.GetGlobalMacros().ApplyMacros(list);
            return new QuotedFunction(list);
        }
        #endregion
    }
}
