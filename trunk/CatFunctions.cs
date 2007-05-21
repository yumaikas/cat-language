/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

namespace Cat
{
    /// <summary>
    /// The base class for all Cat functions. All functions can be invoked like one would 
    /// invoke a MethodInfo object. This is because each one contains its own private 
    /// executor;
    /// </summary>
    public abstract class Function : CatBase
    {
        public Function(string sName, string sDesc)
        {
            msName = sName;
            msDesc = sDesc;
        }

        public Function(string sName)
        {
            msName = sName;
            msDesc = "";
        }

        #region Fields
        public string msName = "_unnamed_"; 
        public string msDesc = "";
        public CatFxnType mpFxnType;
        private Executor mExec;
        #endregion

        public Function()
        {
        }
        public string GetDesc()
        {
            return msDesc;
        }
        public string GetName()
        {
            return msName;
        }
        public override string ToString()
        {
            return "[" + msName + "]";
        }
        public string GetTypeString()
        {
            if (GetFxnType() == null)
                return "untyped";
            else
                return GetFxnType().ToString();
        }
        public CatFxnType GetFxnType()
        {
            return mpFxnType;
        }
        public Executor GetExecutor()
        {
            if (mExec == null)
                mExec = new Executor();
            return mExec;
        }

        #region virtual functions
        public abstract void Eval(Executor exec);
        #endregion

        #region invocation functions        
        public virtual Object Invoke()
        {
            Eval(GetExecutor());
            if (GetExecutor().Count() != 1)
            {
                GetExecutor().Clear();
                throw new Exception("internal error: after invoking " + GetName() + " auxiliary stack should have exactly one value.");
            }
            return GetExecutor().Pop();
        }

        public virtual Object Invoke(Object o)
        {
            GetExecutor().Push(o);
            return Invoke();
        }

        public virtual Object Invoke(Object o1, Object o2)
        {
            GetExecutor().Push(o1);
            GetExecutor().Push(o2);
            return Invoke();
        }

        public virtual Object Invoke(Object[] args)
        {
            foreach (Object arg in args)
                GetExecutor().Push(arg);
            return Invoke();
        }
        #endregion

        #region conversion functions
        public MapFxn ToMapFxn()
        {
            return delegate(object x) 
            { 
                return Invoke(x); 
            };
        }

        public FilterFxn ToFilterFxn()
        {
            return delegate(object x) 
            { 
                return (bool)Invoke(x); 
            };
        }

        public FoldFxn ToFoldFxn()
        {
            return delegate(object x, object y)
            {
                return Invoke(x, y);
            };
        }

        public RangeGenFxn ToRangeGenFxn()
        {
            return delegate(int n) 
            { 
                return Invoke(n); 
            };
        }
        #endregion

        #region static functions
        public static string TypeToString(Type t)
        {
            switch (t.Name)
            {
                case ("HashList"): return "hash_list";
                case ("Int32"): return "int";
                case ("Double"): return "float";
                case ("CatList"): return "list";
                case ("Object"): return "var";
                case ("Function"): return "function";
                case ("Boolean"): return "bool";
                case ("String"): return "string";
                case ("Char"): return "char";
                default: return t.Name;
            }
        }

        public static Type GetReturnType(MethodBase m)
        {
            if (m is ConstructorInfo)
                return (m as ConstructorInfo).DeclaringType;
            if (!(m is MethodInfo))
                throw new Exception("Expected ConstructorInfo or MethodInfo");
            return (m as MethodInfo).ReturnType;
        }

        public static bool HasReturnType(MethodBase m)
        {
            Type t = GetReturnType(m);
            return (t != null) && (!t.Equals(typeof(void)));
        }

        public static bool HasThisType(MethodBase m)
        {
            if (m is ConstructorInfo)
                return false;
            return !m.IsStatic;
        }

        public static Type GetThisType(MethodBase m)
        {
            if (m is ConstructorInfo)
                return null;
            if (!(m is MethodInfo))
                throw new Exception("Expected ConstructorInfo or MethodInfo");
            if (m.IsStatic)
                return null;
            return (m as MethodInfo).DeclaringType;
        }

        public static string MethodToTypeString(MethodBase m)
        {
            string s = "('R ";

            if (HasThisType(m))
                s += "this=" + TypeToString(m.DeclaringType) + " ";

            foreach (ParameterInfo pi in m.GetParameters())
                s += TypeToString(pi.ParameterType) + " ";

            s += "-> 'R";

            if (HasThisType(m))
                s += " this";

            if (HasReturnType(m))
                s += " " + TypeToString(GetReturnType(m));

            s += ")";

            return s;
        }

        #endregion
    }

    public class PushValue<T> : Function
    {
        T mValue;
        public PushValue(T x)
        {
            mValue = x;
            msName = x.ToString();
        }
        public T GetValue()
        {
            return mValue;
        }
        #region overrides
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        public override string ToString()
        {
            return "[" + msName + "]";
        }
        #endregion
    }

    /// <summary>
    /// This is a function that pushes an integer onto the stack.
    /// </summary>
    public class PushInt : Function
    {
        int mnValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R int)");
            
