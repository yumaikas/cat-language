/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Cat
{
    /// <summary>
    /// This wraps a method from a specifc .NET class and allows it to be invoked from Cat.
    /// </summary>
    public class Method : Function
    {
        /// <summary>
        /// A method signature is used to identify a method and to see if it can be called. 
        /// </summary>
        public class MethodSignature
        {
            List<Type> mTypes = new List<Type>();

            public MethodSignature(MethodBase mi)
            {
                if (HasThisType(mi))
                    mTypes.Add(GetThisType(mi));
                foreach (ParameterInfo pi in mi.GetParameters())
                    mTypes.Add(pi.ParameterType);
            }

            public bool Matches(ITypeArray types)
            {
                if (mTypes.Count > types.Count)
                    return false;
                for (int i = 0; i < mTypes.Count; ++i)
                {
                    Type expected = mTypes[mTypes.Count - (i + 1)];
                    Type actual = types.GetType(i);
                    if (!expected.IsAssignableFrom(actual))
                        return false;
                }
                return true;
            }

            public int Count() 
            {
                return mTypes.Count;
            }

            public bool IsBetterMatchThan(MethodSignature sig)
            {

                if (Count() > sig.Count()) return true;
                if (sig.Count() > Count()) return false;

                if (Count() == 0) 
                    throw new Exception("ambiguous method lookup: both methods have no parameters"); 

                Type t = mTypes[0];
                Type u = sig.mTypes[0];

                if (!(t.Equals(u)))
                {
                    if (t.IsAssignableFrom(u))
                    {
                        // This is my assumption about the method
                        Trace.Assert(!(u.IsAssignableFrom(t)));

                        // u is a subclass of t 
                        // therefore "sig" is more specific
                        return false;
                    }
                    else
                    {
                        // This is my assumption about the method
                        Trace.Assert(u.IsAssignableFrom(t));

                        // t is a subclass of u 
                        // therefore "this" is more specific
                        return true;
                    }
                }
                else 
                {
                    throw new Exception("ambiguous method lookup, both methods have the same first parameter");
                }
            }
        }

        MethodBase mMethod;
        MethodSignature mSig;

        public static string MethodToDesc(MethodBase mi)
        {
            return "";
            /*
            if (mi.IsConstructor)
                return "constructor for " + mi.DeclaringType.ToString();
            else
                if (HasThisType(mi))
                    return "method for " + mi.DeclaringType.ToString();
                else
                    return "static method for " + mi.DeclaringType.ToString();
             */
        }

        public static string MethodToName(MethodBase mi)
        {
            if (mi.IsConstructor)
                return mi.DeclaringType.Name;
            else
                return mi.Name;
        }

        public Method(MethodBase mi)
            : base(MethodToName(mi), MethodToTypeString(mi), MethodToDesc(mi))
        {
            mMethod = mi;
            mSig = new MethodSignature(mi);
        }

        /// <summary>
        /// Throws an exception if the current method can't be legally called on the stack
        /// </summary>
        /// <param name="stk"></param>
        void CheckCallIsValid(CatStack stk)
        {
            ParameterInfo[] piArray = mMethod.GetParameters();
            int nCnt = piArray.Length;
            if (stk.Count < nCnt)
                throw new Exception("could not call method " + mMethod.ToString() + ", insuffucient values on stack");

            for (int i = 0; i < nCnt; ++i)
            {
                ParameterInfo pi = mMethod.GetParameters()[nCnt - (i + 1)];
                Object o = stk[i];

                if (!pi.ParameterType.IsAssignableFrom(o.GetType()))
                    throw new Exception("could not call method " + mMethod.ToString() + ", incorrect type on the stack. Expected "
                       + pi.ParameterType + " instead found " + o.GetType());
            }

            if (HasThisType(mMethod))
            {
                if (stk.Count < nCnt + 1)
                    throw new Exception("could not call method " + mMethod.ToString() + ", insuffucient values on stack");

                Object self = stk[nCnt];
                if (!mMethod.DeclaringType.IsAssignableFrom(self.GetType()))
                    throw new Exception("could not call method " + mMethod.ToString() + ", incorrect object type");
            }
        }

        /// <summary>
        /// The Cat verion of Invoke. The arguments are taken from the stack, 
        /// and the result (if applicable) is pushed onto the stack.
        /// </summary>
        /// <param name="stk"></param>
        public override void Eval(Executor exec)
        {
            CatStack stk = exec.GetStack();

            // Throws an exception if any of the arguments are incorrect
            CheckCallIsValid(stk);

            // Create an empty list of arguments for invocation of the methdo
            List<Object> args = new List<Object>();
            
            // get the arguments from the stack for each parameters
            foreach (ParameterInfo pi in mMethod.GetParameters())
            {
                Object o = stk.Pop();
                args.Insert(0, o);
            } 
            
            // Peek at the "this" pointer from the stack (don't remove)
            Object self = null;
            if (HasThisType(mMethod))
                self = stk.Peek();

            // invoke the method and store the return value
            Object ret;
            if (mMethod.IsConstructor)
            {
                ConstructorInfo ci = mMethod as ConstructorInfo;
                ret = ci.Invoke(args.ToArray());
            }
            else
            {
                ret = mMethod.Invoke(self, args.ToArray());
            }
            
            // if there is a return type then we push the result
            if (HasReturnType(mMethod))
                stk.Push(ret);
        }

        public MethodSignature GetSignature()
        {
            return mSig;
        }

        /// <summary>
        /// A factory function for creating CatMethods from MethodBase
        /// objects, if the requirements are met. 
        /// </summary>
        /// <param name="mi"></param>
        /// <returns></returns>
        public static Method Create(MethodBase mi)
        {
            if (mi.ContainsGenericParameters)
                return null;
            if (mi.IsPrivate)
                return null;
            return new Method(mi);
        }
    }
}
