/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class CatExpr
    {
        List<Function> mpFxns;
        CatFxnType mpFxnType;

        public CatExpr(List<Function> fxns, int nFirst, int nLast)
        {
            mpFxns = fxns.GetRange(nFirst, (nLast - nFirst) + 1);
            mpFxnType = TypeInferer.Infer(mpFxns, true);
        }

        public CatFxnType GetFxnType()
        {
            return mpFxnType;
        }

        public List<Function> GetFxns()
        {
            return mpFxns;
        }
        
        public bool HasSingleProduction()
        {
            return mpFxnType.GetMaxProduction() == 1;
        }

        public bool HasNoConsumption()
        {
            return mpFxnType.GetMaxConsumption() == 0;
        }
    }

    public class Macros
    {
        #region static functions and data 
        static Macros gMacros = new Macros();

        public static Macros GetGlobalMacros()
        {
            return gMacros;
        }
        #endregion 

        #region fields
        List<AstMacroNode> mMacros = new List<AstMacroNode>();
        #endregion

        class MacroMatch
        {
            public AstMacroNode mMacro;
            public Dictionary<string, CatExpr> mCapturedVars = new Dictionary<string, CatExpr>();
            public int mnFxnIndex = -1;
            public int mnFxnCount = 0;

            public MacroMatch(AstMacroNode m)
            {
                mMacro = m;
            }

            public bool DoesTokenMatch(AstMacroToken tkn, CatExpr x, out bool bRecoverable)
            {
                bRecoverable = true;

                if (tkn is AstMacroName)
                {
                    bRecoverable = false;
                    if (x.GetFxns().Count != 1)
                        return false;
                    Function f = x.GetFxns()[0];
                    string sName = f.GetName();
                    return sName == tkn.ToString();
                }
                else if (tkn is AstMacroTypeVar)
                {
                    if (x.GetFxnType() == null)
                    {
                        return false;
                    }

                    if (x.HasNoConsumption() && x.HasSingleProduction())
                    {
                        mCapturedVars.Add(tkn.ToString(), x);
                        return true;
                    }
                }
                else
                {
                    throw new Exception("unhandled macro tkn");
                }

                return false;
            }

            public void Replace(List<Function> fxns)
            {   
                List<AstMacroToken> pattern = mMacro.mDest.mPattern;

                fxns.RemoveRange(mnFxnIndex, mnFxnCount);

                for (int i = pattern.Count - 1; i >= 0; --i)
                {
                    AstMacroToken t = pattern[i];
                    if (t is AstMacroTypeVar)
                    {
                        string s = t.ToString();
                        if (!mCapturedVars.ContainsKey(s))
                            throw new Exception("macro variable " + s + " was not captured");
                        CatExpr expr = mCapturedVars[s];
                        for (int j = expr.GetFxns().Count - 1; j >= 0; --j)
                        {
                            fxns.Insert(mnFxnIndex, expr.GetFxns()[j]);
                        }
                    }
                    else if (t is AstMacroStackVar)
                    {
                        throw new Exception("macro stack variables are not yet supported");
                    }
                    else if (t is AstMacroName)
                    {
                        string s = t.ToString();

                        // NOTE: if we had a proper macro name class, this could be done earlier.
                        // the method for looking up functions is too convoluted. I am really 
                        // wondering about the idea of having "global scopes" embedded with 
                        // the executor.                         
                        Function f = Executor.Main.GetGlobalScope().Lookup(s);

                        fxns.Insert(mnFxnIndex, f);
                    }
                }
            }

            static public MacroMatch Create(AstMacroNode m, List<Function> fxns, int nPrevMatchPos, int nFxnIndex, int nSubExprSize)
            {
                if (nFxnIndex < 0) 
                    return null;
                if (nFxnIndex >= fxns.Count) 
                    return null;

                List<AstMacroToken> pattern = m.mSrc.mPattern;

                MacroMatch match = new MacroMatch(m);
                               
                int nFirst = nFxnIndex;
                int nLast = nFxnIndex;
                                
                int nTokenIndex = pattern.Count - 1;
                               
                // Start at the end of the pattern and move backwards comparing expressions
                while (nFirst >= nPrevMatchPos)
                {
                    Trace.Assert(nTokenIndex <= pattern.Count);
                    Trace.Assert(nFirst >= 0);
                    Trace.Assert(nLast >= nFirst);
                    Trace.Assert(nTokenIndex < pattern.Count);

                    // get the current sub-expression that we are evaluating 
                    CatExpr expr = new CatExpr(fxns, nFirst, nLast);
                    
                    AstMacroToken tkn = pattern[nTokenIndex];

                    bool bRecoverable = false;
                    if (match.DoesTokenMatch(tkn, expr, out bRecoverable))
                    {                          
                        // Check if we have matched the whole pattern 
                        if (nTokenIndex == 0)
                        {
                            match.mnFxnIndex = nFirst;
                            match.mnFxnCount = (nFxnIndex - nFirst) + 1;
                            return match;
                        }

                        // Go to the previous token
                        nTokenIndex -= 1;

                        // Adjust the sub-expression range
                        nFirst -= 1;
                        nLast = nFirst;
                    }
                    else
                    {
                        // Some matches (such as identifier names) can not be recovered from.
                        if (!bRecoverable)
                            return null;

                        // Widen the sub-expression. 
                        nFirst -= 1;

                        // Check if we have passed the limit of how big of a 
                        // sub-expression will be examined 
                        if (nLast - nFirst > nSubExprSize)
                            return null;
                    }
                }

                // The loop was finished and no match was found.
                return null;
            }
        }

        public void AddMacro(AstMacroNode node)
        {
            if (node.mSrc.mPattern.Count == 0) 
                throw new Exception("a macro has to have at least one token in the source pattern");
            mMacros.Add(node);
        }

        public void ApplyMacros(List<Function> fxns)
        {
            // This could be done multiple time
            List<MacroMatch> matches = new List<MacroMatch>();
            
            // The peephole is the maximum size of the range of functions that we will consider
            // for rewriting. This helps to reduces the overall complexity of the algorithm.
            int nPeephole = 20;

            // This is the maximum size of the sub-expression that will be considered for matching.
            int nMaxSubExpr = 5;

            // Find matches
            int nLastMatchPos = 0;
            for (int nPos = 0; nPos < fxns.Count; ++nPos)
            {
                for (int nMacro = 0; nMacro < mMacros.Count; ++nMacro)
                {
                    if (nPos - nLastMatchPos > nPeephole)
                        nLastMatchPos = nPos - nPeephole;

                    MacroMatch m = MacroMatch.Create(mMacros[nMacro], fxns, nLastMatchPos, nPos, nMaxSubExpr);
                    if (m != null)
                    {
                        nLastMatchPos = nPos;
                        matches.Add(m);
                    }
                }
            }

            // Replace matches
            for (int i = matches.Count - 1; i >= 0; --i)
            {
                MacroMatch m = matches[i];
                m.Replace(fxns);                
            }
        }
    }
}