        public PushInt(int x) 
        {
            msName = x.ToString();
            mnValue = x;
            mpFxnType = gpFxnType;
        }
        public int GetValue()
        {
            return mnValue;
        }
        #region overrides
        public override void Eval(Executor exec) 
        { 
            exec.Push(GetValue());
        }
        #endregion 
    }

    /// <summary>
    /// This is a function that pushes an integer onto the stack.
    /// </summary>
    public class PushFloat : Function
    {
        double mdValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R double)");
        public PushFloat(double x)
        {
            msName = x.ToString();
            mdValue = x;
            mpFxnType = gpFxnType;
        }
        public double GetValue()
        {
            return mdValue;
        }
        #region overrides
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        #endregion
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class PushString : Function
    {
        string msValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R string)");
        public PushString(string x) 
        {
            msName = "\"" + x + "\"";
            msValue = x;
            mpFxnType = gpFxnType;
        }
        public string GetValue()
        {
            return msValue;
        }
        #region overrides
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        #endregion
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class PushChar : Function
    {
        char mcValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R char)");
        public PushChar(char x)
        {
            msName = x.ToString();
            mcValue = x;
            mpFxnType = gpFxnType;
        }
        public char GetValue()
        {
            return mcValue;
        }
        #region overrides
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        #endregion 
    }

    /// <summary>
    /// Represents a a function literal. In other words a function that pushes an anonymous function onto a stack.
    /// </summary>
    public class Quotation : Function
    {
        List<Function> mChildren;
        
        public Quotation(List<Function> children)
        {
            mChildren = children.GetRange(0, children.Count);
            msDesc = "pushes an anonymous function onto the stack";
            msName = "[";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mChildren[i].GetName();
            }
            msName += "]";

            CatFxnType childType = TypeInferer.Infer(mChildren, true);
            if (childType != null)
                mpFxnType = new CatQuotedFxnType(childType);
            else
                mpFxnType = CatFxnType.Create("('R -> 'R ('A -> 'B))");
        }

        public override void Eval(Executor exec)
        {
            exec.Push(new QuotedFunction(mChildren));
        }

        public List<Function> GetChildren()
        {
            return mChildren;
        }
    }

    /// <summary>
    /// Represents a function that is on the stack.
    /// </summary>
    public class QuotedFunction : Function
    {
        List<Function> mChildren;
        
        public QuotedFunction(List<Function> children)
        {
            mChildren = new List<Function>(children.ToArray());
            msDesc = "anonymous function";
            msName = "";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mChildren[i].GetName();
            }

            mpFxnType = TypeInferer.Infer(mChildren, true);
        }

        public QuotedFunction(Function f)
        {
            mChildren = new List<Function>();
            mChildren.Add(f);
        }

        public QuotedFunction(QuotedFunction first, QuotedFunction second)
        {
            mChildren = new List<Function>(first.GetChildren().ToArray());
            mChildren.AddRange(second.GetChildren().ToArray());

            msDesc = "anonymous composed function";
            msName = "";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mChildren[i].GetName();
            }

            mpFxnType = TypeInferer.Infer(first.GetFxnType(), second.GetFxnType(), true); ;
        }

        public override void Eval(Executor exec)
        {
            foreach (Function f in mChildren)
                f.Eval(exec);
        }

        public List<Function> GetChildren()
        {
            return mChildren;
        }

        public override string ToString()
        {
            string ret = "[";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) ret += " ";
                ret += mChildren[i].GetName();
            }
            ret += "]";
            return ret;
        }
    }

    /// <summary>
    /// This class represents a dynamically created function, 
    /// e.g. the result of calling the quote function.
    /// </summary>
    public class QuotedValue : QuotedFunction
    {
        public QuotedValue(Object x)
            : base(new PushValue<Object>(x))
        {
            msName = x.ToString();
        }

        public override string ToString()
        {
            return "[" + msName + "]";
        }
    }

    /// <summary>
    /// Represents a function defined by the user
    /// </summary>
    public class DefinedFunction : Function
    {
        List<Function> mTerms;

        public DefinedFunction(string s)
        {
            msName = s;
        }

        public void AddFunctions(List<Function> terms)
        {
            mTerms = terms;
            msDesc = "";
            foreach (Function f in mTerms)
                msDesc += f.GetName() + " ";

            mpFxnType = TypeInferer.Infer(terms, true);
        }

        public override void Eval(Executor exec)
        {
            foreach (Function f in mTerms)
                f.Eval(exec);
        }

        public List<Function> GetChildren()
        {
            return mTerms;
        }
    }

    /// <summary>
    /// Todo: remove all of the dynamic dispatching code. 
    /// </summary>
    public class Method : Function, ITypeArray
    {
        MethodInfo mMethod;
        Object mObject;

        public Method(Object o, MethodInfo mi)
            : base(mi.Name, MethodToTypeString(mi))
        {
            mMethod = mi;
            mObject = o;
            string sType = MethodToTypeString(mi);
            mpFxnType = CatFxnType.Create(sType);
        }

        public override void Eval(Executor exec)
        {
            int n = mMethod.GetParameters().Length;
            Object[] a = new Object[n];
            for (int i = 0; i < n; ++i)
            {
                Object o = exec.Pop();
                a[n - i - 1] = o;
            }
            Object ret = mMethod.Invoke(mObject, a);
            if (!mMethod.ReturnType.Equals(typeof(void)))
                exec.Push(ret);
        }

        public Object GetObject()
        {
            return mObject;
        }

        public MethodInfo GetMethodInfo()
        {
            return mMethod;
        }

        #region ITypeArray Members

        public int Count
        {
            get { return GetMethodInfo().GetParameters().Length; }
        }

        public Type GetType(int n)
        {
            return GetMethodInfo().GetParameters()[n].ParameterType;
        }

        #endregion
    }

    public abstract class PrimitiveFunction : Function
    {
        public PrimitiveFunction(string sName, string sType, string sDesc)
            : base(sName, sDesc)
        {
            mpFxnType = CatFxnType.Create(sType);
        }
    }
}
