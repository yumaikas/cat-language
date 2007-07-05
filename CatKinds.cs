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
        public static int gnId = 0;

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
            if (this.ToString().Equals("any")) 
                return true;
            return this.Equals(k);
        }

        public bool IsKindVar()
        {
            return (this is CatTypeVar) || (this is CatStackVar);
        }

        public static string TypeNameFromObject(Object o)
        {
            if (o is HashList) return "hash_list";
            if (o is ByteBlock) return "byte_block";
            if (o is FList) return "list";
            if (o is Boolean) return "bool";
            if (o is int) return "int";
            if (o is Double) return "double";
            if (o is string) return "string";
            if (o is Byte) return "byte";
            if (o is Primitives.Bit) return "bit";
            if (o is Function) return (o as Function).GetTypeString();
            if (o is Char) return "char";
            return "any";
        }

        public static string TypeToString(Type t)
        {
            // TODO: fix this up. I don't like where it is.
            switch (t.Name)
            {
                case ("HashList"): return "hash_list";
                case ("Int32"): return "int";
                case ("Double"): return "double";
                case ("FList"): return "list";
                case ("Object"): return "any";
                case ("Function"): return "function";
                case ("Boolean"): return "bool";
                case ("String"): return "string";
                case ("Char"): return "char";
                default: return t.Name;
            }
        }

        public static CatKind GetKindFromObject(object o)
        {
            if (o is CatObject) return (o as CatObject).GetClass();
            if (o is Function) return (o as Function).GetFxnType();
            return new CatSimpleTypeKind(TypeNameFromObject(o));
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

        public static CatTypeVar CreateUnique()
        {
            return new CatTypeVar("t$" + (gnId++).ToString());
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatTypeVar))
                return false;
            return ToString().CompareTo(k.ToString()) == 0;
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

        public void Add(CatKind k)
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

        public CatTypeVector Clone()
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in GetKinds())
                ret.Add(k);
            return ret;
        }

        public void RemoveBottom()
        {
            GetKinds().RemoveAt(0);            
        }
    }

    public class CatStackVar : CatStackKind
    {
        string msName;

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
            return new CatStackVar("R$" + (gnId++).ToString());
        }

        public override bool Equals(CatKind k)
        {
            if (!(k is CatStackVar)) return false;
            return k.ToString() == this.ToString();
        }
    }

    public class CatFxnType : CatTypeKind
    {
        CatTypeVector mProd;
        CatTypeVector mCons;
        bool mbSideEffects;

        public static CatFxnType Create(string sType)
        {
            Peg.Parser p = new Peg.Parser(sType);
            try
            {
                if (!p.Parse(CatGrammar.FxnType()))
                    throw new Exception("no additional information");
            }
            catch (Exception e)
            {
                throw new Exception(sType + " is not a valid function type ", e);
            }

            Peg.PegAstNode ast = p.GetAst();
            if (ast.GetNumChildren() != 1)
                throw new Exception("invalid number of children in abstract syntax tree");
            AstFxnTypeNode node = new AstFxnTypeNode(ast.GetChild(0));
            CatFxnType ret = new CatFxnType(node);
            
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
        }

        public CatFxnType(AstFxnTypeNode node)
        {
            mbSideEffects = node.HasSideEffects();
            mCons = new CatTypeVector(node.mCons);
            mProd = new CatTypeVector(node.mProd);
        }

        private CatTypeVector AddImplicitRhoVariables(CatTypeVector v)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in v.GetKinds())
            {
                if (k is CatFxnType)
                    ret.Add((k as CatFxnType).AddImplicitRhoVariables());
                else if (k is CatTypeVector)
                    ret.Add(AddImplicitRhoVariables(k as CatTypeVector));
                else
                    ret.Add(k);                    
            }
            return ret;
        }

        private CatTypeVector RemoveImplicitRhoVariables(CatTypeVector v)
        {
            CatTypeVector ret = new CatTypeVector();
            foreach (CatKind k in v.GetKinds())
            {
                if (k is CatFxnType)
                    ret.Add((k as CatFxnType).RemoveImplicitRhoVariables());
                else if (k is CatTypeVector)
                    ret.Add(RemoveImplicitRhoVariables(k as CatTypeVector));
                else
                    ret.Add(k);
            }
            return ret;
        }

        public virtual CatFxnType AddImplicitRhoVariables()
        {
            CatTypeVector cons = AddImplicitRhoVariables(GetCons());
            CatTypeVector prod = AddImplicitRhoVariables(GetProd());            

            if (!(cons.GetBottom() is CatStackVar))
            {
                CatStackVar rho = CatStackVar.CreateUnique();
                cons.PushKindBottom(rho);
                prod.PushKindBottom(rho);
            }
            else
            {
                // Writing ('A -> 'b) should be illegal.
                // TODO: this fails for "qsort" which is very interesting!
                // Trace.Assert(prod.GetBottom() is CatStackVar);
            }

            return new CatFxnType(cons, prod, HasSideEffects());
        }

        public virtual CatFxnType RemoveImplicitRhoVariables()
        {
            CatTypeVector cons = RemoveImplicitRhoVariables(GetCons());
            CatTypeVector prod = RemoveImplicitRhoVariables(GetProd());

            if (!(cons.GetBottom() is CatStackVar) && (cons.GetBottom().Equals(prod.GetBottom())))
            {
                CatStackVar rho = CatStackVar.CreateUnique();
                cons.GetKinds().RemoveAt(0);
                prod.GetKinds().RemoveAt(0);
            }

            return new CatFxnType(cons, prod, HasSideEffects());
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

        public static string IntToPrettyString(int n)
        {
            string s = "";
            if (n > 26) 
                s += IntToPrettyString(n / 26);
            char c = 'a';
            c += (char)(n % 26);
            s += c.ToString();
            return s;
        }

        public static string ToPrettyString(CatTypeVector vec, Dictionary<string, string> dic, bool bMLStyle)
        {
            string s = "";
            for (int i=0; i < vec.GetKinds().Count; ++i)
            {
                if (i > 0) s += " ";
                CatKind k = vec.GetKinds()[i];
                if (k.IsKindVar())
                {
                    if (!dic.ContainsKey(k.ToString()))
                    {
                        string sNew = IntToPrettyString(dic.Count);
                        if (k is CatStackVar)
                            sNew = sNew.ToUpper();
                        dic.Add(k.ToString(), sNew);
                    }

                    if (!bMLStyle)
                        s += "'";

                    s += dic[k.ToString()];                        
                }
                else if (k is CatSelfType)
                {
                    s += "self";
                }
                else if (k is CatFxnType)
                {
                    s += "(" + ToPrettyString(k as CatFxnType, dic, bMLStyle) + ")";
                }
                else if (k is CatTypeVector)
                {
                    s += ToPrettyString(k as CatFxnType, dic, bMLStyle);
                }
                else
                {
                    s += k.ToString();
                }
            }
            return s;
        }

        public virtual string ToPrettyString(bool bMLStyle)
        {
            return "(" + ToPrettyString(this, new Dictionary<string, string>(), bMLStyle) + ")";
        }

        public static string ToPrettyString(CatFxnType ft, Dictionary<string, string> dic, bool bMLStyle)
        {
            if (ft is CatSelfType)
                return "self";

            // remove rho variables
            CatTypeVector cons = new CatTypeVector(ft.GetCons());
            CatTypeVector prod = new CatTypeVector(ft.GetProd());

            if (cons.GetBottom() is CatStackVar && prod.GetBottom().Equals(cons.GetBottom()))
            {
                cons.RemoveBottom();
                prod.RemoveBottom();
            }

            string s = ToPrettyString(cons, dic, bMLStyle);                        
            if (ft.HasSideEffects())
                s += " ~> ";
            else
                s += " -> ";

            // Uses Haskell style so instead of (A -> (B -> C)) we get (A -> B -> C)
            if (bMLStyle && (prod.GetKinds().Count == 1) && (prod.GetKinds()[0] is CatFxnType))
                s += ToPrettyString(prod.GetKinds()[0] as CatFxnType, dic, bMLStyle);
            else
                s += ToPrettyString(prod, dic, bMLStyle);

            return s;
        }

        /// <summary>
        /// This is a raw equivalency check: no normalization is done. 
        /// To comparse function type normally you would use CompareFxnTypes,
        /// which in turn calls this function.
        /// </summary>
        public override bool Equals(CatKind k)
        {
            if (!(k is CatFxnType)) return false;
            CatFxnType f = k as CatFxnType;
            return (GetCons().Equals(f.GetCons()) && GetProd().Equals(f.GetProd()) 
                && HasSideEffects() == f.HasSideEffects());
        }

        /// <summary>
        /// Compares two function types, by first normalizing so that they each have 
        /// names of variables that correspond to the locations in the function
        /// </summary>
        public static bool CompareFxnTypes(CatFxnType f, CatFxnType g)
        {
            CatFxnType f2 = VarRenamer.RenameVars(f.AddImplicitRhoVariables());
            CatFxnType g2 = VarRenamer.RenameVars(g.AddImplicitRhoVariables());
            return f2.Equals(g2);
        }

        public void GetAllVars(TypeVarList vars)
        {
            if (this is CatSelfType)
                return;
            GetAllVars(GetCons(), vars);
            GetAllVars(GetProd(), vars);
        }

        public TypeVarList GetAllVars()
        {
            TypeVarList ret = new TypeVarList();
            GetAllVars(ret);
            return ret;
        }

        private void GetAllVars(CatTypeVector vec, TypeVarList vars)
        {
            foreach (CatKind k in vec.GetKinds())
            {
                if (k is CatFxnType)
                {
                    (k as CatFxnType).GetAllVars(vars);
                }
                else if (k.IsKindVar())
                {
                    if (!vars.ContainsKey(k.ToString()))
                        vars.Add(k.ToString(), k);
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
        /// In the rare case of self-apply functions, ('A -> 'B) is okay as an intermediate step.
        /// </summary>
        public void CheckIfWellTyped()
        {
            TypeVarList consVars = new TypeVarList();

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
                    if (!consVars.ContainsKey(s))
                        throw new Exception("type error: " + s + " appears in production and not in consumption");
                }
            }
            
            // TODO: Deal with functions that return ill-typed functions. 
        }

        public CatFxnType Unquote()
        {
            return TypeInferer.Infer(this, CatFxnType.GetApplyType(), true /*TEMP: Config.gbVerboseInference*/, true);
        }

        #region static data and functions

        // Used so we don't have to create apply types over and over again
        // There used to be a cache of allocated function type objects but I abandoned it 
        // because of the ambiguity of "self" types.
        static CatFxnType gApplyType = CatFxnType.Create("('A ('A -> 'B) -> 'B)");
        
        public static CatFxnType GetApplyType()
        {
            return gApplyType;
        }

        public static CatFxnType ComposeFxnTypes(CatFxnType f, CatFxnType g)
        {
            CatFxnType ft = TypeInferer.Infer(f, g, Config.gbVerboseInference, true);
            return ft;
        }
        #endregion
    }

    /// <summary>
    /// A self type is a function that pushes itself onto the stack.
    /// self : ('A -> 'A self)
    /// </summary>
    public class CatSelfType : CatFxnType
    {
        public CatSelfType()
        {
            GetProd().Add(this);
        }

        public override CatFxnType AddImplicitRhoVariables()
        {
            return this; 
        }

        public override CatFxnType RemoveImplicitRhoVariables()
        {
            return this;
        }

        public override string ToString()
        {
            return "self";
        }

        public override string ToPrettyString(bool bMLStyle)
        {
            return "self";
        }

        public override bool Equals(CatKind k)
        {
            return k is CatSelfType;
        }
    }

    public class CatQuotedFxnType : CatFxnType
    {
        public CatQuotedFxnType(CatFxnType f)
        {
            GetProd().Add(f);
        }
    }

    public class CatCustomKind : CatKind
    {
        int mnId;
        static List<CatCustomKind> gpPool = new List<CatCustomKind>();

        public CatCustomKind()
        {
            mnId = gpPool.Count;
            gpPool.Add(this);
        }

        public int GetId()
        {
            return mnId;
        }

        public override string ToString()
        {
 	        return "_" + mnId.ToString();
        }

        public static CatCustomKind GetCustomKind(int n)
        {
            return gpPool[n];
        }

        public static CatCustomKind GetCustomKind(string s)
        {
            Trace.Assert(s.Length > 1);
            Trace.Assert(s[0] == '_');
            int n = Int32.Parse(s.Substring(1));
            return GetCustomKind(n);
        }

        public static bool IsCustomKind(string s)
        {
            return (s.Length > 0) && (s[0] == '_');
        }
        
        public override bool Equals(CatKind k)
        {
            return k == this;
        }
    }

    public class CatMetaValue<T> : CatCustomKind
    {
        T mData;

        public CatMetaValue(T x)
        {
            mData = x;
        }

        public T GetData()
        {
            return mData;
        }

        public override bool Equals(CatKind k)
        {
            if (k == this)
                return true;
            if (!(k is CatMetaValue<T>))
                return false;
            CatMetaValue<T> tmp = k as CatMetaValue<T>;
            return tmp.GetData().Equals(mData);
        }
    }
}
