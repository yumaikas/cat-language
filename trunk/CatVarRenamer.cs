/// Dedicated to the public domain by Christopher Diggins
/// http://creativecommons.org/licenses/publicdomain/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{

    /// <summary>
    /// The renamer assigns new names to a set of variables either from a supplied 
    /// dictionary or by generating unique names.
    /// </summary>
    public class VarRenamer
    {
        int mnId = 0;
        bool mbGenerateNames;

        Dictionary<string, CatKind> mNames;

        #region constructors

        /// <summary>
        /// When this cosntructor is used the renamer will not generate any new names
        /// and will instead simply use the dictionary given to look up kinds.
        /// </summary>
        public VarRenamer(Dictionary<string, CatKind> names)
        {
            mNames = names;
            mbGenerateNames = false;
        }

        public VarRenamer()
        {
            mNames = new Dictionary<string, CatKind>();
            mbGenerateNames = true;
        }
        #endregion

        #region static functions
        public static bool IsStackVarName(string s)
        {
            Trace.Assert(s.Length > 0);
            Trace.Assert(s[0] == '\'');
            char c = s[1];
            if (char.IsLower(c))
                return false;
            else
                return true;
        }

        public string GenerateNewName(string s)
        {
            if (IsStackVarName(s))
                return "S" + (mnId++).ToString();
            else
                return "t" + (mnId++).ToString();
        }

        public CatKind GenerateNewVar(string s)
        {
            if (IsStackVarName(s))
                return new CatStackVar(GenerateNewName(s));
            else
                return new CatTypeVar(GenerateNewName(s));
        }
        #endregion

        /// <summary>
        /// This allows unique names to continue to be generated, from previously used variable names.
        /// </summary>
        public void ResetNames()
        {
            mNames.Clear();
        }

        public static CatFxnType RenameVars(CatFxnType ft)
        {
            return (new VarRenamer()).Rename(ft);
        }

        public CatKind Rename(CatKind k)
        {
            if (k is CatFxnType)
                return Rename(k as CatFxnType);
            else if (k is CatTypeKind)
                return Rename(k as CatTypeKind);
            else if (k is CatStackVar)
                return Rename(k as CatStackVar);
            else if (k is CatTypeVector)
                return Rename(k as CatTypeVector);
            else
                throw new Exception(k.ToString() + " is an unrecognized kind");
        }

        public CatFxnType Rename(CatFxnType f)
        {
            if (f == null)
                throw new Exception("Invalid null parameter to rename function");
            if (f is CatSelfType)
                return f;
            return new CatFxnType(Rename(f.GetCons()), Rename(f.GetProd()), f.HasSideEffects());
        }

        public CatTypeVector Rename(CatTypeVector s)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in s.GetKinds())
                ret.PushKind(Rename(k));
            return ret;
        }

        public CatStackKind Rename(CatStackVar s)
        {
            string sName = s.ToString();
            if (mNames.ContainsKey(sName))
            {
                CatKind tmp = mNames[sName];
                if (!(tmp is CatStackKind))
                {
                    if (tmp is CatSelfType)
                    {
                        CatTypeVector v = new CatTypeVector();
                        v.PushKind(tmp);
                        tmp = v;
                    }
                    else
                    {
                        throw new Exception(sName + " is not a stack kind");
                    }
                }
                return tmp as CatStackKind;
            }

            if (!mbGenerateNames)
                return s;

            CatStackVar var = new CatStackVar(GenerateNewName(sName));
            mNames.Add(sName, var);
            return var;
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
    }
}