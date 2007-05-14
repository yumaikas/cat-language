using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    public class PartialEvaluator
    {
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
        public static List<Function> Eval(List<Function> fxns)
        {
            Executor exec = new Executor();
            return Eval(exec, fxns);
        }

        /// <summary>
        /// We attempt to execute an expression (list of functions) on an empty stack. 
        /// When no exception is raised we know that the subexpression can be replaced with anything 
        /// that generates the values. 
        /// </summary>
        static List<Function> Eval(Executor exec, List<Function> fxns)
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
                        List<Function> tmp = Eval(q.GetChildren());
                        f = new Quotation(tmp);
                        f.Eval(exec);
                    }
                    else if (f is DefinedFunction)
                    {
                        /*
                        DefinedFunction df = f as DefinedFunction;

                        // Notice, we continue using the current stack
                        List<Function> tmp = Eval(exec, df.GetChildren());

                        // Now we append the result to this one. 
                        ret.AddRange(tmp);
                         */

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
                }
                i++;
            }

            for (int j = values.Length - 1; j >= 0; --j)
                ret.Add(ValueToFunction(values[j]));
            
            return ret;
        }
    }
}
