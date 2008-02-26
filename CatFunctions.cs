/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
        CatMetaDataBlock mpMetaData;
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
        public string GetFxnTypeString()
        {
            if (GetFxnType() == null)
                return "untyped";
            else
                return GetFxnType().ToPrettyString();
        }
        public CatFxnType GetFxnType()
        {
            return mpFxnType;
        }
        public void SetMetaData(CatMetaDataBlock meta)
        {
            mpMetaData = meta;
            CatMetaData desc = meta.Find("desc");
            if (desc != null)
            {
                msDesc = desc.msContent;
            }
        }
        public CatMetaDataBlock GetMetaData()
        {
            return mpMetaData;
        }

        public bool HasMetaData()
        {
            return ((mpMetaData != null) && (mpMetaData.Count > 0));
        }
  
        public void WriteTo(StreamWriter sw)
        {
            sw.Write("define ");
            sw.Write(msName);
            if (mpFxnType != null)
            {
                sw.Write(" : ");
                sw.Write(mpFxnType);
            }
            sw.WriteLine();
            if (mpMetaData != null)
            {
                sw.WriteLine("{{");
                sw.WriteLine(mpMetaData.ToString());
                sw.WriteLine("}}");
            }
            sw.WriteLine("{");
            sw.Write("  ");
            sw.WriteLine(GetImplString());
            sw.WriteLine("}");
        }

        #region virtual functions
        // TODO: rename to Execute
        public abstract void Eval(Executor exec);
        public abstract string GetImplString();
        #endregion

        #region static functions
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
                s += CatKind.TypeToString(m.DeclaringType) + " ";

            foreach (ParameterInfo pi in m.GetParameters())
                s += CatKind.TypeToString(pi.ParameterType) + " ";

            s += "-> 'R";

            if (HasThisType(m))
                s += " this";

            if (HasReturnType(m))
                s += " " + CatKind.TypeToString(GetReturnType(m));

            s += ")";

            return s;
        }

        #endregion

        public virtual List<Function> GetSubFxns()
        {
            return null;
        }
    }

    /// <summary>
    /// Used to push stacks of values on a stack.
    /// Note: Not a PushValueBase subclass!
    /// This is used in the "pull" experimental function
    /// </summary>
    public class PushStack : Function 
    {
        Executor stk;
        
        public PushStack(Executor x)
        {
            stk = x;
        }

        public override void Eval(Executor exec)
        {
            foreach (object o in stk.GetStackAsArray())
                exec.Push(o);
        }

        public Executor GetStack()
        {
            return stk;
        }

        public override string GetImplString()
        {
            return "_stack_";
        }
    }

    abstract public class PushValueBase : Function
    {
    }

    public class PushValue<T> : PushValueBase
    {
        CatMetaValue<T> mValue;
        string msValueType;
        
        public PushValue(T x)
        {
            mValue = new CatMetaValue<T>(x);
            msName = mValue.GetData().ToString();
            msValueType = CatKind.TypeNameFromObject(x);
            mpFxnType = CatFxnType.Create("( -> " + msValueType + ")");
        }
        public T GetValue()
        {
            return mValue.GetData();
        }

        public override string GetImplString()
        {
            return mValue.ToString();
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

    public class PushInt : PushValue<int>
    {
        public PushInt(int x)
            : base(x)
        { }

        #region overrides
        public override void Eval(Executor exec)
        {
            exec.PushInt(GetValue());
        }
        #endregion
    }

    public class PushBool : PushValue<bool>
    {
        public PushBool(bool x)
            : base(x)
        { }

        #region overrides
        public override void Eval(Executor exec)
        {
            exec.PushBool(GetValue());
        }
        #endregion
    }

    /// <summary>
    /// Represents a a function literal. In other words a function that pushes an anonymous function onto a stack.
    /// </summary>
    public class PushFunction : Function
    {
        List<Function> mSubFxns;
        
        public PushFunction(List<Function> children)
        {
            mSubFxns = children.GetRange(0, children.Count);
            msDesc = "pushes an anonymous function onto the stack";
            msName = "[";
            for (int i = 0; i < mSubFxns.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mSubFxns[i].GetName();
            }
            msName += "]";

            if (Config.gbTypeChecking)
            {
                if (Config.gbVerboseInference)
                    Output.WriteLine("inferring type of quoted function " + msName);

                try
                {
                    // Quotations can be unclear?
                    CatFxnType childType = CatTypeReconstructor.Infer(mSubFxns);

                    // Honestly this should never be true.
                    if (childType == null)
                        throw new Exception("unknown type error");

                    mpFxnType = new CatQuotedType(childType);
                    mpFxnType = CatVarRenamer.RenameVars(mpFxnType);
                }
                catch (Exception e)
                {
                    Output.WriteLine("Could not type quotation: " + msName);
                    Output.WriteLine("Type error: " + e.Message);
                    mpFxnType = null;
                }
            }
            else
            {
                mpFxnType = null;
            }
        }

        public override void Eval(Executor exec)
        {
            exec.Push(new QuotedFunction(mSubFxns, CatFxnType.Unquote(mpFxnType)));
        }

        public List<Function> GetChildren()
        {
            return mSubFxns;
        }

        public override string GetImplString()
        {
            string ret = "";
            foreach (Function f in mSubFxns)
                ret += f.msName + " ";
            return ret;
        }
    }

    /// <summary>
    /// Represents a function that is on the stack.
    /// </summary>
    public class QuotedFunction : Function
    {
        List<Function> mSubFxns;
        
        public QuotedFunction(List<Function> children, CatFxnType pFxnType)
        {
            mSubFxns = new List<Function>(children.ToArray());
            msDesc = "anonymous function";
            msName = "";
            for (int i = 0; i < mSubFxns.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mSubFxns[i].GetName();
            }
            mpFxnType = new CatQuotedType(pFxnType);
        }

        public QuotedFunction(List<Function> children)
            : this(children, CatTypeReconstructor.Infer(children))
        {
        }

        public QuotedFunction()
        {
            mSubFxns = new List<Function>();
        }

        public CatFxnType GetUnquotedFxnType()
        {
            CatKind k = GetUnquotedKind();
            if (!(k is CatFxnType))
                throw new Exception("illegal type for a quoted function, should produce a single function : " + mpFxnType.ToString());
            return k as CatFxnType;
        }

        public CatKind GetUnquotedKind()
        {
            if (mpFxnType.GetCons().GetKinds().Count != 0)
                throw new Exception("illegal type for a quoted function, should have no consumption : " + mpFxnType.ToString());
            if (mpFxnType.GetProd().GetKinds().Count != 1)
                throw new Exception("illegal type for a quoted function, should have a single production : " + mpFxnType.ToString());
            CatKind k = mpFxnType.GetProd().GetKinds()[0];
            return k;
        }

        public QuotedFunction(QuotedFunction first, QuotedFunction second)
        {
            mSubFxns = new List<Function>(first.GetSubFxns().ToArray());
            mSubFxns.AddRange(second.GetSubFxns().ToArray());

            msDesc = "anonymous composed function";
            msName = "";
            for (int i = 0; i < mSubFxns.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mSubFxns[i].GetName();
            }

            try
            {
                mpFxnType = new CatQuotedType(
                    CatTypeReconstructor.ComposeTypes(first.GetUnquotedFxnType(), 
                        second.GetUnquotedFxnType()));
            }
            catch (Exception e)
            {
                Output.WriteLine("unable to type quotation: " + ToString());
                Output.WriteLine("type error: " + e.Message);
                mpFxnType = null;
            }
        }

        public override void Eval(Executor exec)
        {
            foreach (Function f in mSubFxns)
                f.Eval(exec);
        }

        public override string ToString()
        {
            string ret = "[";
            for (int i = 0; i < mSubFxns.Count; ++i)
            {
                if (i > 0) ret += " ";
                ret += mSubFxns[i].GetName();
            }
            ret += "]";
            return ret;
        }
    
        public override string GetImplString()
        {
            string ret = "[";
            foreach (Function f in mSubFxns)
                ret += f.msName + " ";
            return ret + "]";
        }

        public override List<Function> GetSubFxns()
        {
            return mSubFxns;
        }
    }

    /// <summary>
    /// Represents a function that is on the stack.
    /// </summary>
    public class SimpleQuotedFunction : QuotedFunction
    {
        Function mFxn;

        public SimpleQuotedFunction(Function f)
        {
            mFxn = f;
            if (mFxn.GetFxnType() != null)
                mpFxnType = new CatQuotedType(mFxn.GetFxnType());
            GetSubFxns().Add(f);
        }

        public override void Eval(Executor exec)
        {
            mFxn.Eval(exec);
        }
    }

    /// <summary>
    /// This class represents a dynamically created function, 
    /// e.g. the result of calling the quote function.
    /// </summary>
    public class QuotedValue : SimpleQuotedFunction
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
        List<Function> mFunctions = new List<Function>();
        bool mbExplicitType = false;
        bool mbTypeError = false;

        public DefinedFunction(string s)
        {
            msName = s;
        }

        public DefinedFunction(string s, List<Function> fxns)
        {
            msName = s;
            AddFunctions(fxns);
        }

        public void AddFunctions(List<Function> fxns)
        {
            mFunctions.AddRange(fxns);
            msDesc = "";

            if (Config.gbVerboseInference && Config.gbTypeChecking)
            {
                Output.WriteLine("");
                Output.WriteLine("inferring type of " + msName);
                Output.WriteLine("===");
            }

            try
            {
                mpFxnType = CatTypeReconstructor.Infer(mFunctions);
            }
            catch (Exception e)
            {
                Output.WriteLine("type error in function " + msName);
                Output.WriteLine(e.Message);
                mpFxnType = null;
            }
        }

        public override void Eval(Executor exec)
        {
            exec.Execute(mFunctions);
        }

        public override List<Function> GetSubFxns()
        {
            return mFunctions;
        }

        public override string GetImplString()
        {
            string ret = "";
            foreach (Function f in mFunctions)
                ret += f.msName + " ";
            return ret;
        }

        public bool IsTypeExplicit()
        {
            return mbExplicitType;
        }

        public bool HasTypeError()
        {
            return mbTypeError;
        }

        public void SetTypeExplicit()
        {
            mbExplicitType = true;
        }
        
        public void SetTypeError()
        {
            mbTypeError = true;
        }
    }

    public class Method : Function
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
            mpFxnType = CatVarRenamer.RenameVars(mpFxnType);
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

       public override string GetImplString()
       {
           return "primitive";
       }

        public int Count
        {
            get { return GetMethodInfo().GetParameters().Length; }
        }

        public Type GetType(int n)
        {
            return GetMethodInfo().GetParameters()[n].ParameterType;
        }
    }

    public abstract class PrimitiveFunction : Function
    {
        public PrimitiveFunction(string sName, string sType, string sDesc)
            : base(sName, sDesc)
        {
            mpFxnType = CatFxnType.Create(sType);
            mpFxnType = CatVarRenamer.RenameVars(mpFxnType);
        }

        public override string GetImplString()
        {
            return "primitive";
        }
    }

    public class SelfFunction : Function
    {
        Function mpFxn;

        public SelfFunction(Function f)
            : base("self")
        {
            mpFxnType = CatFxnType.Create("('A -> 'B)");
            mpFxn = f;
        }

        public override void Eval(Executor exec)
        {
            mpFxn.Eval(exec);
        }

        public override string GetImplString()
        {
            return "self";
        }
    }
}
