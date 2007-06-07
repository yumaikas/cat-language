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
    public class CatKind : IEqualityComparer<CatKind>
    {
        public static CatKind Create(AstTypeNode node)
        {
            if (node is AstSimpleTypeNode)
            {
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

        #region IEqualityComparer<CatKind> Members

        public bool Equals(CatKind x, CatKind y)
        {
            return x.ToString().Equals(y.ToString());
        }

        public int GetHashCode(CatKind obj)
        {
            return obj.ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            // TODO: double check that this is correct
            string s1 = obj.ToString();
            string s2 = this.ToString();
            if (s1.Equals("var") || s2.Equals("var"))
                return true;
            return s1.Equals(s2);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
 
        #endregion
    }

    /// <summary>
    /// Base class for the different Cat types
    /// </summary>
    public class CatTypeKind : CatKind
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
    }

    public class CatStackKind : CatKind
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

        public void AddTop(CatKind k)
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

        public void AddBottom(CatKind k)
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

            // Add implicit rho variables to the bottom of the production and consumption
            if (!(ret.GetCons().GetBottom() is CatStackVar))
            {
                CatStackVar rho = CatStackVar.CreateUnique();
                ret.GetCons().AddBottom(rho);
                ret.GetProd().AddBottom(rho);
            }
            
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
            mCons.AddTop(rho);
            mProd.AddTop(rho);
        }

        public CatFxnType(AstFxnTypeNode node)
        {
            mbSideEffects = node.HasSideEffects();
            mCons = new CatTypeVector(node.mCons);
            mProd = new CatTypeVector(node.mProd);
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

        public void AddToProduction(CatKind x)
        {
            mProd.AddTop(x);
        }

        public void AddToConsumption(CatKind x)
        {
            mCons.AddTop(x);
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
    }

    public class CatQuotedFxnType : CatFxnType
    {
        public CatQuotedFxnType(CatFxnType f)
        {            
            AddToProduction(f);
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
    }

    public class CatPrimitiveType : CatTypeKind
    {
        string msName;

        public CatPrimitiveType(string s)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }
    }

    public class CatParameterizedType : CatTypeKind
    {
        CatTypeKind mType;
        string msName;

        public CatParameterizedType(string s, CatTypeKind t)
        {
            msName = s;
            mType = t;
        }

        public override string ToString()
        {
            return msName + "(" + mType.ToString() + ")";
        }
    }

    public class CatLabeledType : CatTypeKind
    {
        CatTypeKind mType;
        string msName;

        public CatLabeledType(string s, CatTypeKind t)
        {
            msName = s;
            mType = t;
        }

        public override string ToString()
        {
            return msName + "=" + mType.ToString();
        }
    }

    #region algebraic types, not used yet      
    public class CatAlgebraicType : CatTypeKind
    {
        public CatAlgebraicType()
        { 
            throw new Exception("algebraic types are not yet supported");
        }

        public CatTypeKind first;
        public CatTypeKind second;
    }

    public class CatUnionType : CatAlgebraicType
    {
        public CatUnionType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "union(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    public class CatSumType : CatAlgebraicType
    {
        public CatSumType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "sum(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    public class CatProductType : CatAlgebraicType
    {
        public CatProductType(CatTypeKind x, CatTypeKind y)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "product(" + first.ToString() + "," + second.ToString() + ")";
        }
    }
    #endregion
}
