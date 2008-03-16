using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    public class CatList : List<Object>
    {
        public CatList()
        { }

        public CatList(IEnumerable<Object> x)
            : base(x)
        { }

        public CatList(IEnumerable x)
            : base()
        {
            foreach (Object o in x)
                Add(o);
        }

        public static CatList MakeUnit(Object x)
        {
            CatList result = new CatList();
            result.Add(x);
            return result;
        }

        public static CatList MakePair(Object x, Object y)
        {
            CatList result = new CatList();
            result.Add(x);
            result.Add(y);
            return result;
        }

        public CatList(string x)
            : base(x.Length)
        {
            char[] a = x.ToCharArray();
            foreach (char c in a)
                Add(c);
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        public Type[] GetTypeArray()
        {
            Type[] result = new Type[Count];
            for (int i = 0; i < Count; ++i)
                result[i] = this[i].GetType();
            return result;
        }

        public CatList Clone()
        {
            return new CatList(this);
        }

        public string ToShortString()
        {
            // TODO: rename to PrettyString, and fix other things.
            return "(...)";
        }
    }
}
