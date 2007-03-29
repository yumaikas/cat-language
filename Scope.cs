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
    /// Every program and type is associated with a single scope. 
    /// The current implementation of Cat does not support nested scopes, so 
    /// for the time being everything shares the global scope. 
    /// 
    /// The inclusion of non-static Scope objects lays the groundwork for extending 
    /// Cat with the idea of nested functions and namespaces. These are very important 
    /// concepts for non-trivial software development. 
    /// </summary>
    public class Scope
    {
        private string msName = "";
        private Scope mpParent = null;
        private List<Scope> mpChildren = new List<Scope>();
        private Dictionary<string, List<Method>> mpMethods = new Dictionary<string, List<Method>>();
        private Dictionary<string, Function> mpFunctions = new Dictionary<string, Function>();
        static Scope gpGlobal = new Scope();

        public Scope()
        {
        }

        #region static functions
        public static Scope Global()
        {
            return gpGlobal;
        }
        #endregion

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
            return mpMethods.ContainsKey(s) || mpFunctions.ContainsKey(s);
        }

        public List<Function> Lookup(ITypeArray piTypes, string s)
        {
            List<Function> results = new List<Function>();

            if (s.Length < 1) 
                return results;

            if (mpMethods.ContainsKey(s))
            {
                List<Method> methods = mpMethods[s];

                foreach (Method m in methods)
                    if (m.GetSignature().Matches(piTypes))
                        results.Add(m);
            }

            if (mpFunctions.ContainsKey(s))
                results.Add(mpFunctions[s]);

            return results;
        }

        public void Clear()
        {
            mpChildren.Clear();
            mpMethods.Clear();
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

        public void AddMethod(Method f)
        {
            // TODO: someday check that there aren't conflicting method signatures.
            string s = f.GetName();
            if (!mpMethods.ContainsKey(s))
                mpMethods.Add(s, new List<Method>());
            mpMethods[s].Add(f);
        }

        public Dictionary<String, Function>.ValueCollection GetAllFunctions()
        {
            return mpFunctions.Values;
        }

        public Dictionary<String, List<Method>>.ValueCollection GetAllMethods()
        {
            return mpMethods.Values;
        }
        #endregion

        public void Register(Type t)
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
                    Register(memberType);
                }
            }
            foreach (MemberInfo mi in t.GetMembers())
            {
                if ((mi is MethodInfo) || (mi is ConstructorInfo))
                {
                    Method method = Method.Create(mi as MethodBase);
                    if (method != null)
                        AddMethod(method);
                }
            }
        }
   }
}
