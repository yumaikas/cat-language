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
        }

        public CatExpr(List<Function> fxns)
        {
            mpFxns = fxns;
        }

        public CatFxnType GetFxnType()
        {
            if (mpFxnType == null) 
                mpFxnType = TypeInferer.Infer(mpFxns, true);
            return mpFxnType;
        }

        public List<Function> GetFxns()
        {
            return mpFxns;
        }
        
        public bool IsSimpleNullaryFunction()
        {
            if (mpFxns.Count == 0)
                return false;
            return HasSingleProduction() && HasNoConsumption();
        }

        public bool HasSingleProduction()
        {
            return GetFxnType().GetMaxProduction() == 1;
        }

        public bool HasNoConsumption()
        {
            return GetFxnType().GetMaxConsumption() == 0;
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

            public bool DoesTokenMatch(AstMacroTerm tkn, CatExpr x, out bool bRecoverable)
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
                else if (tkn is AstMacroQuote)
                {
                    AstMacroQuote macroQuote = tkn as AstMacroQuote;
                    if ((macroQuote.mTerms.Count != 1) || (! (macroQuote.mTerms[0] is AstMacroStackVar)))
                        throw new Exception("currently only quotations containing a single stack variable are supported as macro patterns");
                    AstMacroStackVar v = macroQuote.mTerms[0] as AstMacroStackVar;

                    // Currently we only match literal quotations 
                    if (x.GetFxns().Count != 1)
                        return false;
                    Quotation quote = x.GetFxns()[0] as Quotation;
                    if (quote == null)
                        return false; 

                    // Capture the quotation. 
                    mCapturedVars.Add(v.ToString(), new CatExpr(quote.GetChildren()));
                    return true;
                }
                else if (tkn is AstMacroStackVar)
                {
                    throw new Exception("illegal location for a stack variable " + tkn.ToString());
                }
                else
                {
                    throw new Exception("unrecognized macro term " + tkn.ToString());
                }

                return false;
            }

            public void Replace(List<Function> fxns, List<AstMacroTerm> pattern)
            {
                if (mnFxnIndex < fxns.Count)
                    fxns.RemoveRange(mnFxnIndex, mnFxnCount);

                for (int i = pattern.Count - 1; i >= 0; --i)
                {
                    AstMacroTerm t = pattern[i];
                    if (t is AstMacroTypeVar)
                    {
                        string s = t.ToString();
                        if (!mCapturedVars.ContainsKey(s))
                            throw new Exception("macro variable " + s + " was not captured");
                        CatExpr expr = mCapturedVars[s];
                        for (int j = expr.GetFxns().Count - 1; j >= 0; --j)
                            fxns.Insert(mnFxnIndex, expr.GetFxns()[j]);
                    }
                    else if (t is AstMacroStackVar)
                    {
                        string s = t.ToString();
                        if (!mCapturedVars.ContainsKey(s))
                            throw new Exception("macro variable " + s + " was not captured");
                        CatExpr expr = mCapturedVars[s];
                        fxns.InsertRange(mnFxnIndex, expr.GetFxns());
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
                    else if (t is AstMacroQuote)
                    {
                        AstMacroQuote macroQuote = t as AstMacroQuote;
                        List<Function> localFxns = new List<Function>();

                        List<AstMacroTerm> localPattern = macroQuote.mTerms;
                        Replace(localFxns, localPattern);
                        Quotation q = new Quotation(localFxns);

                        fxns.Insert(mnFxnIndex, q);
                    }
                    else
                    {
                        throw new Exception("unrecognized macro term " + t.ToString());
                    }
                }
            }

            static public MacroMatch Create(AstMacroNode m, List<Function> fxns, int nPrevMatchPos, int nFxnIndex, int nSubExprSize)
            {
                if (nFxnIndex < 0) 
                    return null;
                if (nFxnIndex >= fxns.Count) 
                    return null;

                List<AstMacroTerm> pattern = m.mSrc.mPattern;

                MacroMatch match = new MacroMatch(m);
                               
                int nFirst = nFxnIndex;
                int nLast = nFxnIndex;
                                
                int nTokenIndex = pattern.Count - 1;
                               
                // Start at the end of the pattern and move backwards comparing expressions
                while (nFirst > nPrevMatchPos)
                {
                    Trace.Assert(nTokenIndex <= pattern.Count);
                    Trace.Assert(nFirst >= 0);
                    Trace.Assert(nLast >= nFirst);
                    Trace.Assert(nTokenIndex < pattern.Count);

                    // get the current sub-expression that we are evaluating 
                    CatExpr expr = new CatExpr(fxns, nFirst, nLast);
                    
                    AstMacroTerm tkn = pattern[nTokenIndex];

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
            int nLastMatchPos = -1;
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
                List<AstMacroTerm> pattern = m.mMacro.mDest.mPattern;
                m.Replace(fxns, pattern);      
            }
        }
    }
}