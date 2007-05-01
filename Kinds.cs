using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{    
    /// <summary>
    /// All CatKinds should be immutable. This avoids a lot of problems and confusion.
    /// </summary>
    class CatKind : IEqualityComparer<CatKind>
    {
        public CatFxnType mParent;

        public CatStackKind CreateStackKind(CatStackKind rest, CatKind top)
        {
            if (top is CatTypeKind)
            {
                return new CatSimpleStackKind(GetParentOrSelf(), rest, top as CatTypeKind);
            }
            else if (top is CatStackKind)
            {
                if (rest is CatEmptyStackKind)
                {
                    // A stack with nothing under it, is the same thing as the stack.
                    return top as CatStackKind;
                }
                else 
                {
                    if (top is CatEmptyStackKind)
                        return rest;
                    return new CatStackPairKind(GetParentOrSelf(), rest, top as CatStackKind);
                }
            }
            else if (top == null)
            {
                return rest;
            }
            else
            {
                throw new Exception("unhandled kind " + top.ToString());
            }
        }
        public CatStackKind CreateStackFromNode(AstStackNode node)
        {
            CatStackKind ret = new CatEmptyStackKind(GetParentOrSelf());
            foreach (AstTypeNode t in node.mTypes)
                ret = CreateStackKind(ret, CreateKindFromNode(t));
            return ret;
        }

        public CatKind CreateKindFromNode(AstTypeNode node)
        {
            if (node is AstSimpleTypeNode)
            {
                return new CatSimpleTypeKind(GetParentOrSelf(), node.ToString());
            }
            else if (node is AstTypeVarNode)
            {
                return new CatTypeVar(GetParentOrSelf(), node.ToString());
            }
            else if (node is AstStackVarNode)
            {
                return new CatStackVar(GetParentOrSelf(), node.ToString());
            }
            else if (node is AstFxnTypeNode)
            {
                return new CatFxnType(GetParentOrSelf(), node as AstFxnTypeNode);
            }
            else
            {
                throw new Exception("unrecognized kind " + node.ToString());
            }
        }

        public CatKind(CatFxnType parent)
        {
            if (parent == null)
                if (!(this is CatFxnType))
                    throw new Exception("only function types can be top-level nodes (e.g. have no parents)");
            mParent = parent;
        }

        public CatFxnType GetParentOrSelf()
        {
            if (mParent != null)
            {
                return mParent;
            }
            else
            {
                if (!(this is CatFxnType))
                    throw new Exception("only function types can be top-level nodes (e.g. have no parents)");
                return this as CatFxnType;
            }
        }

        public virtual string GetIdString()
        {
            if (mParent != null)
                return mParent.GetIdString();
            throw new Exception("internal error: GetIdString() should be implemented in CatFxnType and not call base class");
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

        #endregion
    }

    /// <summary>
    /// Base class for the different Cat types
    /// </summary>
    class CatTypeKind : CatKind
    {
        public CatTypeKind(CatFxnType parent) 
            : base(parent)
        { }
    }

    class CatSimpleTypeKind : CatTypeKind
    {
        string msName;

        public CatSimpleTypeKind(CatFxnType parent, string s)
            : base(parent)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }
    }

    class CatStackKind : CatKind
    {
        public CatStackKind(CatFxnType parent)
            : base(parent)
        { }
    }

    class CatEmptyStackKind : CatStackKind
    {
        public CatEmptyStackKind(CatFxnType parent)
            : base(parent)
        {
        }

        public CatTypeKind GetTop()
        {
            return null;
        }

        public CatStackKind GetRest()
        {
            return null;
        }

        public override string ToString()
        {
            return "";
        }
    }

    class CatSimpleStackKind : CatStackKind
    {
        public CatSimpleStackKind(CatFxnType parent, CatStackKind r, CatTypeKind t)
            : base(parent)
        {
            Trace.Assert(r != null);
            Trace.Assert(t != null);
            top = t;
            rest = r;
        }

        CatTypeKind top;
        CatStackKind rest;

        public CatTypeKind GetTop()
        {
            return top;
        }

        public CatStackKind GetRest()
        {
            return rest;
        }

        public override string ToString()
        {
            return (rest.ToString() + "." + top.ToString());
        }
    }

    /// <summary>
    /// Represents a stack with a stack on top.
    /// </summary>
    class CatStackPairKind : CatStackKind
    {
        CatStackKind top;
        CatStackKind rest;

        public CatStackPairKind(CatFxnType parent, CatStackKind pRest, CatStackKind pTop)
            : base(parent)
        {
            Trace.Assert(pTop != null);
            Trace.Assert(pRest  != null);
            top = pTop;
            rest = pRest;
        }

        public CatStackKind GetTop()
        {
            return top;
        }

        public CatStackKind GetRest()
        {
            return rest;
        }

        public override string ToString()
        {
            return GetRest().ToString() + "." + GetTop().ToString();
        }
    }

    class CatStackVar : CatStackKind
    {
        string msName;

        public CatStackVar(CatFxnType parent, string s)
            : base(parent)
        {
            msName = s;
        }

        public override string ToString()
        {
            return "$" + msName + GetIdString();
        }
    }

    class CatFxnType : CatTypeKind
    {
        static int gnId;
        int mnId; 

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
            return new CatFxnType(null, node);
        }

        public CatFxnType(CatFxnType parent)
            : base(parent)
        {
            mCons = new CatEmptyStackKind(GetParentOrSelf());
            mProd = new CatEmptyStackKind(GetParentOrSelf());
        }

        public CatFxnType(CatFxnType parent, AstFxnTypeNode node)
            : base(parent)
        {
            mCons = CreateStackFromNode(node.mCons);
            mProd = CreateStackFromNode(node.mProd);

            if (parent == null)
            {
                // For the time being I am always writing the correct type.
                //CatStackVar rho = new CatStackVar(this, "rho");
                //mCons = CreateStackKind(rho, mCons);
                //mProd = CreateStackKind(rho, mProd);

                // Only bother incrementing the id counter if there is no parent
                mnId = gnId++;
            }
        }

        public override string GetIdString()
        {
            if (mParent != null)
                return mParent.GetIdString();
            return mnId.ToString();
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
            return "(" + GetCons().ToString() + "->" + GetProd().ToString() + ")";
        }
    }

    class CatComposedFxnType : CatFxnType
    {
        public CatComposedFxnType(CatFxnType parent, CatFxnType x, CatFxnType y, Constraints c)
            : base(parent)
        {
            AddToConsumption(x.GetCons());
            AddToProduction(y.GetProd());
            c.AddStackConstraint(x.GetProd(), y.GetCons());
        }
    }

    class CatQuotedFxnType : CatFxnType
    {
        public CatQuotedFxnType(CatFxnType parent, CatFxnType f)
            : base(parent)
        {
            AddToProduction(f.GetProd());
        }
    }

    class CatTypeVar : CatTypeKind
    {
        string msName;

        public CatTypeVar(CatFxnType parent, string s)
            : base(parent)
        {
            msName = s;
        }

        public override string ToString()
        {
            return "$" + msName + GetIdString();
        }
    }

    class CatPrimitiveType : CatTypeKind
    {
        string msName;

        public CatPrimitiveType(CatFxnType parent, string s)
            : base(parent)
        {
            msName = s;
        }

        public override string ToString()
        {
            return msName;
        }
    }

    class CatParameterizedType : CatTypeKind
    {
        CatTypeKind mType;
        string msName;

        public CatParameterizedType(CatFxnType parent, string s, CatTypeKind t)
            : base(parent)
        {
            msName = s;
            mType = t;
        }

        public override string ToString()
        {
            return msName + "(" + mType.ToString() + ")";
        }
    }

    class CatAlgebraicType : CatTypeKind
    {
        public CatAlgebraicType(CatFxnType parent)
            : base(parent)
        { }

        public CatTypeKind first;
        public CatTypeKind second;
    }

    class CatUnionType : CatAlgebraicType
    {
        public CatUnionType(CatFxnType parent, CatTypeKind x, CatTypeKind y)
            : base(parent)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "union(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    class CatSumType : CatAlgebraicType
    {
        public CatSumType(CatFxnType parent, CatTypeKind x, CatTypeKind y)
            : base(parent)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "sum(" + first.ToString() + "," + second.ToString() + ")";
        }
    }

    class CatProductType : CatAlgebraicType
    {
        public CatProductType(CatFxnType parent, CatTypeKind x, CatTypeKind y)
            : base(parent)
        {
            first = x;
            second = y;
        }

        public override string ToString()
        {
            return "product(" + first.ToString() + "," + second.ToString() + ")";
        }
    }
}
