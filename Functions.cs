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
    /// The base class for all Cat functions 
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

        #region virtual functions
        public virtual CatFxnType GetFxnType()
        {
            return null;
        }
        public virtual void Expand(List<Function> fxns)
        {
            fxns.Add(this);
        }
        public abstract void Eval(Executor exec);
        #endregion

        #region invocation functions
        public virtual Object Invoke()
        {
            Eval(Executor.Aux);
            if (Executor.Aux.GetStack().Count != 1)
            {
                Executor.Aux.GetStack().Clear();
                throw new Exception("internal error: after invoking " + GetName() + " auxiliary stack should have exactly one value.");
            }
            return Executor.Aux.GetStack().Pop();
        }

        public virtual Object Invoke(Object o)
        {
            Executor.Aux.Push(o);
            return Invoke();
        }

        public virtual Object Invoke(Object o1, Object o2)
        {
            Executor.Aux.Push(o1);
            Executor.Aux.Push(o2);
            return Invoke();
        }

        public virtual Object Invoke(Object[] args)
        {
            foreach (Object arg in args)
                Executor.Aux.Push(arg);
            return Invoke();
        }
        #endregion

        #region conversion functions
        public MapFxn ToMapFxn()
        {
            return delegate(object x) { return Invoke(x); };
        }

        public FilterFxn ToFilterFxn()
        {
            return delegate(object x) { return (bool)Invoke(x); };
        }

        public FoldFxn ToFoldFxn()
        {
            return delegate(object x, object y) { return Invoke(x, y); };
        }

        public RangeGenFxn ToRangeGenFxn()
        {
            return delegate(int n) { return Invoke(n); };
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
  
    /// <summary>
    /// This is a function that pushes an integer onto the stack.
    /// </summary>
    public class IntFunction : Function
    {
        int mnValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R int)");
            
        public IntFunction(int x) 
        {
            msName = x.ToString();
            mnValue = x;
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
        public override CatFxnType GetFxnType()
        {
            return gpFxnType;
        }
        #endregion 
    }

    /// <summary>
    /// This is a function that pushes an integer onto the stack.
    /// </summary>
    public class FloatFunction : Function
    {
        double mdValue;
        static CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R double)");
        public FloatFunction(double x)
        {
            msName = x.ToString();
            mdValue = x;
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
        public override CatFxnType GetFxnType()
        {
            return gpFxnType;
        }
        #endregion
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class StringFunction : Function
    {
        string msValue;
        CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R string)");
        public StringFunction(string x) 
        {
            msName = "\"" + x + "\"";
            msValue = x;
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
        public override CatFxnType GetFxnType()
        {
            return gpFxnType;
        }
        #endregion
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class CharFunction : Function
    {
        char mcValue;
        CatFxnType gpFxnType = CatFxnType.Create("('R -> 'R char)");
        public CharFunction(char x)
        {
            msName = x.ToString();
            mcValue = x;
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
        public override CatFxnType GetFxnType()
        {
            return gpFxnType;
        }
        #endregion 
    }

    /// <summary>
    /// This class represents a dynamically created function, 
    /// e.g. the result of calling the quote function.
    /// </summary>
    public class QuoteValue : Function
    {
        Object mpValue;
        CatFxnType gpFxnType = CatFxnType.Create("('R 'a -> 'R ('S -> 'S 'a))");
        public QuoteValue(Object x) 
        {
            mpValue = x;
            msName = mpValue.ToString();
        }
        public Object GetValue()
        {
            return mpValue;
        }
        #region overrides
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        public override CatFxnType GetFxnType()
        {
            return gpFxnType;
        }
        #endregion
    }

    /// <summary>
    /// Represents a quotation (pushes an anonymous function onto a stack)
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
        }

        public override void Eval(Executor exec)
        {
            exec.Push(new QuotedFunction(mChildren));
        }

        public List<Function> GetChildren()
        {
            return mChildren;
        }

        public override void Expand(List<Function> fxns)
        {
            List<Function> list = new List<Function>();
            foreach (Function f in GetChildren())
                f.Expand(list);
            fxns.Add(new Quotation(list));
        }
    }

    public class QuotedFunction : Function
    {
        List<Function> mChildren;
        
        public QuotedFunction(List<Function> children)
        {
            mChildren = children.GetRange(0, children.Count);
            msDesc = "anonymous function";
            msName = "";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mChildren[i].GetName();
            }
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

        public override void Expand(List<Function> fxns)
        {
            foreach (Function f in GetChildren())
                f.Expand(fxns);
        }
    }

    /// <summary>
    /// This class represents a function, created by calling
    /// compose.
    /// </summary>
    public class ComposedFunction : Function
    {
        Function mFirst;
        Function mSecond;
        public ComposedFunction(Function first, Function second)
        {
            mFirst = first;
            mSecond = second;
            msName = mFirst.GetName() + " " + mSecond.GetName();
            msDesc = "composed function";
        }
        public override void Eval(Executor exec)
        {
            mFirst.Eval(exec);
            mSecond.Eval(exec);
        }
    }

    /// <summary>
    /// Represents a function defined by the user
    /// </summary>
    public class DefinedFunction : Function
    {
        List<Function> mTerms;

        public DefinedFunction(string s, List<Function> terms)
        {
            msName = s;
            // TODO: eventually get the type from the annotation.
            // we then have to either verify it or trust it.
            mTerms = terms;
            msDesc = "";
            foreach (Function f in mTerms)
                msDesc += f.GetName() + " ";
        }

        public override void Eval(Executor exec)
        {
            foreach (Function f in mTerms)
                f.Eval(exec);
        }

        public override void Expand(List<Function> fxns)
        {
            foreach (Function f in mTerms)
                fxns.Add(f);
        }
    }

    /// <summary>
    /// Todo: remove all of the dynamic dispatching code. 
    /// </summary>
    public class Method : Function, ITypeArray
    {
        MethodInfo mMethod;
        Object mObject;
        CatFxnType mFxnType;

        public Method(Object o, MethodInfo mi)
            : base(mi.Name, MethodToTypeString(mi))
        {
            mMethod = mi;
            mObject = o;
            string sType = MethodToTypeString(mi);
            mFxnType = CatFxnType.Create(sType);
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

        public override CatFxnType GetFxnType()
        {
            return mFxnType;
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
        CatFxnType mpType;

        public PrimitiveFunction(string sName, string sType, string sDesc)
            : base(sName, sDesc)
        {
            mpType = CatFxnType.Create(sType);
        }

        public override CatFxnType  GetFxnType()
        {
            return mpType;
        }
    }
}
