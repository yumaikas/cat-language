/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

// Comment this line to enable all safety checks
#define FAST

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
    public class CatDefaultStack 
    {
        const int MAX_STACK = 100;
        int mCount = 0;
        Object[] mBase = new Object[MAX_STACK];

        public bool IsEmpty()
        {
            return mCount == 0;
        }
        public void Clear()
        {
            mCount = 0;
        }
        public void ClearTo(int n)
        {
            Trace.Assert(n >= mCount);
            mCount = n;
        }
        public int Count
        {
            get
            {
                return mCount;
            }
        }
        public Object Peek()
        {
            #if (!FAST)
            if (mCount == 0)
                throw new Exception("stack indexing error");
            #endif 
            return mBase[mCount - 1];
        }

        public void Push(Object x)
        {
            #if (!FAST)
            if (mCount > MAX_STACK)
                throw new Exception("stack indexing error");
            #endif 
            mBase[mCount++] = x;
        }
        public Object Pop()
        {
            #if (!FAST)
            if (mCount == 0)
                throw new Exception("stack indexing error");
            #endif 
            return mBase[--mCount];
        }
        public Object this[int index]
        {
            get
            {
                #if (!FAST) 
                if (index >= mCount) 
                    throw new Exception("stack indexing error");
                #endif
                return mBase[mCount - 1 - index];
            }
            set
            {
                #if (!FAST) 
                if (index >= mCount) 
                    throw new Exception("stack indexing error");
                #endif
                mBase[mCount - 1 - index] = value;
            }
        }

        public Object[] ToArray()
        {
            Object[] ret = new Object[mCount];
            mBase.CopyTo(ret, mCount);
            return ret;
        }

        public FList ToList()
        {
            return new FArray<object>(ToArray());
        }

        public void Dup()
        {
            if (mBase[mCount - 1] is FMutableList)
            {
                mBase[mCount] = (mBase[mCount - 1] as FMutableList).Clone();
            }
            else
            {
                mBase[mCount] = mBase[mCount - 1];
            }
            ++mCount;
        }

        public void Swap()
        {
            Object x = mBase[mCount - 2];
            mBase[mCount - 2] = mBase[mCount - 1];
            mBase[mCount - 1] = x;
        }
    }

    public class CatIntStack  
    {
        const int MAX_STACK = 100;
        int mCount = 0;
        int[] mBase = new int[MAX_STACK];

        public bool IsEmpty()
        {
            return mCount == 0;
        }
        public int Count
        {
            get
            {
                return mCount;
            }
        }
        public void Clear()
        {
            mCount = 0;
        }
        public void ClearTo(int n)
        {
            Trace.Assert(n >= mCount);
            mCount = n;
        }

        public int Peek()
        {
            #if (!FAST)
            if (mCount == 0)
                throw new Exception("stack indexing error");
            #endif 
            return mBase[mCount - 1];
        }

        public void Push(int x)
        {
            #if (!FAST)
            if (mCount > MAX_STACK)
                throw new Exception("stack indexing error");
            #endif 
            mBase[mCount++] = x;
        }
        public int Pop()
        {
            #if (!FAST)
            if (mCount == 0)
                throw new Exception("stack indexing error");
            #endif 
            return mBase[--mCount];
        }
        public int this[int index]
        {
            get
            {
                #if (!FAST) 
                if (index >= mCount) 
                    throw new Exception("stack indexing error");
                #endif
                return mBase[mCount - 1 - index];
            }
            set
            {
                #if (!FAST) 
                if (index >= mCount) 
                    throw new Exception("stack indexing error");
                #endif
                mBase[mCount - 1 - index] = value;
            }
        }

        public Object[] ToArray()
        {
            Object[] ret = new Object[mCount];
            mBase.CopyTo(ret, mCount);
            return ret;
        }

        public FList ToList()
        {
            return new FArray<object>(ToArray());
        }

       public void Dup()
        {
            mBase[mCount] = mBase[mCount - 1];
            ++mCount;
        }

        public void Swap()
        {
            int x = mBase[mCount - 2];
            mBase[mCount - 2] = mBase[mCount - 1];
            mBase[mCount - 1] = x;
        }

        public void AddInt(int x)
        {
            mBase[mCount - 1] += x;
        }

        public void SubInt(int x)
        {
            mBase[mCount - 1] -= x;
        }

        public void LtInt(int x)
        {
            mBase[mCount - 1] = mBase[mCount - 1] < x ? 1 : 0;
        }

        public void DecInt()
        {
            --(mBase[mCount - 1]);
        }

        public void IncInt()
        {
            ++(mBase[mCount - 1]);
        }
    }

    public abstract class Executor 
    {
        #region fields
        static public Executor Main = new DefaultExecutor();
        public TextReader input = Console.In;
        public TextWriter output = Console.Out;
        Context mpScope;
        #endregion

        #region constructor
        protected Executor(Context scope)
        {
            mpScope = scope;
        }
        #endregion

        #region abstract functions
        public abstract Object Peek();
        public abstract void Push(Object o);
        public abstract Object Pop();
        public abstract Object PeekBelow(int n);
        public abstract int GetStackSize();
        public abstract FList GetStackAsList();
        public abstract Object[] GetStackAsArray();
        public abstract void ClearTo(int n);
        public abstract void Dup();
        public abstract void Swap();
        public abstract bool IsEmpty();
        public abstract int Count();
        public abstract void Clear();
        public abstract string StackToString();
        #endregion

        #region typed stack access functions
        public T TypedPop<T>()
        {
            T result = TypedPeek<T>();
            Pop();
            return result;
        }
        public T TypedPeek<T>()
        {
#if (!FAST)
            if (stack.Count == 0) throw new Exception("Trying to peek into an empty stack ");
#endif

            Object o = Peek();

#if (!FAST)
            if (!(o is T))
                throw new Exception("Expected type " + typeof(T).Name + " but instead found " + o.GetType().Name);
#endif

            return (T)o;
        }
        public virtual void PushInt(int n)
        {
            Push(n);
        }
        public virtual void PushBool(bool x)
        { 
            Push(x); 
        }
        public virtual void PushString(string x)
        {
            Push(x);
        }
        public virtual void PushFxn(Function x)
        {
            Push(x);
        }
        public virtual int PopInt()
        {
            return TypedPop<int>();
        }
        public virtual bool PopBool()
        {
            return TypedPop<bool>();
        }
        public virtual QuotedFunction PopFunction()
        {
            return TypedPop<QuotedFunction>();
        }
        public virtual String PopString()
        {
            return TypedPop<String>();
        }
        public virtual Function PeekProgram()
        {
            return TypedPeek<Function>();
        }
        public virtual String PeekString()
        {
            return TypedPeek<String>();
        }
        public virtual int PeekInt()
        {
            return TypedPeek<int>();
        }
        public virtual bool PeekBool()
        {
            return TypedPeek<bool>();
        }
        public virtual void AddInt(int n)
        {
            PushInt(PopInt() + n);
        }
        public virtual void SubInt(int n)
        {
            PushInt(PopInt() + n);
        }
        public virtual void IncInt()
        {
            PushInt(PopInt() + 1);
        }
        public virtual void DecInt()
        {
            PushInt(PopInt() - 1);
        }
        public virtual void LtInt(int n)
        {
            PushBool(PopInt() < n);
        }
        #endregion

        #region environment serialization
        public Context GetGlobalContext()
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
                Output.WriteLine("Failed to load \"" + s + "\" with message: " + e.Message);
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
                Output.WriteLine("error: " + e.Message);
            }
        }
        #endregion

        #region utility functions
        public void OutputStack()
        {
            Output.WriteLine("stack: " + StackToString());
        }
        #endregion
    }

    public class DefaultExecutor : Executor
    {
        CatDefaultStack mStack = new CatDefaultStack();

        public DefaultExecutor()
            : base(new Context())
        {
        }

        public DefaultExecutor(Executor exec)
            : base(new Context(exec.GetGlobalContext()))
        {
        }

        public DefaultExecutor(Context scope)
            : base(scope)
        {
        }

        public CatDefaultStack GetStack()
        {
            return mStack;
        }

        public override Object Peek()
        {
            return mStack.Peek();
        }

        public override Object PeekBelow(int n)
        {
            return mStack[n];
        }
        public override void Push(Object o)
        {
            mStack.Push(o);
        }
        public override Object Pop()
        {
            return mStack.Pop();
        }
        public override int GetStackSize()
        {
            return GetStack().Count;
        }
        public override FList GetStackAsList()
        {
            return GetStack().ToList();
        }
        public override Object[] GetStackAsArray()
        {
            return GetStack().ToArray();
        }
        public override void ClearTo(int n)
        {
            GetStack().ClearTo(n);
        }
        public override void Dup()
        {
            GetStack().Dup();
        }
        public override void Swap()
        {
            GetStack().Swap();
        }
        public override bool IsEmpty()
        {
            return (GetStack().Count == 0);
        }
        public override int Count()
        {
            return (GetStack().Count);
        }
        public override void Clear()
        {
            GetStack().Clear();
        }
        public override string StackToString()
        {
            if (GetStack().Count == 0) return "_empty_";
            string s = "";
            int nMax = 5;
            if (GetStack().Count > nMax)
                s = "...";
            if (GetStack().Count < nMax)
                nMax = GetStack().Count;
            for (int i = nMax - 1; i >= 0; --i)
            {
                Object o = PeekBelow(i);
                s += Output.ObjectToString(o) + " ";
            }
            return s;
        }
    }

    public class IntExecutor : Executor
    {
        CatIntStack mStack = new CatIntStack();

        public IntExecutor()
            : base(new Context())
        {
        }

        public CatIntStack GetStack()
        {
            return mStack;
        }

        public override Object Peek()
        {
            return mStack.Peek();
        }

        public override Object PeekBelow(int n)
        {
            return mStack[n];
        }

        public override void Push(Object o)
        {
            if (o is Boolean)
            {
                mStack.Push((bool)o ? 1 : 0);
            }
            else
            {
                mStack.Push((int)o);
            }
        }

        public override Object Pop()
        {
            return mStack.Pop();
        }

        public override int PopInt()
        {
            return mStack.Pop();
        }

        public override bool PopBool()
        {
            return mStack.Pop() != 0;
        }

        public override int PeekInt()
        {
            return mStack.Peek();
        }

        public override bool PeekBool()
        {
            return mStack.Peek() != 0;
        }

        public override void PushInt(int x)
        {
            mStack.Push(x);
        }

        public override void PushBool(bool x)
        {
            mStack.Push(x ? 1 : 0);
        }

        public override void AddInt(int n)
        {
            mStack.AddInt(n);
        }
        public override void SubInt(int n)
        {
            mStack.SubInt(n);
        }
        public override void LtInt(int n)
        {
            mStack.LtInt(n);
        }
        public override void DecInt()
        {
            mStack.DecInt();
        }
        public override void IncInt()
        {
            mStack.IncInt();
        }
        public override int GetStackSize()
        {
            return mStack.Count;
        }
        public override FList GetStackAsList()
        {
            return mStack.ToList();
        }
        public override Object[] GetStackAsArray()
        {
            return mStack.ToArray();
        }
        public override void ClearTo(int n)
        {
            mStack.ClearTo(n);
        }
        public override void Dup()
        {
            mStack.Dup();
        }
        public override void Swap()
        {
            mStack.Swap();
        }
        public override bool IsEmpty()
        {
            return (mStack.Count == 0);
        }
        public override int Count()
        {
            return (mStack.Count);
        }
        public override void Clear()
        {
            mStack.Clear();
        }
        public override string StackToString()
        {
            if (mStack.Count == 0) return "_empty_";
            string s = "";
            int nMax = 5;
            if (mStack.Count > nMax)
                s = "...";
            if (mStack.Count < nMax)
                nMax = mStack.Count;
            for (int i = nMax - 1; i >= 0; --i)
            {
                Object o = PeekBelow(i);
                s += Output.ObjectToString(o) + " ";
            }
            return s;
        }
    }
}
