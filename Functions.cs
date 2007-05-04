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
        public Function(string sName, string sType, string sDesc)
        {
            msName = sName;
            msType = sType;
            msDesc = sDesc;
        }

        public Function(string sName, string sType)
        {
            msName = sName;
            msType = sType;
            msDesc = "";
        }

        #region Fields
        public string msName = "_unnamed_"; 
        public string msDesc = "";
        public string msType = "";
        public CatFxnType mpType = null;
        #endregion

        public Function()
        {
        }
        public void SetType(string s)
        {
            msType = s;
        }
        public CatFxnType GetCatType()
        {
            // compute as requested
            if (mpType == null)
                mpType = CatFxnType.CreateFxnType(msType);
            return mpType;
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
            return msType;
        }

        public abstract void Eval(Executor exec);

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
            string s = "(";

            if (HasThisType(m))
                s += "this=" + TypeToString(m.DeclaringType) + " ";

            foreach (ParameterInfo pi in m.GetParameters())
                s += TypeToString(pi.ParameterType) + " ";

            s += " -> ";

            if (HasThisType(m))
                s += "this ";

            if (HasReturnType(m))
                s += TypeToString(GetReturnType(m));

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
        public IntFunction(int x) 
        {
            msName = x.ToString();
            SetType("( -> int)");
            mnValue = x;
        }
        public override void Eval(Executor exec) 
        { 
            exec.Push(GetValue());
        }
        public override string ToString()
        {
            return msName;
        }
        public int GetValue()
        {
            return mnValue;
        }
    }

    /// <summary>
    /// This is a function that pushes an integer onto the stack.
    /// </summary>
    public class FloatFunction : Function
    {
        double mdValue;
        public FloatFunction(double x)
        {
            msName = x.ToString();
            SetType("( -> int)");
            mdValue = x;
        }
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        public double GetValue()
        {
            return mdValue;
        }
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class StringFunction : Function
    {
        string msValue;
        public StringFunction(string x) 
        {
            msName = "\"" + x + "\"";
            msValue = x;
            SetType("( -> string)");
        }
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        public string GetValue()
        {
            return msValue;
        }
    }

    /// <summary>
    /// This is a function that pushes a string onto the stack.
    /// </summary>
    public class CharFunction : Function
    {
        char mcValue;
        public CharFunction(char x)
        {
            msName = x.ToString();
            mcValue = x;
            SetType("( -> char)");
        }
        public override void Eval(Executor exec)
        {
            exec.Push(GetValue());
        }
        public char GetValue()
        {
            return mcValue;
        }
    }

    /// <summary>
    /// This class represents a dynamically created function, 
    /// e.g. the result of calling the quote function.
    /// </summary>
    public class QuoteValue : Function
    {
        Object mpValue;        
        
        public QuoteValue(Object x) 
        {
            mpValue = x;
            msName = mpValue.ToString();
        }
        public override void Eval(Executor exec)
        {
            exec.Push(mpValue);
        }
        public Object GetValue()
        {
            return mpValue;
        }
    }

    /// <summary>
    /// Represents a quotation (pushes an anonymous function onto a stack)
    /// </summary>
    public class Quotation : Function
    {
        List<Function> mChildren;
        
        public Quotation(List<Function> children)
        {
            mChildren = children;
            msDesc = "pushes an anonymous function onto the stack";
            msType = "( -> ('A -> 'B))";
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
    }

    public class QuotedFunction : Function
    {
        List<Function> mChildren;
        
        public QuotedFunction(List<Function> children)
        {
            mChildren = children;
            msDesc = "anonymous function";
            msType = "('A -> 'B)";
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
    /// This represents a function call. 
    /// 
    /// For now the only scope is global, but the apporach is that the function call 
    /// is bound to the scope where the call is declared, not where it is called. 
    /// This would matter only if implicit redefines are allowed in the semantics.
    /// </summary>
    public class FunctionName : Function
    {
        public FunctionName(string s)
            : base(s, "???", "")
        {
            msName = s;
        }

        public override void Eval(Executor exec)
        {
            Lookup(exec).Eval(exec);
        }

        public Function Lookup(Executor exec)
        {
            Scope scope = exec.GetGlobalScope();
            if (!scope.FunctionExists(msName))
                throw new Exception(msName + " is not defined");
            return scope.Lookup(exec.GetStack(), msName);
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
            msType = "untyped";
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
    }

    public class Method : Function, ITypeArray
    {
        MethodInfo mMethod;
        Object mObject;

        public Method(Object o, MethodInfo mi)
            : base(mi.Name, MethodToTypeString(mi))
        {
            mMethod = mi;
            mObject = o;
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

    public class MethodGroup : Function
    {
        List<Method> mOverloads = new List<Method>();

        public MethodGroup(Method m)
            : base(m.GetName(), m.GetTypeString())
        {
            mOverloads.Add(m);
        }

        public override void Eval(Executor exec)
        {
            Method m = mOverloads[0];
            for (int i = 1; i < mOverloads.Count; ++i)
            {
                m = GetBestMatch(exec.GetStack(), m, mOverloads[i]);
            }
            m.Eval(exec);
        }
        
        private bool CanCastTo(Type from, Type to)
        {
            if (from.Equals(typeof(int)))
                return to.Equals(typeof(double));

            if (from.Equals(typeof(byte)))
                return to.Equals(typeof(double)) || to.Equals(typeof(int));

            return false;
        }

        /// <summary>
        /// Used for computing which types are better matches. 
        /// </summary>
        private int AssignMatchScore(ITypeArray stk, ITypeArray sig)
        {
            int ret = 0;
            for (int i = 0; i < sig.Count; ++i)
            {
                Type t = stk.GetType(i);
                Type u = sig.GetType(i);
                if (u.Equals(t))
                {
                    ret += 100;
                }
                else if (u.IsAssignableFrom(t))
                {
                    ret += 10;
                }
                else if (CanCastTo(t, u))
                {
                    ret += 1;
                }
                else
                {
                    ret -= 10000;
                }
            }
            return ret;
        }

        private Method GetBestMatch(ITypeArray stk, Method x, Method y)
        {
            ITypeArray xt = x;
            ITypeArray yt = y;

            if (xt.Count != yt.Count)
                throw new Exception("mismatched number of parameters in overload " + y.ToString());

            if (stk.Count < xt.Count)
                throw new Exception("insufficient number of items on the stack");

            int xscore = AssignMatchScore(stk, xt);
            int yscore = AssignMatchScore(stk, yt);

            return xscore >= yscore ? x : y;
        }

        public void AddOverload(Method m)
        {
            if (m.GetName() != GetName())
                throw new Exception("overload must share the same name");
            if (mOverloads == null)
                mOverloads = new List<Method>();
            mOverloads.Add(m);
        }
    }
}
