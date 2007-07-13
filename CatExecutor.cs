/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Cat
{
    /// <summary>
    /// Manages the main stack and exposes functions for manipulating them.
    /// </summary>
    public class Executor 
    {
        #region fields
        static public Executor Main = new Executor();
        private CatStack stack = new CatStack();
        public TextReader input = Console.In;
        public TextWriter output = Console.Out;
        Context mpScope;
        List<Function> fxns = new List<Function>();
        #endregion

        #region constructor
        public Executor()
        {
            mpScope = new Context();
        }

        public Executor(Executor exec)
        {
            mpScope = new Context(exec.GetGlobalScope());
        }

        public Executor(Context scope)
        {
            mpScope = scope;
        }
        #endregion

        #region stack functions
        public CatStack GetStack()
        {
            return stack;
        }
        public void Swap()
        {
            object tmp1 = Pop();
            object tmp2 = Pop();
            Push(tmp1);
            Push(tmp2);
        }
        public void Push(Object o)
        {
            stack.Push(o);
        }
        public void PushInt(int n)
        {
            stack.Push(n);
        }
        public void PushString(string s)
        {
            stack.Push(s);
        }
        public void PushRef(Function p)
        {
            stack.Push(p);
        }
        public Object Pop()
        {
            return stack.Pop();
        }
        public T TypedPop<T>()
        {
            if (stack.Count == 0)
                throw new Exception("Trying to pop an empty stack");
            Object o = stack.Pop();
            if (!(o is T))
                throw new Exception("Expected type " + typeof(T).Name + " but instead found " + o.GetType().Name);
            return (T)o;
        }
        public int PopInt()
        {
            return TypedPop<int>();
        }
        public bool PopBool()
        {
            return TypedPop<bool>();
        }
        public QuotedFunction PopFunction()
        {
            return TypedPop<QuotedFunction>();
        }
        public String PopString()
        {
            return TypedPop<String>();
        }
        public Object Peek()
        {
            return stack.Peek();
        }
        public T TypedPeek<T>()
        {
            if (stack.Count == 0)
                throw new Exception("Trying to peek into an empty stack ");
            Object o = stack.Peek();
            if (!(o is T))
                throw new Exception("Expected type " + typeof(T).Name + " but instead found " + o.GetType().Name);
            return (T)o;
        }
        public Function PeekProgram()
        {
            return TypedPeek<Function>();
        }
        public String PeekString()
        {
            return TypedPeek<String>();
        }
        public bool IsEmpty()
        {
            return (stack.Count == 0);
        }
        public int Count()
        {
            return (stack.Count);
        }
        public void Clear()
        {
            stack.Clear();
        }
        #endregion

        #region environment serialization
        public Context GetGlobalScope()
        {
            return mpScope;
        }
        public void Import()
        {
            LoadModule(PopString());
        }
        public void LoadModule(string s)
        {
            bool b = Config.gbVerboseInference;
            Config.gbVerboseInference = Config.gbVerboseInferenceOnLoad;
            try
            {
                Execute(Util.FileToString(s));
            }
            catch (Exception e)
            {
                MainClass.WriteLine("Failed to load \"" + s + "\" with message: " + e.Message);
            }
            Config.gbVerboseInference = b;
        }
        #endregion

        #region exection functions
        public void Execute(string s)
        {
            try
            {
                CatParser.Parse(s + "\n", this);
            }
            catch (Exception e)
            {
                MainClass.WriteLine("error: " + e.Message);
            }
        }
        public void ExecuteFunction(Function f)
        {
            f.Eval(this);
            fxns.Add(f);
        }
        #endregion

        #region utility functions
        public string StackToString(CatStack stk)
        {
            if (stk.Count == 0) return "_empty_";
            string s = "";
            int nMax = 5;
            if (stk.Count > nMax)
                s = "...";
            if (stk.Count < nMax)
                nMax = stk.Count;
            for (int i = nMax - 1; i >= 0; --i)
            {
                Object o = stk[i];
                s += Output.ObjectToString(o) + " ";
            }
            return s;
        }
        public void OutputStack()
        {
            MainClass.WriteLine("stack: " + StackToString(stack));
        }
        #endregion
    }
}
