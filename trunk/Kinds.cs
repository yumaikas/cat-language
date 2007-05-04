/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

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
        public static CatStackKind CreateStackKind(CatStackKind rest, CatKind top)
        {
            if (top is CatTypeKind)
            {
                return new CatSimpleStackKind(rest, top as CatTypeKind);
            }
            else if (top == null)
            {
                return rest;
            }
            else if (top is CatStackKind)
            {
                if ((rest == null) || (rest.IsEmpty()))
                {
                    return top as CatStackKind;
                }

                throw new Exception("stacks can not be placed on stacks");
            }
            else
            {
                throw new Exception("unrecognized kind " + top.ToString());
            }
        }
        public CatStackKind CreateStackFromNode(AstStackNode node)
        {
            CatStackKind ret = new CatEmptyStackKind();
            foreach (AstTypeNode t in node.mTypes)
                ret = CreateStackKind(ret, CreateKindFromNode(t));
            return ret;
        }

        public CatKind CreateKindFromNode(AstTypeNode node)
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
            // This should always be overridden
            return "error";
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
            // TODO: refine this so that it is more accurate.
            return obj.ToString() == this.ToString();
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

    public abstract class CatStackKind : CatKind
    {
        public CatStackKind()
        { }

        public bool IsEmpty()
        {
            return this is CatEmptyStackKind;
        }

        public abstract CatTypeKind GetTop();
        public abstract CatStackKind GetRest();
    }

    public class CatEmptyStackKind : CatStackKind
    {
        public CatEmptyStackKind()
        {
        }

        public override CatTypeKind GetTop()
        {
            throw new Exception("empty stacks have no top");
        }

        public override CatStackKind GetRest()
        {
            throw new Exception("empty stacks have no bottom");
        }

        public override string ToString()
        {
            return "";
        }
    }

    public class CatSimpleStackKind : CatStackKind
    {
        // Note: I am not sure I am happy about implementing this the way I did. 
        // It currently behaves like a cons-list cell but I think it might 
        // be more elegant to simply use a list of types.

        public CatSimpleStackKind(CatStackKind r, CatTypeKind t)
        {
            Trace.Assert(r != null);
            Trace.Assert(t != null);
            top = t;
            rest = r;
        }

        CatTypeKind top;
        CatStackKind rest;

        public override CatTypeKind GetTop()
        {
            return top;
        }

        public override CatStackKind GetRest()
        {
            return rest;
        }

        public override string ToString()
        {
            return (rest.ToString() + " " + top.ToString());
        }
    }

    public class CatStackVar : CatStackKind
    {
        string msName;

        public CatStackVar(string s)
        {
            msName = s;
        }

        public override CatTypeKind GetTop()
        {
            throw new Exception("stack variables have no top");
        }

        public override CatStackKind GetRest()
        {
            throw new Exception("stack variables have no bottom");
        }

        public override string ToString()
        {
            return msName;
        }
    }

    public class CatFxnType : CatTypeKind
    {
        CatStackKind mProd;
        CatStackKind mCons;

        public static CatFxnType CreateFxnType(string sType)
        {
            Peg.Parser p = new Peg.Parser(sType);
            if (!p.Parse(CatGrammar.FxnType()))
                throw new Exception(sType + " is not a valid function type");
            Peg.Ast ast = p.GetAst();
            if (ast.GetNumChildren() != 1)
                throw new Exception("invalid number of children in abstract syntax tree");
            AstFxnTypeNode node = new AstFxnTypeNode(ast.GetChild(0));
            return new CatFxnType(node);
        }

        public CatFxnType(CatStackKind cons, CatStackKind prod)
        {
            AddToConsumption(cons);
            AddToProduction(prod);
        }


        public CatFxnType()
        {
            mCons = new CatEmptyStackKind();
            mProd = new CatEmptyStackKind();
        }

        public CatFxnType(AstFxnTypeNode node)
        {
            mCons = CreateStackFromNode(node.mCons);
            mProd = CreateStackFromNode(node.mProd);
        }

        public void AddToProduction(CatKind x)
        {
            mProd = CreateStackKind(mProd, x);
        }

        public void AddToConsumption(CatKind x)
        {
            mCons = CreateStackKind(mCons, x);
        }

        public CatStackKind GetProd()
        {
            return mProd;
        }

        public CatStackKind GetCons()
        {
            return mCons;
        }

        public override string ToString()
        {
            return "(" + GetCons().ToString() + " -> " + GetProd().ToString() + ")";
        }
    }

    public class CatQuotedFxnType : CatFxnType
    {
        public CatQuotedFxnType(CatFxnType f)
        {
            AddToProduction(f.GetProd());
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
            return msName;
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
