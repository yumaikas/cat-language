/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

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

    public class Unifier
    {
        List<string> mLeft = new List<string>();
        List<string> mRight = new List<string>();
        Dictionary<string, CatKind> mUnifiers = new Dictionary<string, CatKind>();            
        
        public void Clear()
        {
            mLeft.Clear();
            mRight.Clear();
            mUnifiers.Clear();
        }

        public override string ToString()
        {
            Trace.Assert(mRight.Count == mLeft.Count);
            string ret = "";
            for (int i = 0; i < mLeft.Count; ++i)
                ret += mLeft[i] + " = " + mRight[i] + "\n";
            return ret;
        }

        public void AddEquality(string sLeft, string sRight)
        {
            if (sLeft.Length < 1) throw new Exception("kind names can not be empty");
            if (sRight.Length < 1) throw new Exception("kind names can not be empty");
            
            if (Renamer.IsStackVarName(sLeft) != Renamer.IsStackVarName(sRight)) 
                throw new Exception(sLeft + " and " + sRight + " are not both types or both stacks");

            // Check for vacuous knowledge
            if (sLeft == sRight)
                return;

            mLeft.Add(sLeft);
            mRight.Add(sRight);
        }

        public void AddVarDefinition(string sVar, CatKind k)
        {
            Trace.Assert(!(k is CatStackVar));
            Trace.Assert(!(k is CatTypeVar));

            if (mUnifiers.ContainsKey(sVar))
            {
                // Look at the current unifier
                CatKind u = mUnifiers[sVar];

                if (!(u is CatStackVar) && !(u is CatTypeVar))
                {
                    // Well we have two hard-coded types, 
                    // so we have to check for equivalency.
                    if (!k.Equals(u))
                        throw new KindException(k, u);
                
                    // Note: a more sophisticated algorithm would choose the more refined type 
                    // and use that one.
                    return;
                }

                // Replace all unification variables with the actual type.
                List<string> keysToReplace = new List<string>();
                foreach (string tmpKey in mUnifiers.Keys)
                    if (u.Equals(mUnifiers[tmpKey]))
                        keysToReplace.Add(tmpKey);
                foreach (string tmpKey in keysToReplace)
                    mUnifiers[tmpKey] = k;                                                        
            }
            else
                Unify(sVar, k);
        }

        public Dictionary<string, CatKind> GetUnifiers()        
        {
            Renamer r = new Renamer(mUnifiers);
            Dictionary<string, CatKind> ret = new Dictionary<string, CatKind>();
            
            foreach (string s in mUnifiers.Keys)
                ret.Add(s, r.Rename(mUnifiers[s]));

            return ret;
        }

        public void ComputeCoreUnifiers()
        {
            mUnifiers.Clear();
            Trace.Assert(mLeft.Count == mRight.Count);

            while (mLeft.Count > 0)
            {
                string sLeft = mLeft[0];
                string sRight = mRight[0];
                Trace.Assert(Renamer.IsStackVarName(sLeft) == Renamer.IsStackVarName(sRight));
                CatKind kUnifier = Renamer.GenerateNewVar(sLeft);
                mLeft.RemoveAt(0);
                mRight.RemoveAt(0);
                Unify(sLeft, kUnifier);
                Unify(sRight, kUnifier);
            }
        }

        // Probably not an efficient unification algorithm
        public void Unify(string sVar, CatKind kUnifier)
        {
            // Check if we need to unify a new variable.
            if (mUnifiers.ContainsKey(sVar))
                return;
            
            mUnifiers.Add(sVar, kUnifier);

            int i = 0;
            while (i < mLeft.Count)
            {
                Trace.Assert(mLeft.Count == mRight.Count);

                if (mLeft[i] == sVar) 
                {
                    string sRight = mRight[i];
                    mLeft.RemoveAt(i);
                    mRight.RemoveAt(i);
                    Unify(sRight, kUnifier);
                }
                else if (mRight[i] == sVar)
                {
                    string sLeft = mLeft[i];
                    mLeft.RemoveAt(i);
                    mRight.RemoveAt(i);
                    Unify(sLeft, kUnifier);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    public class Renamer
    {
        static int gnId = 0;
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
            char c = s[0];
            if (char.IsLower(c))
                return false;
            else
                return true;
        }

        public static string GenerateNewName(string s)
        {
            if (IsStackVarName(s))
                return "S" + (gnId++).ToString();
            else
                return "t" + (gnId++).ToString();
        }

        public static CatKind GenerateNewVar(string s)
        {
            if (IsStackVarName(s))
                return new CatStackVar(GenerateNewName(s));
            else
                return new CatTypeVar(GenerateNewName(s));
        }
        #endregion

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
            else if (k is CatStackKind)
                return Rename(k as CatStackKind);
            else
                throw new Exception(k.ToString() + " is an unrecognized kind");
        }

        public CatFxnType Rename(CatFxnType f)
        {
            if (f == null)
                throw new Exception("Invalid null parameter to rename function");
            return new CatFxnType(Rename(f.GetCons()), Rename(f.GetProd()));
        }

        public CatStackKind Rename(CatStackKind s)
        {
            if (s == null)
                throw new Exception("Invalid null parameter to rename function");
            if (s.IsEmpty())
            {
                return s;
            }
            else if (s is CatStackVar)
            {
                string sName = s.ToString();
                if (mNames.ContainsKey(sName))
                {
                    CatStackKind ret = mNames[sName] as CatStackKind;
                    if (ret == null)
                        throw new Exception(sName + " is not a stack kind");
                    return ret;
                }

                if (!mbGenerateNames)
                    return s;

                CatStackVar var = new CatStackVar(GenerateNewName(sName));
                mNames.Add(sName, var);
                return var;
            }
            else
            {
                return CatKind.CreateStackKind(Rename(s.GetRest()), Rename(s.GetTop()));
            }
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

        public static void ResetId()
        {
            gnId = 0;
        }
    }

    public class TypeInferer
    {
        Dictionary<string, CatStackKind> mStackVars = new Dictionary<string, CatStackKind>();
        Dictionary<string, CatTypeKind> mTypeVars = new Dictionary<string, CatTypeKind>();
        Unifier mu = new Unifier();

        private void ResolveStackVars(string name, CatStackKind stk, Dictionary<string, CatKind> unifiers)
        {
            /*
            if (stk is CatStackVar)
                throw new Exception("Stack variable equalties should already have been added to the unifier");

            CatStackKind result = null;

            // Used for manual reconstruction of the stack.
            List<CatTypeKind> types = new List<CatTypeKind>();

            // This is more or less the same algorithm
            // as the renaming algorithm
            while (!stk.IsEmpty())
            {
                if (stk is CatStackVar)
                {
                    string s = stk.ToString();
                    if (unifiers.ContainsKey(s))
                        result = unifiers[s] as CatStackKind;
                    else
                        result = stk;
                    break;
                }
                else
                {
                    CatTypeKind t = stk.GetTop();
                    
                    // TODO: fix!
                    // t = RenameVars(t, unifiers);
                    
                    types.Add(t);
                }
            }

            // This really makes me think that I designed the stack kind class rather badly 
            // I've made a note to fix it some day.
            if (result == null)
            {
                // if we get to the bottom of the stack, and didn't find a stack variable
                // then something is horribly wrong with the code.
                throw new Exception("Stacks must always have a stack variable at the bottom");
            }

            // Reconstruct the stack
            foreach (CatTypeKind t in types)
                result = CatKind.CreateStackKind(result, t);

            unifiers.Add(name, result);
             */
        }

        private void AddTypeVar(string s, CatTypeKind t)
        {
            if (t is CatTypeVar)
            {
                mu.AddEquality(s, t.ToString());
            }
            else
            {
                if (mTypeVars.ContainsKey(s))
                {
                    CatTypeKind u = mTypeVars[s];
                    
                    // Type variables should never occur as in the type variable lookup
                    // that is what the the TheoremProver is for. 
                    Trace.Assert(!(u is CatTypeVar));
                    
                    // I am just restating that t can't be a CatTypeVar if we are in
                    // this block of code. 
                    Trace.Assert(!(t is CatTypeVar));

                    // It is impossible for AddtypeConstraint to re-enter to this position
                    // creating an infinite loop.
                    AddTypeConstraint(t, u);
                }
                else
                {
                    mTypeVars.Add(s, t);
                }
            }           
        }

        private void AddStackVar(string s, CatStackKind t)
        {
            if (t is CatStackVar)
            {
                mu.AddEquality(s, t.ToString());
            }
            else
            {
                if (mStackVars.ContainsKey(s))
                {
                    CatStackKind u = mStackVars[s];

                    // Stack variables should never occur in the stack variable lookup.
                    // That is what the the TheoremProver is for. 
                    Trace.Assert(!(u is CatStackVar));

                    // I am just restating that "t" can't be a CatStackVar if we are in
                    // this block of code. 
                    Trace.Assert(!(t is CatStackVar));

                    // This won't cause an infinite loop because neither t nor u are 
                    // stack variables
                    AddStackConstraint(t, u);
                }
                else
                {
                    mStackVars.Add(s, t);
                }
            }
        }

        private void AddStackConstraint(CatStackKind left, CatStackKind right)
        {
            if (!left.IsEmpty() && !right.IsEmpty())
            {
                if (left is CatStackVar)
                {
                    AddStackVar(left.ToString(), right);
                }
                else if (right is CatStackVar)
                {
                    AddStackVar(right.ToString(), left);
                }
                else
                {
                    AddTypeConstraint(left.GetTop(), right.GetTop());
                    AddStackConstraint(left.GetRest(), right.GetRest());
                }
            }
            else if (left.IsEmpty())
            {
                if (!right.IsEmpty())
                {
                    if (right is CatStackVar)
                    {
                        AddStackVar(right.ToString(), left);
                    }
                    else
                    {
                        // An empty stack is being equated with 
                        // a non-empty stack which is not a stack variable
                        throw new KindException(left, right);
                    }
                }
            }
            else 
            {
                // Just stating known facts for clarification
                Trace.Assert(!left.IsEmpty());
                Trace.Assert(right.IsEmpty());
                
                if (left is CatStackVar)
                {
                    AddStackVar(left.ToString(), right);
                }
                else
                {
                    // An empty stack is being equated with 
                    // a non-empty stack which is not a stack variable
                    throw new KindException(left, right);
                }
            }
        }

        private void AddTypeConstraint(CatTypeKind x, CatTypeKind y)
        {
            Trace.Assert(x != null);
            Trace.Assert(y != null);

            if (x is CatTypeVar)
            {
                AddTypeVar(x.ToString(), y);
            }
            else if (y is CatTypeVar)
            {
                AddTypeVar(y.ToString(), x);
            }
            else if (x is CatFxnType)
            {
                if (!(y is CatFxnType))
                    throw new KindException(x, y);

                AddFxnConstraint(x as CatFxnType, y as CatFxnType);
            }
            else
            {
                if (!x.ToString().Equals(y.ToString()))
                    throw new KindException(x, y);
            }
        }

        private void AddFxnConstraint(CatFxnType x, CatFxnType y)
        {
            AddStackConstraint(x.GetCons(), y.GetCons());
            AddStackConstraint(x.GetProd(), y.GetProd());
        }

        private void CompareTypes(CatKind x, CatKind y)
        {
            // make sure that the types are the same.
            if (!x.ToString().Equals(y.ToString()))
                throw new KindException(x, y);
        }

        #region public functions
        /// <summary>
        /// A composed function satisfy the type equation 
        /// 
        ///   ('A -> 'B) ('C -> 'D) compose == ('A -> 'D) and ('B == 'C)
        /// 
        /// This makes the raw type trivial to determine, but the result isn't helpful 
        /// because 'D is not expressed in terms of the variables of 'A. The goal of 
        /// type inference is to find new variables that unify 'A and 'C based on the 
        /// observation that the production of the left function must be equal to the 
        /// consumption of the second function
        /// </summary>
        public CatFxnType InferType(CatFxnType left, CatFxnType right)
        {
            Renamer r = new Renamer();
            left = r.Rename(left);
            r.ResetNames();
            right = r.Rename(right);
           
            CatStackKind stkRightCons = right.GetCons();
            CatStackKind stkLeftProd = left.GetProd();

            // The production of the left function must be equal to 
            // the consumption of the second function
            AddStackConstraint(stkRightCons, stkLeftProd);

            if (Config.gbShowInferenceProcess)
            {
                // Create a temporary function type showing the type before 
                // unfification
                CatFxnType tmp = new CatFxnType(left.GetCons(), right.GetProd());
                MainClass.WriteLine("Before unification: ");
                MainClass.WriteLine(tmp.ToString());

                // Show the top level contraint
                MainClass.WriteLine(left.GetProd() + " = " + right.GetCons());

                MainClass.WriteLine("Constraints:");
                
                // Show stack variable names
                foreach (KeyValuePair<string, CatStackKind> kvp in mStackVars)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
                
                // Show type variable names 
                foreach (KeyValuePair<string, CatTypeKind> kvp in mTypeVars)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());

                // Show all equalties generated
                MainClass.Write(mu.ToString());
            }

            // This creates new variables which "unify" the original types.
            // Each original variables is assigned a generated variable. If two original 
            // variables are equal (either directly or by commutativity) they will
            // be replaced with the same generated variable. 
            mu.ComputeCoreUnifiers();
            
            // Add type variable definitions
            foreach (KeyValuePair<string, CatTypeKind> kvp in mTypeVars)
                mu.AddVarDefinition(kvp.Key, kvp.Value);

            // Add stack variable definitions
            foreach (KeyValuePair<string, CatStackKind> kvp in mStackVars)
                mu.AddVarDefinition(kvp.Key, kvp.Value);

            // Note: at this point I am still not completely satisfied 
            // because definitions of stack variables can contain other stack variables 
            // and function variables, but those aren't getting properly added.
            // so I am missing an important recursive step here.

            Dictionary<string, CatKind> unifiers = mu.GetUnifiers();
            r = new Renamer(unifiers);

            if (Config.gbShowInferenceProcess)
            {
                MainClass.WriteLine("Unifiers:");
                foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                    MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
            }

            // The left consumption and right production make up the result type.
            CatStackKind stkLeftCons = r.Rename(left.GetCons());
            CatStackKind stkRightProd = r.Rename(right.GetProd());
            
            // Finally create and return the result type
            CatFxnType ret = new CatFxnType(stkLeftCons, stkRightProd);

            if (Config.gbShowInferenceProcess || Config.gbShowInferredType)
                MainClass.WriteLine("Type: " + ret.ToString());
            
            return ret;
        }

        public void OutputUnifiers(Dictionary<string, CatKind> unifiers)
        {
            MainClass.WriteLine("Unifiers:");
            foreach (KeyValuePair<string, CatKind> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + " = " + kvp.Value.ToString());
        }
        #endregion 
    }
}
