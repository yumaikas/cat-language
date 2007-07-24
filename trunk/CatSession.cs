using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cat
{
    /// <summary>
    /// A CatSession contains all loaded and defined functions, for saving loading and editing. 
    /// </summary>
    public class Session
    {
        static Session gpSession = new Session(Executor.Main.GetGlobalContext());

        Context mpContext;

        public Session(Context pContext)
        {
            mpContext = pContext;
        }

        public static Session GetGlobalSession()
        {
            return gpSession;
        }

        public static int CompareFxns(Function f1, Function f2)
        {
            return f1.GetName().CompareTo(f2.GetName());
        }

        public List<DefinedFunction> GetSortedFxns()
        {
            List<DefinedFunction> fxns = new List<DefinedFunction>(mpContext.GetDefinedFunctions());
            fxns.Sort(CompareFxns);
            return fxns;
        }

        public void Output(CatWriter w)
        {
            foreach (DefinedFunction f in GetSortedFxns())
                w.WriteFunction(f);
        }

        public static void SaveToFile(string sFile)
        {
            // TODO:
        }

        public void AddFunction(DefinedFunction f)
        {
            mpContext.AddFunction(f);
        }

        public void RedefineFunction(DefinedFunction f)
        {
            mpContext.RedefineFunction(f);
        }
    }
}
