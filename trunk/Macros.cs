/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    public class CatExpr
    {
        CatExpr mRest;
        Function mFirstFxn;
        CatFxnType mFxnType;

        int mnProd = 0;
        int mnCons = 0;

        public CatExpr()
        {
            mFxnType = CatFxnType.Create("( -> )");
        }

        public CatExpr(Function f)
        {
            mRest = null;
            mFxnType = f.GetFxnType();
        }

        public CatExpr(Function f, CatExpr rest)
        {
            if (rest.GetFxnType() == null)
                mFxnType = null;
            else
                mFxnType = TypeInferer.Infer(f.GetFxnType(), rest.GetFxnType());
        }

        public CatExpr(List<Function> list)
        {
                
        }

        public int GetTotalProduction()
        {
            return mnProd;
        }

        public int GetTotalConsumption()
        {
            return mnCons;
        }

        public bool IsTypable()
        {
            return mFxnType != null;
        }

        public CatFxnType GetFxnType()
        {
            return mFxnType;
        }
    }

    class Macros
    {
        class MacroTerm
        {            
            public MacroTerm(AstNode x)
            {
            }
        }

        class Macro
        {
            // TODO: Add extra nodes to CatAst
            public Macro(AstNode x)
            {
                
            }

            public bool Match(CatExpr expr)
            {
                if (!expr.IsTypable())
                    return false;                
                
                return mMatches.Count > 0;
            }

            Dictionary<string, CatExpr> mMatches = new Dictionary<string, CatExpr>();
            List<MacroTerm> mSrcPattern;
            List<MacroTerm> mDestPattern;
        }

        public void LoadMacros(string s)
        {
            // open file
            // parse macros
        }

        public void ApplyMacros(List<Function> f)
        {
            // find macro
            // are there multiple matching macros? 
            // how do we assign values to macros?            
        }
    }
}