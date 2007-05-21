using System;
using System.Collections.Generic;
using System.Text;

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
            qf = Expand(qf);
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
                return new PushInt((int)o);
            }
            else if (o is Double)
            {
                return new PushFloat((double)o);
            }
            else if (o is String)
            {
                return new PushString((string)o);
            }
            else if (o is Boolean)
            {
                bool b = (bool)o;
                if (b)
                    return new Primitives.True();
                else
                    return new Primitives.False();
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
            List<Function> ret = new List<Function>();
            object[] values = null;

            int i = 0;
            while (i < fxns.Count)
            {
                try
                {
                    Function f = fxns[i];
                    
                    if (f is Quotation)
                    {   
                        // We can partially evaluate quotations as well.
                        Quotation q = f as Quotation;
                        List<Function> tmp = PartialEval(new Executor(), q.GetChildren());
                        f = new Quotation(tmp);
                        f.Eval(exec);
                    }
                    else if (f is DefinedFunction)
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
                    // Copy all of the values from the previous good execution 
                    for (int j = values.Length - 1; j >= 0; --j)
                        ret.Add(ValueToFunction(values[j]));

                    ret.Add(fxns[i]);
                    exec.GetStack().Clear();
                    values = null;
                }
                i++;
            }

            if (values != null)
                for (int j = values.Length - 1; j >= 0; --j)
                    ret.Add(ValueToFunction(values[j]));
            
            return ret;
        }
        #endregion

        #region inline expansion
        static public QuotedFunction Expand(QuotedFunction f)
        {
            List<Function> ret = new List<Function>();
            Expand(ret, f);
            return new QuotedFunction(ret);
        }

        static void Expand(List<Function> list, Function f)
        {
            if (f is Quotation)
            {
                Expand(list, f as Quotation);
            }
            else if (f is QuotedFunction)
            {
                Expand(list, f as QuotedFunction);
            }
            else if (f is DefinedFunction)
            {
                Expand(list, f as DefinedFunction);
            }
            else
            {
                list.Add(f);
            }
        }

        static void Expand(List<Function> fxns, Quotation q)
        {
            List<Function> tmp = new List<Function>();
            foreach (Function f in q.GetChildren())
                Expand(tmp, f);
            fxns.Add(new Quotation(tmp));
        }

        static void Expand(List<Function> fxns, QuotedFunction q)
        {
            foreach (Function f in q.GetChildren())
                Expand(fxns, f);
        }

        static void Expand(List<Function> fxns, DefinedFunction d)
        {
            foreach (Function f in d.GetChildren())
                Expand(fxns, f);
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
