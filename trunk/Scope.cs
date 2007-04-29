/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace Cat
{
    /// <summary>
    /// A scope class holds definitions and definition lookup tables. 
    /// It could have just as easily been called a "context"
    /// Every program and type is associated with a single scope. 
    /// The current implementation of Cat does not support nested scopes, so 
    /// for the time being everything shares the global scope. 
    /// </summary>
    public class Scope
    {
        private Dictionary<string, Function> mpFunctions = new Dictionary<string, Function>();

        // The following fields are not yet supported
        private string msName = "";
        private Scope mpParent = null;
        private List<Scope> mpChildren = null;

        public Scope()
        {
        }

        public Scope(Scope x)
        {
            foreach (KeyValuePair<string, Function> kvp in x.mpFunctions)
            {
                mpFunctions.Add(kvp.Key, kvp.Value);
            }
        }

        #region public functions
        public Scope GetParent()
        {
            return mpParent;
        }
        public string GetName()
        {
            return msName;
        }

        public bool IsNamed()
        {
            return msName.Length > 0;
        }

        public string GetFullName()
        {
            string result = "";
            if (mpParent != null)
            {
                result += mpParent.GetFullName();
            }
            if (IsNamed())
            {
                result += msName + ".";
            }
            return result;
        }

        public bool FunctionExists(string s)
        {
            return mpFunctions.ContainsKey(s);
        }

        public Function Lookup(ITypeArray piTypes, string s)
        {
            if (mpFunctions.ContainsKey(s))
                return mpFunctions[s];
            else
                return null;
        }

        public Function Lookup(string s)
        {
            if (mpFunctions.ContainsKey(s))
                return mpFunctions[s];
            else
                return null;
        }

        public void Clear()
        {
            mpChildren.Clear();
            mpFunctions.Clear();
        }

        public void AddFunction(Function f)
        {
            string s = f.GetName();
            if (mpFunctions.ContainsKey(s))
            {
                if (!Config.gbAllowImplicitRedefines)
                    throw new Exception("attempting to redefine " + s);
                mpFunctions[s] = f;
            }
            else
            {
                mpFunctions.Add(s, f);
            }
        }

        /// <summary>
        /// Methods allow overloading of function definitions.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="meth"></param>
        public void AddMethod(Object o, MethodInfo mi)
        {
            if (!mi.IsPublic) 
                return;

            if (mi.IsStatic)
                o = null;
            
            Method f = new Method(o, mi);
            string s = f.GetName();
            
            if (mpFunctions.ContainsKey(s))
            {
                MethodGroup g = mpFunctions[s] as MethodGroup;
                if (g == null)
                    throw new Exception("expected method_group type, instead found " + mpFunctions[s].ToString());
                g.AddOverload(f);
            }
            else
            {
                MethodGroup g = new MethodGroup(f);
                mpFunctions.Add(s, g);
            }
        }

        public void RemoveFunctions(string s)
        {
            mpFunctions.Remove(s);
        }

        public Dictionary<String, Function>.ValueCollection GetAllFunctions()
        {
            return mpFunctions.Values;
        }
        #endregion

        public void RegisterType(Type t)
        {            
            foreach (Type memberType in t.GetNestedTypes())
            {
                // Is is it a function object
                if (typeof(Function).IsAssignableFrom(memberType))
                {
                    ConstructorInfo ci = memberType.GetConstructor(new Type[] { });
                    Object o = ci.Invoke(null);
                    if (!(o is Function))
                        throw new Exception("Expected only function objects in " + t.ToString());
                    Function f = o as Function;
                    AddFunction(f);
                }
                else
                {
                    RegisterType(memberType);
                }
            }
            foreach (MemberInfo mi in t.GetMembers())
            {
                if (mi is MethodInfo) 
                {
                    MethodInfo meth = mi as MethodInfo;
                    if (meth.IsStatic)
                    {
                        AddMethod(null, meth);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an ObjectBoundMethod for each public function in the object
        /// </summary>
        /// <param name="o"></param>
        public void RegisterObject(Object o)
        {
            foreach (MemberInfo mi in o.GetType().GetMembers())
            {
                if (mi is MethodInfo)
                {
                    MethodInfo meth = mi as MethodInfo;
                    AddMethod(o, meth);
                }
            }
        }

        public void UnregisterObject(Object o)
        {
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, Function> kvp in mpFunctions)
            {
                if (kvp.Value is Method)
                {
                    Method obm = kvp.Value as Method;
                    if (obm.GetObject() == o)
                    {
                        keys.Add(kvp.Key);
                    }
                }
            }

            foreach (string s in keys)
                mpFunctions.Remove(s);
        }
    }
}
