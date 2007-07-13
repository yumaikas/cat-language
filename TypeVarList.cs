using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    public class TypeVarList : Dictionary<string, CatKind> 
    {
        public TypeVarList()
            : base()
        { }

        public TypeVarList(TypeVarList list)
            : base(list)
        { }

        public void Add(CatKind k)
        {
            if (ContainsKey(k.ToString()))
                return;
            Trace.Assert(k.IsKindVar());
            base.Add(k.ToString(), k);
        }
    }
}
