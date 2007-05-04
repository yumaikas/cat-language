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
        
        public void Clear()
        {
            mLeft.Clear();
            mRight.Clear();
        }

        public void OutputEqualities()
        {
            for (int i = 0; i < mLeft.Count; ++i)
                MainClass.WriteLine(mLeft[i] + " = " + mRight[i]);
        }

        public void AddEquality(string sLeft, string sRight)
        {
            // Check for vacuous knowledge
            if (sLeft == sRight)
                return;

            mLeft.Add(sLeft);
            mRight.Add(sRight);
        }

        public Dictionary<string, string> GetUnifiers()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();            
            Trace.Assert(mLeft.Count == mRight.Count);

            int n = 0;
            while (mLeft.Count > 0)
            {
                string sLeft = mLeft[0];
                string sRight = mRight[0];
                string sUnifier = "$" + (n++).ToString();
                mLeft.RemoveAt(0);
                mRight.RemoveAt(0);
                Unify(sUnifier, sLeft, ret);
                Unify(sUnifier, sRight, ret);
            }

            return ret;
        }

        // Probably not an efficient unification algorithm
        public void Unify(string sUnifier, string sVar, Dictionary<string, string> ret)
        {
            // Check if we need to unify a new variable.
            if (ret.ContainsKey(sVar))
                return;
            
            ret.Add(sVar, sUnifier);

            int i = 0;
            while (i < mLeft.Count)
            {
                Trace.Assert(mLeft.Count == mRight.Count);

                if (mLeft[i] == sVar) 
                {
                    mLeft.RemoveAt(i);
                    mRight.RemoveAt(i);
                    Unify(sUnifier, mRight[i], ret);
                }
                else if (mRight[i] == sVar)
                {
                    mLeft.RemoveAt(i);
                    mRight.RemoveAt(i);
                    Unify(sUnifier, mLeft[i], ret);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    public class TypeInferer
    {
        Dictionary<string, CatStackKind> mStackVars = new Dictionary<string, CatStackKind>();
        Dictionary<string, CatTypeKind> mTypeVars = new Dictionary<string, CatTypeKind>();
        Unifier mu = new Unifier();

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

        private bool ResolveConstraints()
        {
            // TODO:
            return true;
        }

        #region variable renaming
        int mnId = 0;
        Dictionary<string, CatKind> mRenamer = new Dictionary<string, CatKind>();

        private void InitializeRenaming()
        {
            mnId++;
            mRenamer.Clear();
        }
        
        private CatFxnType RenameVars(CatFxnType f)
        {
            return new CatFxnType(RenameVars(f.GetCons()), RenameVars(f.GetProd()));
        }

        private CatStackKind RenameVars(CatStackKind s)
        {
            if (s.IsEmpty())
            {
                return s;
            }
            else if (s is CatStackVar)
            {
                string sName = s.ToString();
                if (mRenamer.ContainsKey(sName))
                {
                    CatStackKind ret = mRenamer[sName] as CatStackKind;
                    if (ret == null)
                        throw new Exception(sName + " is not a stack kind");
                    return ret;
                }

                CatStackVar var = new CatStackVar(sName + mnId.ToString());
                mRenamer.Add(sName, var);
                return var;
            }
            else
            {
                return CatKind.CreateStackKind(RenameVars(s.GetRest()), RenameVars(s.GetTop()));
            }
        }

        private CatTypeKind RenameVars(CatTypeKind t)
        {
            if (t is CatFxnType)
            {
                return RenameVars(t as CatFxnType);
            }
            else if (t is CatTypeVar)
            {
                string sName = t.ToString();
                if (mRenamer.ContainsKey(sName))
                {
                    CatTypeKind ret = mRenamer[sName] as CatTypeKind;
                    if (ret == null)
                        throw new Exception(sName + " is not a type kind");
                    return ret;
                }

                CatTypeVar var = new CatTypeVar(sName + mnId.ToString());
                mRenamer.Add(sName, var);
                return var;
            }
            else
            {
                return t;
            }
        }
        #endregion

        #region public functions
        public CatFxnType InferType(CatFxnType left, CatFxnType right)
        {
            InitializeRenaming();
            left = RenameVars(left);

            InitializeRenaming();
            right = RenameVars(right);
           
            CatStackKind stkRightCons = right.GetCons();
            CatStackKind stkLeftProd = left.GetProd();

            AddStackConstraint(stkRightCons, stkLeftProd);

            // TODO: resolve constraints and compute the correct type

            CatStackKind stkLeftCons = left.GetCons();
            CatStackKind stkRightProd = right.GetProd();

            CatFxnType ret = new CatFxnType(stkLeftCons, stkRightProd);            
            return ret;
        }

        public void OutputConstraints()
        {
            MainClass.WriteLine("Stack Variables:");
            foreach (KeyValuePair<string, CatStackKind> kvp in mStackVars)
                MainClass.WriteLine(kvp.Key + "=" + kvp.Value.ToString());

            MainClass.WriteLine("Type Variables:");
            foreach (KeyValuePair<string, CatTypeKind> kvp in mTypeVars)
                MainClass.WriteLine(kvp.Key + "=" + kvp.Value.ToString());

            MainClass.WriteLine("Equalities:");
            mu.OutputEqualities();

            MainClass.WriteLine("Unifiers:");
            Dictionary<string, string> unifiers = mu.GetUnifiers();
            foreach (KeyValuePair<string, string> kvp in unifiers)
                MainClass.WriteLine(kvp.Key + "=" + kvp.Value);
        }
        #endregion 
    }
}
