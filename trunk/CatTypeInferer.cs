/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class KindException : Exception
    {
        public CatKind mFirst;
        public CatKind mSecond;

        public KindException(CatKind first, CatKind second)
            : base("Incompatible kinds " + first.ToString() + " conflicts with " + second.ToString())
        {
            mFirst = first;
            mSecond = second;
        }
    }

    /// <summary>
    /// Unifiers are types associated with variables. These types may be other variables. So 
    /// it can be considered a directed graph. It is important to assure that no cycles ever exist.
    /// </summary>
    public class Unifiers
    {
        int mnId = 0;
        Dictionary<string, CatKind> mUnifiers = new Dictionary<string, CatKind>();
        
        public override string ToString()
        {
            string ret = "";
            foreach (KeyValuePair<string, CatKind> kvp in mUnifiers)
                ret += kvp.Key + " = " + kvp.Value.ToString() + "\n";
            return ret;
        }

        public static void TypeCheck(CatKind k1, CatKind k2)
        {
            // TODO: check that k1 and k2 are compatible
        }

        public void AddVectorConstraint(CatTypeVector v1, CatTypeVector v2)
        {
            while (!v1.IsEmpty() && !v2.IsEmpty())
            {
                CatKind k1 = v1.GetTop();
                CatKind k2 = v2.GetTop();

                // TODO: this might not cover all typing possibilities.
                if (k1 is CatStackVar)
                {
                    AddConstraint(k1.ToString(), v2);
                    if (k2 is CatStackVar)
                        AddConstraint(k2.ToString(), v1);
                    return;
                }
                else if (k2 is CatStackVar)
                {
                    AddConstraint(k2.ToString(), v1);
                    return;
                }
                
                if (k1 is CatTypeVar)
                {
                    AddConstraint(k1.ToString(), k2);
                }
                if (k2 is CatTypeVar)
                {
                    AddConstraint(k2.ToString(), k1);
                }
                if ((k1 is CatFxnType) && (k2 is CatFxnType))
                {
                    AddFxnConstraint(k1 as CatFxnType, k2 as CatFxnType);
                }

                TypeCheck(k1, k2);

                v1 = v1.GetRest();
                v2 = v2.GetRest();
            }
        }

        public void AddFxnConstraint(CatFxnType f1, CatFxnType f2)
        {
            AddVectorConstraint(f1.GetCons(), f2.GetCons());
            AddVectorConstraint(f1.GetProd(), f2.GetProd());
        }

        private void ReplaceUnifiers(CatKind kOld, CatKind kNew)
        {
            // HACK: this could be improved 
            Dictionary<string, CatKind> dict = new Dictionary<string, CatKind>();
            foreach (KeyValuePair<string, CatKind> kvp in mUnifiers)
            {
                if (kvp.Value == kOld){
                    dict.Add(kvp.Key, kNew);
                }
                else
                {
                    dict.Add(kvp.Key, kvp.Value);
                }
            }
            mUnifiers = dict;
        }

        public void AddConstraint(string s, CatKind k)               
        {
            // Don't add self-referential variables 
            if (k.ToString().CompareTo(s) == 0)
                return;

            // Check for single unit vectors 
            if (k is CatTypeVector)
            {
                CatTypeVector vec = k as CatTypeVector;
                if (vec.GetKinds().Count == 1)
                {
                    // vectors with only one thing, are really that thing. 
                    AddConstraint(s, vec.GetKinds()[0]);
                    return;
                }
            }

            if (mUnifiers.ContainsKey(s))
            {
                CatKind u = CreateUnifier(k, mUnifiers[s]);
                ReplaceUnifiers(mUnifiers[s], u);
            }
            else
            {
                if (k.IsKindVar() && mUnifiers.ContainsKey(k.ToString()))
                {
                    CatKind u = CreateUnifier(k, mUnifiers[k.ToString()]);
                    ReplaceUnifiers(mUnifiers[k.ToString()], u);
                    mUnifiers.Add(s, u);
                }
                else
                {
                    mUnifiers.Add(s, CreateUnifier(k, k));
                }
            }
        }

        private CatKind CreateUnifier(CatKind k1, CatKind k2)
        {
            if ((k1 is CatFxnType) || (k2 is CatFxnType))
            {
                if (!(k1 is CatFxnType)) return k2;
                if (!(k2 is CatFxnType)) return k1;
                CatFxnType ft1 = k1 as CatFxnType;
                CatFxnType ft2 = k2 as CatFxnType;
                if (ft1.GetCons().GetKinds().Count >= ft2.GetCons().GetKinds().Count)  return ft1;
                return ft2;
            }
            else if ((k1 is CatTypeVector) || (k2 is CatTypeVector))
            {
                if (!(k1 is CatTypeVector)) return k2;
                if (!(k2 is CatTypeVector)) return k1;
                CatTypeVector vec1 = k1 as CatTypeVector;
                CatTypeVector vec2 = k2 as CatTypeVector;
                // TODO: check if they are all types in each vector. If so and 
                // the number of types differs we have a type error
                if (vec1.GetKinds().Count >= vec2.GetKinds().Count) return vec1;
                return vec2;
            }
            else if (k1 is CatTypeVar) 
            {
                if (k2 is CatStackKind)
                    throw new Exception(k1.ToString() + " is a type and is not compatible with the type vector " + k2.ToString());
                return new CatTypeVar("u" + mnId++.ToString());
            }
            else if (k1 is CatStackVar)
            {
                if (!(k2 is CatStackVar))
                    throw new Exception("Stack variable " + k1.ToString() + " is not compatible with " + k2.ToString());
                return new CatStackVar("U" + mnId++.ToString());
            }
            else
            {
                throw new Exception("Unsupported kinds " + k1.ToString() + ":" + k1.GetType().ToString() 
                    + " and " + k2.ToString() + ":" + k2.GetType().ToString());
            }
        }

        public Dictionary<string, CatKind> GetResolvedUnifiers()        
        {
            Dictionary<string, CatKind> ret = new Dictionary<string, CatKind>();

            Stack<CatKind> stk = new Stack<CatKind>();
            foreach (string s in mUnifiers.Keys)
            {
                ret.Add(s, ResolveKind(s, mUnifiers[s], stk));
                Trace.Assert(stk.Count == 0);
            }

            return ret;
        }

        private CatKind ResolveKind(string s, CatKind k, Stack<CatKind> visited)
        {
            Trace.Assert(k != null);
            Trace.Assert(visited != null);

            if (visited.Contains(k))
            {
                // TODO: if it is simply self-referential use "self"
                throw new Exception("Circular type reference");
            }

            if (k.IsKindVar())
            { 
                if (mUnifiers.ContainsKey(k.ToString()))
                {
                    visited.Push(k);
                    CatKind ret = ResolveKind(s, mUnifiers[k.ToString()], visited);
                    visited.Pop();
                    return ret;
                }
                return k;
            }

            if (k is CatFxnType)
            {
                visited.Push(k);
                CatTypeVector cons = new CatTypeVector();
                CatTypeVector prod = new CatTypeVector();
                
                CatFxnType ft = k as CatFxnType;
                foreach (CatKind tmp in ft.GetCons().GetKinds())
                    cons.AddBottom(ResolveKind(s, tmp, visited));
                foreach (CatKind tmp in ft.GetProd().GetKinds())
                    prod.AddBottom(ResolveKind(s, tmp, visited));

                CatFxnType ret = new CatFxnType(cons, prod, ft.HasSideEffects());
                visited.Pop();
                return ret;
            }
            else if (k is CatTypeVector)
            {
                visited.Push(k);
                CatTypeVector vec = k as CatTypeVector;
                CatTypeVector ret = new CatTypeVector();
                foreach (CatKind tmp in vec.GetKinds())
                    ret.AddBottom(ResolveKind(s, tmp, visited));
                visited.Pop();
                return ret;
            }
            else if (k is CatLabeledType)
            {
                throw new Exception("labeled types not handled yet");
            }
            else
            {
                return k;
            }
        }

        public void Clear()
        {
            mnId = 0;
            mUnifiers.Clear();
        }
    }

    public class TypeInferer
    {
        Unifiers mUnifiers = new Unifiers();

        static TypeInferer gInferer = new TypeInferer();

        public static CatFxnType Infer(CatFxnType f, CatFxnType g, bool bSilent)
        {
            Config.gbVerboseInference = !bSilent;

            if (f == null) return null;
            if (g == null) return null;

            try
            {
                return gInferer.InferType(f, g);
            }
            catch
            {
                return null;
            }
        }

        public static CatFxnType Infer(List<Function> f, bool bSilent)
        {
            try
            {
                if (f.Count == 0)
                {
                    return CatFxnType.Create("( -> )");
                }
                else if (f.Count == 1)
                {
                    Function x = f[0];
                    return x.GetFxnType();
                }
                else
                {
                    Function x = f[0];
                    CatFxnType ft = x.GetFxnType();

                    for (int i = 1; i < f.Count; ++i)
                    {
                        if (ft == null)
                            return ft;
                        Function y = f[i];
                        ft = TypeInferer.Infer(ft, y.GetFxnType(), bSilent);
                    }
                    return ft;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// A composed function satisfy the type equation 
        /// 
        ///   ('A -> 'B) ('C -> 'D) compose : ('A -> 'D) with constraints ('B == 'C)
        /// 
        /// This makes the raw type trivial to determine, but the result isn't helpful 
        /// because 'D is not expressed in terms of the variables of 'A. The goal of 
        /// type inference is to find new variables that unify 'A and 'C based on the 
        /// observation that the production of the left function must be equal to the 
        /// consumption of the second function
        /// </summary>
        private CatFxnType InferType(CatFxnType left, CatFxnType right)
        {
            mUnifiers.Clear();
            Renamer renamer = new Renamer();
            left = renamer.Rename(left);
            renamer.ResetNames(); 
            right = renamer.Rename(right);           
            
            mUnifiers.AddVectorConstraint(left.GetProd(), right.GetCons());

            if (Config.gbVerboseInference)
            {
                // Create a temporary function type showing the type before unfification
                CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd(), left.HasSideEffects() || right.HasSideEffects());
                MainClass.WriteLine("Before unification: ");
                MainClass.WriteLine(tmp.ToString());

                MainClass.WriteLine("Unresolved unifiers:");
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());
                MainClass.Write(mUnifiers.ToString());
            }

            Dictionary<string, CatKind> unifiers = mUnifiers.GetResolvedUnifiers();
            renamer = new Renamer(unifiers);

            if (Config.gbVerboseInference)
            {                
                MainClass.WriteLine("Resolved unifiers:");
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }                

            // The left consumption and right production make up the result type.
            CatTypeVector stkLeftCons = renamer.Rename(left.GetCons());
            CatTypeVector stkRightProd = renamer.Rename(right.GetProd());
            
            // Finally create and return the result type
            CatFxnType ret = new CatFxnType(stkLeftCons, stkRightProd, left.HasSideEffects() || right.HasSideEffects());
            
            if (Config.gbVerboseInference)
            {
                MainClass.WriteLine("Type: " + ret.ToString());
            }

            // And one last renaming for good measure:
            renamer = new Renamer();
            return renamer.Rename(ret);
        }

        public void OutputUnifiers(Dictionary<string, CatKind> unifiers)
        {
            MainClass.WriteLine("Unifiers:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
    }

    /// <summary>
    /// The renamer assigns new names to a set of variables either from a supplied 
    /// dictionary or by generating unique names.
    /// </summary>
    public class Renamer
    {
        int mnId = 0;
        bool mbGenerateNames;

        Dictionary<string, CatKind> mNames;

        #region constructors

        /// <summary>
        /// When this cosntructor is used the renamer will not generate any new names
        /// and will instead simply use the dictionary given to look up kinds.
        /// </summary>
        public Renamer(Dictionary<string, CatKind> names)
        {
            mNames = names;
            mbGenerateNames = false;
        }

        public Renamer()
        {
            mNames = new Dictionary<string, CatKind>();
            mbGenerateNames = true;
        }
        #endregion

        #region static functions
        public static bool IsStackVarName(string s)
        {
            Trace.Assert(s.Length > 0);
            Trace.Assert(s[0] == '\'');
            char c = s[1];
            if (char.IsLower(c))
                return false;
            else
                return true;
        }

        public string GenerateNewName(string s)
        {
            if (IsStackVarName(s))
                return "S" + (mnId++).ToString();
            else
                return "t" + (mnId++).ToString();
        }

        public CatKind GenerateNewVar(string s)
        {
            if (IsStackVarName(s))
                return new CatStackVar(GenerateNewName(s));
            else
                return new CatTypeVar(GenerateNewName(s));
        }
        #endregion

        /// <summary>
        /// This allows unique names to continue to be generated, from previously used variable names.
        /// </summary>
        public void ResetNames()
        {
            mNames.Clear();
        }

        public CatKind Rename(CatKind k)
        {
            if (k is CatFxnType)
                return Rename(k as CatFxnType);
            else if (k is CatTypeKind)
                return Rename(k as CatTypeKind);
            else if (k is CatStackVar)
                return Rename(k as CatStackVar);
            else if (k is CatTypeVector)
                return Rename(k as CatTypeVector);
            else
                throw new Exception(k.ToString() + " is an unrecognized kind");
        }

        public CatFxnType Rename(CatFxnType f)
        {
            if (f == null)
                throw new Exception("Invalid null parameter to rename function");
            return new CatFxnType(Rename(f.GetCons()), Rename(f.GetProd()), f.HasSideEffects());
        }

        public CatTypeVector Rename(CatTypeVector s)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in s.GetKinds())
                ret.AddTop(Rename(k));
            return ret;
        }

        public CatStackKind Rename(CatStackVar s)
        {
            string sName = s.ToString();
            if (mNames.ContainsKey(sName))
            {
                CatKind tmp = mNames[sName];
                if (!(tmp is CatStackKind))
                    throw new Exception(sName + " is not a stack kind");
                return tmp as CatStackKind;
            }

            if (!mbGenerateNames)
                return s;

            CatStackVar var = new CatStackVar(GenerateNewName(sName));
            mNames.Add(sName, var);
            return var;
        }

        public CatTypeKind Rename(CatTypeKind t)
        {
            if (t == null)
                throw new Exception("Invalid null parameter to rename function");
            if (t is CatFxnType)
            {
                return Rename(t as CatFxnType);
            }
            else if (t is CatTypeVar)
            {
                string sName = t.ToString();
                if (mNames.ContainsKey(sName))
                {
                    CatTypeKind ret = mNames[sName] as CatTypeKind;
                    if (ret == null)
                        throw new Exception(sName + " is not a type kind");
                    return ret;
                }

                if (!mbGenerateNames)
                    return t;

                CatTypeVar var = new CatTypeVar(GenerateNewName(sName));
                mNames.Add(sName, var);
                return var;
            }
            else
            {
                return t;
            }
        }
    }
}
