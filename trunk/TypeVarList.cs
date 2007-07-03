using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
