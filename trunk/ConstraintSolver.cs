using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class Pair<T>
    {
        public T First;
        public T Second;
        public Pair(T first, T second)
        {
            First = first;
            Second = second;
        }
    }

    public class Constraint
    {
        public virtual IEnumerable<Constraint> GetSubConstraints()
        {
            yield return this;
        }

        public bool EqualsVar(string s)
        {
            if (!(this is Var))
                return false;
            return ToString().Equals(s);
        }
    }

    public class Var : Constraint
    {
        protected string m;

        public Var(string s)
        {
            m = s;
        }

        public override string ToString()
        {
            return m;
        }
    }

    public class ScalarVar : Var
    {
        public ScalarVar(string s)
            : base(s)
        {
        }
    }

    public class VectorVar : Var
    {
        public VectorVar(string s)
            : base(s)
        {
        }
    }

    public class Constant : Constraint
    {
        string m;
        public Constant(string s)
        {
            m = s;
        }
        public override string ToString()
        {
            return m;
        }
    }

    public class Relation : Constraint
    {
        Vector mLeft;
        Vector mRight;

        List<Var> mVars = new List<Var>();

        public Relation(Vector left, Vector right)
        {
            mLeft = left;
            mRight = right;

            foreach (Constraint c in GetSubConstraints())
            {
                if (c is Var)
                {
                    Var v = c as Var;
                    if (!mVars.Contains(v))
                        mVars.Add(v);
                }
            }
        }

        public Vector GetLeft()
        {
            return mLeft;
        }

        public Vector GetRight()
        {
            return mRight;
        }

        public override string ToString()
        {
            return mLeft.ToString() + "->" + mRight.ToString();
        }

        public override IEnumerable<Constraint> GetSubConstraints()
        {
            foreach (Constraint c in mLeft)
                yield return c;
            foreach (Constraint c in mRight)
                yield return c;
        }

        public List<Var> GetVars()
        {
            return mVars;
        }
    }

    public class Vector : Constraint
    {
        List<Constraint> mList;

        public Vector(IEnumerable<Constraint> list)
        {
            mList = new List<Constraint>(list);
        }

        public Vector(List<Constraint> list)
        {
            mList = list;
        }

        public Vector()
        {
            mList = new List<Constraint>();
        }

        public Constraint GetFirst()
        {
            return mList[0];
        }

        public Vector GetRest()
        {
            return new Vector(mList.GetRange(1, mList.Count - 1));
        }
        
        public override string ToString()
        {
            string ret = "(";
            for (int i = 0; i < mList.Count; ++i)
            {
                if (i > 0) ret += ",";
                ret += mList[i].ToString();
            }
            ret += ")";
            return ret;
        }

        public List<Constraint> GetList()
        {
            return mList;
        }

        public bool IsEmpty()
        {
            return mList.Count == 0;
        }

        public int GetCount()
        {
            return mList.Count;
        }

        public override IEnumerable<Constraint> GetSubConstraints()
        {
            foreach (Constraint c in mList)
                foreach (Constraint d in c.GetSubConstraints())
                    yield return d;
        }

        public int GetSubConstraintCount()
        {
            int ret = 0;
            foreach (Constraint c in GetSubConstraints())
                ++ret;
            return ret;
        }

        public void Add(Constraint c)
        {
            mList.Add(c);
        }

        public IEnumerator<Constraint> GetEnumerator()
        {
            return mList.GetEnumerator();
        }
    }

    public class ConstraintList : List<Constraint>
    {
        Constraint mUnifier;

        public ConstraintList(IEnumerable<Constraint> a)
            : base(a)
        { }

        public ConstraintList()
            : base()
        { }

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < Count; ++i)
            {
                if (i > 0) ret += " = ";
                ret += this[i].ToString();
            }
            if (mUnifier != null)
                ret += "; unifier = " + mUnifier.ToString();
            return ret;
        }

        public Vector ChooseBetterVectorUnifier(Vector v1, Vector v2)
        {
            if (v1.GetCount() > v2.GetCount())
            {
                return v1;
            }
            else if (v1.GetCount() < v2.GetCount())
            {
                return v2;
            }
            else if (v1.GetSubConstraintCount() >= v2.GetSubConstraintCount())
            {
                return v1;
            }
            else
            {
                return v2;
            }
        }

        public Constraint ChooseBetterUnifier(Constraint c1, Constraint c2)
        {
            Trace.Assert(c1 != null);
            Trace.Assert(c2 != null);
            Trace.Assert(c1 != c2);

            if (c1 is Var)
            {
                return c2;
            }
            else if (c2 is Var)
            {
                return c1;
            }
            else if (c1 is Constant)
            {
                return c1;
            }
            else if (c2 is Constant)
            {
                return c2;
            }
            else if ((c1 is Vector) && (c2 is Vector))
            {
                return ChooseBetterVectorUnifier(c1 as Vector, c2 as Vector);
            }
            else
            {
                return c1;
            }
        }

        public void ComputeUnifier()
        {
            if (Count == 0)
                throw new Exception("Can not compute unifier for an empty list");
            mUnifier = this[0];
            for (int i = 1; i < Count; ++i)
                mUnifier = ChooseBetterUnifier(mUnifier, this[i]);
        }

        public Constraint GetUnifier()
        {
            if (mUnifier == null)
                throw new Exception("Unifier hasn't been computed yet");
            return mUnifier;
        }

        public bool ContainsVar(string s)
        {
            foreach (Constraint c in this)
                if (c.EqualsVar(s))
                    return true;
            return false;
        }
    }

    public class ConstraintSolver
    {
        List<ConstraintList> mConstraintList; // left null, because it is created upon request
        Dictionary<string, ConstraintList> mLookup = new Dictionary<string, ConstraintList>();
        List<Pair<Vector>> mConstraintQueue = new List<Pair<Vector>>();
        Dictionary<string, Var> mVarPool = new Dictionary<string, Var>();
        int mnId = 0;

        public Var CreateUniqueVar(string s)
        {
            return CreateVar(s + "$" + mnId++.ToString());
        }

        public Var CreateVar(string s)
        {
            Trace.Assert(s.Length > 0);
            if (!mVarPool.ContainsKey(s))
            {
                Var v = char.IsUpper(s[0]) 
                    ? new VectorVar(s) as Var 
                    : new ScalarVar(s) as Var;
                mVarPool.Add(s, v);
                return v;
            }
            else
            {
                return mVarPool[s];
            }
        }

        ConstraintList GetConstraints(string s)
        {
            if (!mLookup.ContainsKey(s))
            {
                ConstraintList a = new ConstraintList();                
                a.Add(CreateVar(s));
                mLookup.Add(s, a);
            }

            Trace.Assert(mLookup[s].ContainsVar(s));
            return mLookup[s];
        }

        public static T Last<T>(List<T> a)
        {
            return a[a.Count - 1];
        }

        public static Pair<Vector> VecPair(Vector v1, Vector v2)
        {
            return new Pair<Vector>(v1, v2);
        }

        public void AddToConstraintQueue(Vector v1, Vector v2)
        {
            // Don't add redundnant things to the constraint queue
            foreach (Pair<Vector> pair in mConstraintQueue)
                if ((pair.First == v1) && (pair.Second == v2))
                    return;

            Log("adding to constraint queue: " + v1.ToString() + " = " + v2.ToString());
            mConstraintQueue.Add(new Pair<Vector>(v1, v2));
        }

        public void AddSubConstraints(Constraint c1, Constraint c2)
        {
            if (!(c1 is Vector)) return;
            if (!(c2 is Vector)) return;
            if (c1 == c2) return;
            AddToConstraintQueue(c1 as Vector, c2 as Vector);
        }

        public void AddVarConstraint(Var v, Constraint c)
        {
            AddConstraintToList(c, GetConstraints(v.ToString()));
        }

        public void AddVecConstraint(Vector v1, Vector v2)
        {
            if (v1 == v2) 
                return;

            if (v1.IsEmpty() && v2.IsEmpty())
                return;

            if (v1.IsEmpty() || v2.IsEmpty())
                Err("Incompatible length vectors");

            Log("Constraining vector: " + v1.ToString() + " = " + v2.ToString());

            if (v1.GetFirst() is VectorVar)
            {
                Check(v1.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                if (v2.GetFirst() is VectorVar)
                {
                    Check(v2.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                    ConstrainVars(v1.GetFirst() as Var, v2.GetFirst() as Var);
                }
                else
                {
                    AddVarConstraint(v1.GetFirst() as VectorVar, v2);
                }
            }
            else if (v2.GetFirst() is VectorVar)
            {
                Trace.Assert(!(v1.GetFirst() is VectorVar));
                Check(v2.GetCount() == 1, "Vector variables can only occur in the last position of a vector");
                AddVarConstraint(v2.GetFirst() as VectorVar, v1);
            }
            else 
            {
                AddConstraints(v1.GetFirst(), v2.GetFirst());

                // Recursive tail call
                AddVecConstraint(v1.GetRest(), v2.GetRest());
            }
        }

        public void AddConstraintToList(Constraint c, ConstraintList a)
        {
            if (a.Contains(c))
                return;
            if (c is Var)
            {
                // Update the constraint list associated with this particular variable
                // to now be "a". 
                string sVar = c.ToString();
                Trace.Assert(mLookup.ContainsKey(sVar));
                ConstraintList list = mLookup[sVar];
                if (list == a)             
                    Err("Internal error, expected constraint list to contain constraint " + c.ToString());
                mLookup[sVar] = a;
            }
            foreach (Constraint k in a)
                AddSubConstraints(k, c);
            a.Add(c);
        }

        public void ConstrainVars(Var v1, Var v2)
        {
            Check(
                ((v1 is ScalarVar) && (v2 is ScalarVar)) ||
                ((v1 is VectorVar) && (v2 is VectorVar)),
                "Incompatible variable kinds " + v1.ToString() + " and " + v2.ToString());

            ConstrainVars(v1.ToString(), v2.ToString());
        }

        public void ConstrainVars(string s1, string s2)
        {
            if (s1.Equals(s2))
                return;
            ConstraintList a1 = GetConstraints(s1);
            ConstraintList a2 = GetConstraints(s2);
            if (a1 == a2)
                return;

            Trace.Assert(a1 != null);
            Trace.Assert(a2 != null);

            Log("Constraining var: " + s1 + " = " + s2);
            
            foreach (Constraint c in a2)
                AddConstraintToList(c, a1);
            
            // While we have items left in the queue to merge, we merge them
            while (mConstraintQueue.Count > 0)
            {
                Pair<Vector> p = mConstraintQueue[0];
                mConstraintQueue.RemoveAt(0);
                Log("Constraining queue item");
                AddVecConstraint(p.First, p.Second);
            }
        }

        public static void Err(string s)
        {
            throw new Exception(s);
        }

        public static void Check(bool b, string s)
        {
            if (!b)
                Err(s);
        }

        public static void Log(string s)
        {
            if (Config.gbVerboseInference)
                Output.WriteLine(s);
        }

        public void AddDeduction(Constant c, Constraint x)
        {
            // TODO: manage some kind of deduction list
            Log(c.ToString() + " = " + x.ToString());
        }

        public void AddConstraints(Constraint c1, Constraint c2)
        {
            if (c1 == c2)
                return;

            if ((c1 is Var) && (c2 is Var))
                ConstrainVars(c1 as Var, c2 as Var);
            else if (c1 is Var)
                AddVarConstraint(c1 as Var, c2);
            else if (c2 is Var)
                AddVarConstraint(c2 as Var, c1);
            else if ((c1 is Vector) && (c2 is Vector))
                AddVecConstraint(c1 as Vector, c2 as Vector);

            if (c1 is Constant)
                AddDeduction(c1 as Constant, c2);
            if (c2 is Constant)
                AddDeduction(c2 as Constant, c1);               
        }

        /// <summary>
        /// Constructs the list of unique constraint lists
        /// </summary>
        public void ComputeConstraintLists()
        {
            mConstraintList = new List<ConstraintList>();
            foreach (ConstraintList list in mLookup.Values)
                if (!mConstraintList.Contains(list))
                    mConstraintList.Add(list);
        }

        public List<ConstraintList> GetConstraintLists()
        {
            if (mConstraintList == null)
                throw new Exception("constraint lists haven't been computed");
            return mConstraintList;
        }

        public IEnumerable<string> GetConstrainedVars()
        {
            return mLookup.Keys;
        }

        public IEnumerable<string> GetAllVars()
        {
            return mVarPool.Keys;
        }

        public void ComputeUnifiers()
        {
            foreach (ConstraintList list in GetConstraintLists())
            {
                Trace.Assert(list.Count > 0);
                list.ComputeUnifier();
            }
        }

        /// <summary>
        /// This takes a unifier and replaces all variables with their unifiers.
        /// The visited parameter is used to detect recursive unifiers
        /// </summary>
        public Constraint Resolve(Constraint c, Stack<Constraint> visited)
        {
            if (visited.Contains(c))
            {
                throw new Exception("recursion not yet handled");
            }

            visited.Push(c);
            Constraint ret;
            if (c is Var)
            {
                Var v = c as Var;
                ret = GetUnifierFor(v);
                if (ret == null)
                {
                    ret = c;
                }
                else
                {
                    if (ret != c)
                        ret = Resolve(ret, visited);
                }
            }
            else if (c is Vector)
            {
                Vector vec = new Vector();
                foreach (Constraint tmp in (c as Vector))
                    vec.Add(Resolve(tmp, visited));
                ret = vec;
            }
            else if (c is Relation)
            {
                Relation rel = c as Relation;
                Vector vLeft = Resolve(rel.GetLeft(), visited) as Vector;
                Vector vRight = Resolve(rel.GetRight(), visited) as Vector;
                ret = new Relation(vLeft, vRight);
            }
            else
            {
                ret = c;
            }
            visited.Pop();
            Trace.Assert(ret != null);
            return ret;
        }

        public bool IsConstrained(string s )
        {
            return mLookup.ContainsKey(s);
        }

        public Constraint GetUnifierFor(Var v)
        {
            Constraint ret = GetUnifierFor(v.ToString());
            if (ret == null) return v;
            return ret;
        }

        public Constraint GetUnifierFor(string s)
        {
            if (!IsConstrained(s))
                return null;
            return mLookup[s].GetUnifier();            
        }

        public Constraint GetResolvedUnifierFor(string s)
        {
            Constraint ret = GetUnifierFor(s);
            if (ret == null)
                return ret;
            return Resolve(ret, new Stack<Constraint>());
        }

        public void LogConstraints()
        {
            foreach (ConstraintList list in GetConstraintLists())
                Log(list.ToString());
        }

        public Constraint RenameVars(Constraint c, Dictionary<Var, Var> vars)
        {
            if (c is Vector)
            {
                return RenameVars(c as Vector, vars);
            }
            else if (c is Relation)
            {
                Relation rel = RenameVars(c as Relation, vars);
                rel = RenameVars(rel, new Dictionary<Var, Var>());
                return rel;
            }
            else if (c is Var)
            {
                Var oldVar = c as Var;
                if (vars.ContainsKey(oldVar))
                    return vars[oldVar];
                Var newVar = CreateUniqueVar(oldVar.ToString());
                vars.Add(oldVar, newVar);
                ConstrainVars(newVar, oldVar);
                return newVar;
            }
            else
            {
                return c;
            }
        }

        public Vector RenameVars(Vector vec, Dictionary<Var, Var> vars)
        {
            Vector ret = new Vector();
            foreach (Constraint c in vec)
                ret.Add(RenameVars(c, vars));
            return ret;
        }

        public Relation RenameVars(Relation rel, Dictionary<Var, Var> vars)
        {
            Vector vLeft = RenameVars(rel.GetLeft(), vars);
            Vector vRight = RenameVars(rel.GetRight(), vars);
            return new Relation(vLeft, vRight);
        }

        public void AddTopLevelConstraints(Vector vLeft, Vector vRight)
        {
            Log("Before lambda lifting");
            Log("left  : " + vLeft.ToString());
            Log("right : " + vRight.ToString());
            
            // convert all free variables to bound variables
            vLeft = RenameVars(vLeft, new Dictionary<Var, Var>());
            vRight = RenameVars(vRight, new Dictionary<Var, Var>());

            Log("After lambda lifting");
            Log("left  : " + vLeft.ToString());
            Log("right : " + vRight.ToString());

            Log("Constraints generated from lambda lifting");
            ComputeConstraintLists();
            LogConstraints();       

            AddVecConstraint(vLeft, vRight);
        }
    }
    
    class TypeSolver : ConstraintSolver
    {
        public Constraint KindVarToConstraintVar(CatKind k)
        {
            Trace.Assert(k.IsKindVar());
            return CreateVar(k.ToString().Substring(1));
        }

        public Vector TypeVectorToConstraintVector(CatTypeVector x)
        {
            Vector vec = new Vector();
            foreach (CatKind k in x.GetKinds())
                vec.GetList().Insert(0, CatKindToConstraint(k));
            return vec;
        }

        public Relation FxnTypeToRelation(CatFxnType ft)
        {
            Vector cons = TypeVectorToConstraintVector(ft.GetCons());
            Vector prod = TypeVectorToConstraintVector(ft.GetProd());
            return new Relation(cons, prod);
        }

        public Constraint CatKindToConstraint(CatKind k)
        {
            if (k is CatTypeVector)
            {
                return TypeVectorToConstraintVector(k as CatTypeVector);
            }
            else if (k is CatSelfType)
            {
                return new Constant((k as CatSelfType).ToIdString());
            }
            else if (k is CatFxnType)
            {
                return FxnTypeToRelation(k as CatFxnType);
            }
            else if (k.IsKindVar())
            {
                return KindVarToConstraintVar(k);
            }
            else
            {
                return new Constant(k.ToIdString());
            }
        }

        public CatTypeVector CatTypeVectorFromVec(Vector vec)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (Constraint c in vec)
                ret.PushKindBottom(ConstraintToCatKind(c));
            return ret;
        }

        public CatFxnType CatFxnTypeFromRelation(Relation rel)
        {
            CatTypeVector cons = CatTypeVectorFromVec(rel.GetLeft());
            CatTypeVector prod = CatTypeVectorFromVec(rel.GetRight());

            // TODO: add the boolean as a third value in the vector.
            // it becomes a variable when unknown, and is resolved otherwise.
            return new CatFxnType(cons, prod, false);
        }

        public CatKind ConstraintToCatKind(Constraint c)
        {
            if (c is ScalarVar)
            {
                return new CatTypeVar(c.ToString());
            }
            else if (c is VectorVar)
            {
                return new CatStackVar(c.ToString());
            }
            else if (c is Vector)
            {
                return CatTypeVectorFromVec(c as Vector);
            }
            else if (c is Relation)
            {
                return CatFxnTypeFromRelation(c as Relation);
            }
            else if (c is Constant)
            {
                // TODO: deal with CatCustomKinds
                return new CatSimpleTypeKind(c.ToString());
            }
            else
            {
                throw new Exception("unhandled constraint " + c.ToString());
            }
        }

        public CatKind ReconstructKind(CatKind k)
        {
            if (k.IsKindVar())
            {
                string s = k.ToString().Substring(1);
                Constraint u = GetResolvedUnifierFor(s);
                if (u == null)
                    return k;
                return ConstraintToCatKind(u);
            }
            else if (k is CatSelfType)
            {
                return k;
            }
            else if (k is CatFxnType)
            {
                CatFxnType ft = k as CatFxnType;
                CatTypeVector cons = ReconstructKind(ft.GetCons()) as CatTypeVector;
                CatTypeVector prod = ReconstructKind(ft.GetProd()) as CatTypeVector;
                return new CatFxnType(cons, prod, ft.HasSideEffects());
            }
            else if (k is CatTypeVector)
            {
                CatTypeVector vec = k as CatTypeVector;
                CatTypeVector ret = new CatTypeVector();
                foreach (CatKind tmp in vec.GetKinds())
                    ret.Add(ReconstructKind(tmp));
                return ret;
            }
            else
            {
                return k;
            }
        }

        public CatFxnType ComposeTypes(CatFxnType left, CatFxnType right)
        {
            Log("==");
            Log("Composing : " + left.ToString());
            Log("with      : " + right.ToString());

            Log("Adding constraints");
            Vector vLeft = TypeVectorToConstraintVector(left.GetProd());
            Vector vRight = TypeVectorToConstraintVector(right.GetCons());
            AddTopLevelConstraints(vLeft, vRight);

            Log("Constraints");
            ComputeConstraintLists();
            LogConstraints();

            Log("Unifiers");
            ComputeUnifiers();
            foreach (string sVar in GetConstrainedVars())
            {
                Constraint u = GetUnifierFor(sVar);
                Constraint v = Resolve(u, new Stack<Constraint>());
                Log("var = " + sVar + ", unresolved = " + u + ", resolved = " + v);
            }

            /*
            Log("Resolved Unifiers");
            ResolveUnifiers();
            foreach (string sVar in GetConstrainedVars())
                Log(sVar + " = " + GetResolvedUnifier(sVar));
             */

            Log("Composed Type");
            CatTypeVector newCons = ReconstructKind(left.GetCons()) as CatTypeVector;
            CatTypeVector newProd = ReconstructKind(right.GetProd()) as CatTypeVector;
            CatFxnType ft = new CatFxnType(newCons, newProd, left.HasSideEffects() || right.HasSideEffects());
            Log("raw type    : " + ft.ToString());
            Log("pretty type : " + ft.ToPrettyString());
            Log("==");
            return ft;
        }
    }
}
