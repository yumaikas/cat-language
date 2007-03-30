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
        #endregion

        public Function()
        {
        }
        public void SetType(string s)
        {
            msType = s;
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

        public abstract void Eval(CatStack stk);
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
        public override void Eval(CatStack stk) 
        { 
            stk.Push(GetValue());
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
        public override void Eval(CatStack stk)
        {
            stk.Push(GetValue());
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
        public override void Eval(CatStack stk)
        {
            stk.Push(GetValue());
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
        public override void Eval(CatStack stk)
        {
            stk.Push(GetValue());
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
        public override void Eval(CatStack stk)
        {
            stk.Push(mpValue);
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

        public override void Eval(CatStack stk)
        {
            stk.Push(new QuotedFunction(mChildren));
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

        public override void Eval(CatStack stk)
        {
            foreach (Function f in mChildren)
                f.Eval(stk);
        }

        public List<Function> GetChildren()
        {
            return mChildren;
        }
    }

    /// <summary>
    /// This class represents a dynamically created function, 
    /// e.g. the result of calling the quote function.
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
        public override void Eval(CatStack stk)
        {
            mFirst.Eval(stk);
            mSecond.Eval(stk);
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
        Scope mpScope;
        
        public FunctionName(string s, Scope scope)
            : base(s, "???", "")
        {
            msName = s;
            mpScope = scope;
        }

        private bool IsBetterMatchThan(Function f, Function g)
        {
            // Methods are always better matches.
            if (f is Method && !(g is Method)) return true;
            if (!(f is Method) && g is Method) return false;

            // A method with more parameters is always a better match
            Method fm = f as Method;
            Method gm = g as Method;

            return fm.GetSignature().IsBetterMatchThan(gm.GetSignature());
        }

        public override void Eval(CatStack stk)
        {
            if (!mpScope.FunctionExists(msName))
                throw new Exception(msName + " is not defined");
            List<Function> fs = mpScope.Lookup(stk, msName);
            if (fs.Count == 0)
                throw new Exception("unable to find " + msName + " with matching types");
            Function f = fs[0];
            for (int i=1; i < fs.Count; ++i)
            {
                if (IsBetterMatchThan(fs[i], f))
                    f = fs[i];
            }
            f.Eval(stk);
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
            msType = "???";
            msDesc = "";
            mTerms = terms;
        }

        public override void Eval(CatStack stk)
        {
            foreach (Function f in mTerms)
                f.Eval(stk);
        }

        public string GetTermsAsString()
        {
            string s = "";
            foreach (Function f in mTerms)
                s += f.GetName() + " ";
            return s;
        }
    }
}
