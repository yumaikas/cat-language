/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{    
    /// <summary>
    /// All CatKinds should be immutable. This avoids a lot of problems and confusion.
    /// </summary>
    abstract public class CatKind 
    {
        public static CatKind Create(AstTypeNode node)
        {
            if (node is AstSimpleTypeNode)
            {
                if (node.ToString().Equals("self"))
                    return new CatSelfType();
                else
                    return new CatSimpleTypeKind(node.ToString());
            }
            else if (node is AstTypeVarNode)
            {
                return new CatTypeVar(node.ToString());
            }
            else if (node is AstStackVarNode)
            {
                return new CatStackVar(node.ToString());
            }
            else if (node is AstFxnTypeNode)
            {
                return new CatFxnType(node as AstFxnTypeNode);
            }
            else
            {
                throw new Exception("unrecognized kind " + node.ToString());
            }
        }

        public CatKind()
        {
        }

        public override string ToString()
        {
            throw new Exception("ToString must be overridden");
        }

        public abstract bool Equals(CatKind k);

        public bool IsSubtypeOf(CatKind k)
        {
            if (this.ToString().Equals("var")) 
                return true;
            return this.Equals(k);
        }

        public bool IsKindVar()
        {
            return (this is CatTypeVar) || (this is CatStackVar);
        }
    }

    /// <summary>
    /// Base class for the different Cat types
    /// </summary>
    public abstract class CatTypeKind : CatKind
    {
        public CatTypeKind() 
        { }
    }

    public class CatSimpleTypeKind : CatTypeKind
    {
        string msName;

        public CatSimpleTypeKind(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }

        public override bool Equals(CatKind k)
        {
            return (k is CatSimpleTypeKind) && (msName == k.ToString());
        }
    }

    public abstract class CatStackKind : CatKind
    {
    }

    public class CatTypeVector : CatStackKind
    {
        List<CatKind> mList;

        public CatTypeVector(AstStackNode node)
        {
            mList = new List<CatKind>();
            foreach (AstTypeNode tn in node.mTypes)
                mList.Add(Create(tn));
        }

        public CatTypeVector()
        {
            mList = new List<CatKind>();
        }

        public CatTypeVector(CatTypeVector k)
        {
            mList = new List<CatKind>(k.mList);
        }

        public CatTypeVector(List<CatKind> list)
        {
            mList = new List<CatKind>(list);
        }

        public List<CatKind> GetKinds()
        {
            return mList;
        }

        public void PushKind(CatKind k)
        {
            Trace.Assert(k != null);
            if (k is CatTypeVector)
            {
                mList.AddRange((k as CatTypeVector).GetKinds());
            }
            else
            {
                mList.Add(k);
            }
        }

        public void PushKindBottom(CatKind k)
        {
            Trace.Assert(k != null);
            if (k is CatTypeVector)
            {
                mList.InsertRange(0, (k as CatTypeVector).GetKinds());
            }
            else
            {
                mList.Insert(0, k);
            }
        }

        public bool IsEmpty()
        {
            return mList.Count == 0;
        }
        
        public CatKind GetBottom()
        {
            if (mList.Count > 0)
                return mList[0];
            else
                return null;
        }

        public CatKind GetTop()
        {
            if (mList.Count > 0)
                return mList[mList.Count - 1];
            else
                return null;
        }

        public CatTypeVector GetRest()
        {
            return new CatTypeVector(mList.GetRange(0, mList.Count - 1));
        }

        public override string ToString()
        {
            string ret = "";
            foreach (CatKind k in mList)
                ret += " " + k.ToString();
            if (mList.Count > 0)
                return ret.Substring(1);
            else
                return "";
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatTypeVector))
                return false;
            CatTypeVector v1 = this;
            CatTypeVector v2 = k as CatTypeVector;
            while (!v1.IsEmpty() && !v2.IsEmpty())
            {
                CatKind t1 = v1.GetTop();
                CatKind t2 = v2.GetTop();
                if (!t1.Equals(t2)) 
                    return false;
                v1 = v1.GetRest();
                v2 = v2.GetRest();                
            }
            if (!v1.IsEmpty())
                return false;
            if (!v2.IsEmpty())
                return false;
            return true;
        }
    }

    public class CatStackVar : CatStackKind
    {
        string msName;
        static int gnId = 0;

        public CatStackVar(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return "'" + msName;
        }

        public static CatStackVar CreateUnique()
        {
            return new CatStackVar("$R" + (gnId++).ToString());
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatStackVar)) return false;
            return k.ToString() == this.ToString();
        }
    }

    public class CatSelfType : CatTypeKind
    {
        public override string ToString()
        {
            return "self";
        }

        public override bool Equals(CatKind k)
        {
            return k is CatSelfType;
        }
    }

    public class CatFxnType : CatTypeKind
    {
        CatTypeVector mProd;
        CatTypeVector mCons;
        bool mbSideEffects;

        // This is for simple memoization of results from calling CreateFxnType
        static Dictionary<string, CatFxnType> gFxnTypePool = new Dictionary<string, CatFxnType>();

        public static CatFxnType Create(string sType)
        {
            if (gFxnTypePool.ContainsKey(sType))
                return gFxnTypePool[sType];

            Peg.Parser p = new Peg.Parser(sType);
            if (!p.Parse(CatGrammar.FxnType()))
                throw new Exception(sType + " is not a valid function type");
            Peg.PegAstNode ast = p.GetAst();
            if (ast.GetNumChildren() != 1)
                throw new Exception("invalid number of children in abstract syntax tree");
            AstFxnTypeNode node = new AstFxnTypeNode(ast.GetChild(0));
            CatFxnType ret = new CatFxnType(node);
            
            gFxnTypePool.Add(sType, ret);
            return ret;
        }

        public CatFxnType(CatTypeVector cons, CatTypeVector prod, bool bSideEffects)
        {
            mCons = new CatTypeVector(cons);
            mProd = new CatTypeVector(prod);
            mbSideEffects = bSideEffects;
        }

        public CatFxnType()
        {
            CatStackVar rho = CatStackVar.CreateUnique();
            mbSideEffects = false;
            mCons = new CatTypeVector();
            mProd = new CatTypeVector();
            mCons.PushKind(rho);
            mProd.PushKind(rho);
        }

        public CatFxnType(AstFxnTypeNode node)
        {
            mbSideEffects = node.HasSideEffects();
            mCons = new CatTypeVector(node.mCons);
            mProd = new CatTypeVector(node.mProd);
            AddImplicitRhoVariables();
        }

        public void AddImplicitRhoVariables()
        {
            if (!(GetCons().GetBottom() is CatStackVar))
            {
                CatStackVar rho = CatStackVar.CreateUnique();
                GetCons().PushKindBottom(rho);
                GetProd().PushKindBottom(rho);
            }
        }

        public CatFxnType Clone()
        {
            return new CatFxnType(mCons, mProd, mbSideEffects);
        }

        public bool HasSideEffects()
        {
            return mbSideEffects;
        }

        public int GetMaxProduction()
        {
            int nCnt = 0;

            List<CatKind> list = mProd.GetKinds();
            for (int i = list.Count - 1; i >= 0; --i)
            {
                CatKind k = list[i];
                if (k is CatStackVar)
                {
                    if ((i == 0) && k.Equals(mCons.GetBottom()))
                        return nCnt;
                    else
                        return -1;
                }
                nCnt++;
            }

            return nCnt;
        }

        public int GetMaxConsumption()
        {
            int nCnt = 0;

            List<CatKind> list = mCons.GetKinds();
            for (int i = list.Count - 1; i >= 0; --i)
            {
                CatKind k = list[i];
                if (k is CatStackVar)
                {
                    if ((i == 0) && k.Equals(mProd.GetBottom()))
                        return nCnt;
                    else
                        return -1;
                }
                nCnt++;
            }

            return nCnt;
        }

        public CatTypeVector GetProd()
        {
            return mProd;
        }

        public CatTypeVector GetCons()
        {
            return mCons;
        }

        public override string ToString()
        {
            if (mbSideEffects)
            {
                return "(" + GetCons().ToString() + " ~> " + GetProd().ToString() + ")";
            }
            else
            {
                return "(" + GetCons().ToString() + " -> " + GetProd().ToString() + ")";
            }
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatFxnType)) return false;
            CatFxnType f = k as CatFxnType;
            return (f.GetCons().Equals(mCons) && f.GetProd().Equals(mProd) && f.HasSideEffects() == HasSideEffects());
        }

        public static bool CompareFxnTypes(CatFxnType f, CatFxnType g)
        {
            CatFxnType f2 = VarRenamer.RenameVars(f);
            CatFxnType g2 = VarRenamer.RenameVars(g);
            return f2.Equals(g2);
        }

        private void GetAllVars(CatTypeVector vec, List<string> vars)
        {
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatFxnType)
                {
                    CatFxnType ft = k as CatFxnType;
                    GetAllVars(ft.GetCons(), vars);
                    GetAllVars(ft.GetProd(), vars);
                }
                else if (k.IsKindVar())
                {
                    vars.Add(k.ToString());
                }
            }
        }

        /// <summary>
        /// A well-typed term has no variables in the top-level of the production (i.e. nested 
        /// in functions doesn't matter) that aren't first declared somewhere (nested or not) 
        /// in the consumption. The simplest example of an ill-typed term is: ('A -> 'B) 
        /// Note that top-level self types implicitly carry all of the consumption variables so
        /// something like ('A self -> 'B) is well-typed because it expands to:
        /// ('A ('A self -> 'B) -> 'B) 
        /// </summary>
        public void CheckIfWellTyped()
        {
            List<string> consVars = new List<string>();

            foreach (CatKind k in GetCons().GetKinds())
            {
                // A self type in the consumption, implies that all production variables 
                // are embedded in the self-type (by definition of self-type)
                if (k is CatSelfType)
                    return;
            }

            GetAllVars(GetCons(), consVars);
            foreach (CatKind k in GetProd().GetKinds())
            {
                if (k.IsKindVar())
                {
                    string s = k.ToString();
                    if (!consVars.Contains(s))
                        throw new Exception("ill-typed function: " + s + " appears in production and not in consumption");
                }
            }
            
            // TODO: Deal with functions that returning ill-typed functions. 
            // There seems to be no easy way to deal with it. I don't even know if it implies
            // that the result is ill-tyyped 
        }
    }

    public class CatQuotedFxnType : CatFxnType
    {
        public CatQuotedFxnType(CatFxnType f)
        {            
            GetProd().PushKind(f);
        }
    }

    public class CatTypeVar : CatTypeKind
    {
        string msName;

        public CatTypeVar(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return "'" + msName;
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatTypeVar)) 
                return false;
            return ToString().CompareTo(k.ToString()) == 0; 
        }
    }
}
