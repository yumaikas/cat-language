/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
            Trace.Assert(mpFxnType != null);
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
    }

    public class PushValue<T> : Function
    {
        CatMetaValue<T> mValue;
        
        public PushValue(T x)
        {
            mValue = new CatMetaValue<T>(x);
            msName = mValue.GetData().ToString(); 
            mpFxnType = CatFxnType.Create("( -> " + mValue.ToString() + ")");
        }
        public T GetValue()
        {
            return mValue.GetData();
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

            if (Config.gbVerboseInference)
                MainClass.WriteLine("inferring type of quoted function " + msName);

            try
            {
                // Quotations can be unclear? 
                CatFxnType childType = TypeInferer.Infer(mChildren, Config.gbVerboseInference, false);
                
                // Honestly this should never be true.
                if (childType == null)
                    throw new Exception("unknown type error");

                mpFxnType = new CatQuotedFxnType(childType);
                mpFxnType = VarRenamer.RenameVars(mpFxnType);
            }
            catch (Exception e)
            {
                MainClass.WriteLine("Could not type quotation: " + msName);
                MainClass.WriteLine("Type error: " + e.Message);
                mpFxnType = null;
            }
        }

        public override void Eval(Executor exec)
        {
            exec.Push(new QuotedFunction(mChildren, mpFxnType.Unquote()));
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
        
        public QuotedFunction(List<Function> children, CatFxnType pFxnType)
        {
            mChildren = new List<Function>(children.ToArray());
            msDesc = "anonymous function";
            msName = "";
            for (int i = 0; i < mChildren.Count; ++i)
            {
                if (i > 0) msName += " ";
                msName += mChildren[i].GetName();
            }
            mpFxnType = pFxnType;
        }

        public QuotedFunction(List<Function> children)
         : this(children, TypeInferer.Infer(children, Config.gbVerboseInference, true))
        {
        }

        public QuotedFunction(Function f)
        {
            mChildren = new List<Function>();
            mChildren.Add(f);
            mpFxnType = f.GetFxnType();
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

            try
            {
                // BUG PRI3: this occurs during "demo" calls. I suspect it is a symptom of errors in the recursion code.
                mpFxnType = TypeInferer.Infer(first.GetFxnType(), second.GetFxnType(), Config.gbVerboseInference, false);
                // mpFxnType = TypeInferer.Infer(first.GetFxnType(), second.GetFxnType(), Config.gbVerboseInference, true);
            }
            catch (Exception e)
            {
                MainClass.WriteLine("unable to type quotation: " + ToString());
                MainClass.WriteLine("type error: " + e.Message);
                mpFxnType = null;
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
            // Make sure the functions are okay
            foreach (Function f in terms)
            {
                // Detect self-references at the top level, 
                // this indicates an infinite loop
                if (f == this)
                    throw new Exception("a function can't call itself directly, this will result in an infinite loop");

                if (f.GetType() == null)
                    throw new Exception("passing an untyped term to function " + msName);
            }


            mTerms = terms;
            msDesc = "";
            foreach (Function f in mTerms)
                msDesc += f.GetName() + " ";

            if (Config.gbVerboseInference)
            {
                MainClass.WriteLine("");
                MainClass.WriteLine("inferring type");
                MainClass.Write(msName + " = { ");
                foreach (Function f in terms)
                   MainClass.Write(f.GetName() + " ");
                MainClass.WriteLine("}");
            }

            try
            {
                mpFxnType = TypeInferer.Infer(terms, Config.gbVerboseInference, true);
            }
            catch (Exception e)
            {
                MainClass.WriteLine("type error in function " + msName);
                MainClass.WriteLine(e.Message);
                mpFxnType = null;
            }
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
            mpFxnType = VarRenamer.RenameVars(mpFxnType);
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
            mpFxnType = VarRenamer.RenameVars(mpFxnType);
        }
    }

    public class SelfFunction : Function
    {
        Function mpFxn;

        public SelfFunction(Function f)
            : base(f.msName)
        {
            mpFxn = f;

            mpFxnType = new CatSelfType();
        }

        public override void Eval(Executor exec)
        {
            mpFxn.Eval(exec);
        }
    }

    public abstract class ObjectFieldFxn : Function
    {
        protected CatKind mpFieldType;
        protected CatClass mpClass;
        protected string msFieldName;

        public bool IsInitialized()
        {
            return mpFieldType != null && mpClass != null;
        }

        public abstract void ComputeType(CatFxnType ft);

        protected void CheckInitialized()
        {
            if (!IsInitialized())
                throw new Exception("field accessor is uninitialized");        
        }
    }

    public class GetFieldFxn : ObjectFieldFxn
    {
        public override void ComputeType(CatFxnType ft)
        {
            List<CatKind> prod = ft.GetProd().GetKinds();
            int n = prod.Count;
            if (n < 2) 
                throw new Exception("invalid usage of _get_, insufficient arguments");
            CatKind kName = prod[n - 1];
            CatKind kClass = prod[n - 2];
            CatMetaValue<string> metaName = kName as CatMetaValue<string>;
            if (metaName == null) 
                throw new Exception("invalid usage of _get_, missing field identifier");
            string sName = metaName.GetData();
            CatClass cClass = kClass as CatClass;
            if (cClass == null)
                throw new Exception("invalid usage of _get_, missing object");
            ComputeType(cClass, sName);
        }

        public void ComputeType(CatClass c, string sName)
        {
            mpClass = c;
            msFieldName = sName;
            if (!mpClass.HasField(msFieldName))
                throw new Exception("object does not have field " + msFieldName);
            mpFieldType = mpClass.GetFieldType(msFieldName);
            mpFxnType = CatFxnType.Create("(" + mpClass.ToString() + " string -> " + mpClass.ToString() + " " + mpFieldType.ToString() + ")");
        }

        public override void Eval(Executor exec)
        {
            if (!IsInitialized())
            {
                // Compute the type now
                Object o1 = exec.GetStack()[0];
                Object o2 = exec.GetStack()[1];
                ComputeType((o2 as CatObject).GetClass(), (string)o1);
            }
            string s = exec.TypedPop<string>();
            if (!s.Equals(msFieldName))
                throw new Exception("internal error: incorrect field name");
            CatObject o = exec.TypedPeek<CatObject>();
            exec.Push(o.GetField(s));
        }
    }

    public class SetFieldFxn : ObjectFieldFxn
    {
        public override void ComputeType(CatFxnType ft)
        {
            List<CatKind> prod = ft.GetProd().GetKinds();
            int n = prod.Count;
            if (n < 3)
                throw new Exception("invalid usage of _set_, insufficient arguments");
            CatKind kName = prod[n - 1];
            CatKind kValue = prod[n - 2];
            CatKind kClass = prod[n - 3];
            CatMetaValue<string> metaName = kName as CatMetaValue<string>;
            if (metaName == null)
                throw new Exception("invalid usage of _set_, missing field identifier");
            string sName = metaName.GetData();
            CatClass cClass = kClass as CatClass;
            if (cClass == null)
                throw new Exception("invalid usage of _set_, missing object");
            ComputeType(cClass, kValue, sName);
        }

        public void ComputeType(CatClass c, CatKind val, string sName)
        {
            mpClass = c;
            msFieldName = sName;
            if (!mpClass.HasField(msFieldName))
            {
                throw new Exception("the field " + msFieldName + " has not been defined");
            }
            else
            {
                CatKind pFieldType = mpClass.GetFieldType(msFieldName);
                if (!pFieldType.IsSubtypeOf(mpFieldType))
                    throw new Exception("invalid type, expected " + mpFieldType.ToString() + " but recieved " + pFieldType.ToString());
            }
            mpFxnType = CatFxnType.Create("(" + mpClass.ToString() + " 'a string -> " + mpClass.ToString() + ")");
        }

        public override void Eval(Executor exec)
        {
            if (!IsInitialized())
            {
                // Compute the type now 
                Object o1 = exec.GetStack()[0];
                Object o2 = exec.GetStack()[1];
                Object o3 = exec.GetStack()[2];
                ComputeType((o3 as CatObject).GetClass(), CatKind.GetKindFromObject(o2), (string)o1);
            }

            string s = exec.TypedPop<string>();
            if (!s.Equals(msFieldName))
                throw new Exception("internal error: incorrect field name");

            // TODO: dynamically check that arg is of the right type.
            Object arg = exec.Pop();
            CatObject o = exec.TypedPeek<CatObject>();
            o.SetField(s, o, mpClass);
        }
    }

    public class DefFieldFxn : ObjectFieldFxn
    {
        /// <summary>
        /// This implementation is almost precisely the same as SetFieldFxn
        /// </summary>
        public override void ComputeType(CatFxnType ft)
        {
            List<CatKind> prod = ft.GetProd().GetKinds();
            int n = prod.Count;
            if (n < 3)
                throw new Exception("invalid usage of _def_, insufficient arguments");
            CatKind kName = prod[n - 1];
            CatKind kValue = prod[n - 2];
            CatKind kClass = prod[n - 3];
            CatMetaValue<string> metaName = kName as CatMetaValue<string>;
            if (metaName == null)
                throw new Exception("invalid usage of _def_, missing field identifier");
            string sName = metaName.GetData();
            CatClass cClass = kClass as CatClass;
            if (cClass == null)
                throw new Exception("invalid usage of _def_, missing object");
            ComputeType(cClass, kValue, sName);
        }

        public void ComputeType(CatClass c, CatKind val, string sName)
        {
            msFieldName = sName;
            if (!c.HasField(msFieldName))
            {
                mpClass = c.AddFieldType(msFieldName, val);
                mpFieldType = val;
            }
            else
            {
                throw new Exception("the field " + msFieldName + " has already been defined");
            }
            mpFxnType = CatFxnType.Create("(" + c.ToString() + " 'a string -> " + mpClass.ToString() + ")");
        }

        public override void Eval(Executor exec)
        {
            if (!IsInitialized())
            {
                // Compute the type now
                Object o1 = exec.GetStack()[0];
                Object o2 = exec.GetStack()[1];
                Object o3 = exec.GetStack()[2];
                ComputeType((o3 as CatObject).GetClass(), CatKind.GetKindFromObject(o2), (string)o1);
            }

            string s = exec.TypedPop<string>();
            if (!s.Equals(msFieldName))
                throw new Exception("internal error: incorrect field name");
            
            // TODO: dynamically check that arg is of the right type.
            Object arg = exec.Pop();
            CatObject o = exec.TypedPeek<CatObject>();
            o.SetField(s, o, mpClass);
        }
    }
}
