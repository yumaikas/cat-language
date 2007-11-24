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
            //qf = ApplyMacros(qf);
            //qf = ApplyMacros(qf);
            //qf = Expand(qf);
            //qf = ApplyMacros(qf);
            //qf = PartialEval(qf);
            //qf = Expand(qf);
            //qf = ReplaceSimpleQuotations(qf);
            qf = ApplyMacros(qf);
            qf = ExpandInline(qf, 4);
            qf = ApplyMacros(qf);
            qf = EmbedConstants(qf);
            return qf;
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
            Executor exec = new DefaultExecutor();
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
                    List<Function> tmp = PartialEval(new DefaultExecutor(), q.GetChildren());
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
                    values = exec.GetStackAsArray();
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
                    exec.Clear();
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
        static public QuotedFunction ExpandInline(QuotedFunction f, int nMaxDepth)
        {
            List<Function> ret = new List<Function>();
            ExpandInline(ret, f, nMaxDepth);
            return new QuotedFunction(ret);
        }

        static void ExpandInline(List<Function> list, Function f, int nMaxDepth)
        {
            if (nMaxDepth == 0)
            {
                list.Add(f);
            }
            else if (f is Quotation)
            {
                ExpandInline(list, f as Quotation, nMaxDepth);
            }
            else if (f is QuotedFunction)
            {
                ExpandInline(list, f as QuotedFunction, nMaxDepth);
            }
            else if (f is DefinedFunction)
            {
                ExpandInline(list, f as DefinedFunction, nMaxDepth);
            }
            else
            {
                list.Add(f);
            }
        }

        static void ExpandInline(List<Function> fxns, Quotation q, int nMaxDepth)
        {
            List<Function> tmp = new List<Function>();
            foreach (Function f in q.GetChildren())
                ExpandInline(tmp, f, nMaxDepth - 1);
            fxns.Add(new Quotation(tmp));
        }

        static void ExpandInline(List<Function> fxns, QuotedFunction q, int nMaxDepth)
        {
            foreach (Function f in q.GetChildren())
                ExpandInline(fxns, f, nMaxDepth - 1);
        }

        static void ExpandInline(List<Function> fxns, DefinedFunction d, int nMaxDepth)
        {
            foreach (Function f in d.GetChildren())
                ExpandInline(fxns, f, nMaxDepth - 1);
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

        #region embedded constant primitive instructions 
        /// <summary>
        /// This is a bit of a hacked optimization. We replace functions such as "2 add_int" 
        /// with a new instruction that embeds the constant within it. 
        /// </summary>
        static public QuotedFunction EmbedConstants(QuotedFunction f)
        {
            List<Function> ret = new List<Function>(f.GetChildren());
            EmbedConstants(ret);
            return new QuotedFunction(ret);
        }
        static public void EmbedConstants(List<Function> fxns)
        {
            for (int i=0; i < fxns.Count - 1; ++i)
            {
                Function f = fxns[i];
                if (f is PushValue<int>)
                {
                    int n = (f as PushValue<int>).GetValue();
                    Function g = fxns[i + 1];
                    if (g is Primitives.AddInt)
                    {
                        fxns.RemoveAt(i + 1);
                        if (n == 1)
                        {
                            fxns[i] = new IncInt();
                        }
                        else
                        {
                            fxns[i] = new AddConstInt(n);
                        }
                    }
                    else if (g is Primitives.SubInt)
                    {
                        fxns.RemoveAt(i + 1);
                        if (n == 1)
                        {
                            fxns[i] = new DecInt();
                        }
                        else
                        {
                            fxns[i] = new SubConstInt(n);
                        }
                    }
                    else if (g is Primitives.LtInt)
                    {
                        fxns.RemoveAt(i + 1);
                        fxns[i] = new LtConstInt(n);
                    }
                }
                else if (f is Quotation)
                {
                    Quotation q = f as Quotation;
                    List<Function> tmp = new List<Function>(q.GetChildren());
                    EmbedConstants(tmp);
                    fxns[i] = new Quotation(tmp);
                }
            }
        }
       
        public class AddConstInt : OptimizedFunction  
        {
            int mData;
            public AddConstInt(int n) { mData = n; mpFxnType = CatFxnType.Create("(int -> int)");  }
            public override void Eval(Executor exec)
            { exec.AddInt(mData); }
        }
        public class SubConstInt : OptimizedFunction 
        {
            int mData;
            public SubConstInt(int n) { mData = n; mpFxnType = CatFxnType.Create("(int -> int)"); }
            public override void Eval(Executor exec)
            { exec.SubInt(mData); }
        }
        public class DecInt : OptimizedFunction
        {
            public DecInt() { mpFxnType = CatFxnType.Create("(int -> int)"); }
            public override void Eval(Executor exec)
            { exec.DecInt(); }
        }
        public class IncInt : OptimizedFunction
        {
            public IncInt() { mpFxnType = CatFxnType.Create("(int -> int)"); }
            public override void Eval(Executor exec)
            { exec.IncInt(); }
        }
        public class LtConstInt : OptimizedFunction
        {
            int mData;
            public LtConstInt(int n) { mData = n; mpFxnType = CatFxnType.Create("(int -> bool)"); }
            public override void Eval(Executor exec)
            { exec.LtInt(mData); }
        }
        #endregion
    }
}
