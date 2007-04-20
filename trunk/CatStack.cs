/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections;

namespace Cat
{
    public interface ITypeArray
    {
        int Count { get; }
        Type GetType(int n);
    }

    /// <summary>
    /// Used in the executor and to implement base list type
    /// </summary>
    public class CatStack : ArrayList, ITypeArray
    {
        public ArrayList GetBase()
        {
            return this;
        }
        public Object Peek()
        {
            return this[0];
        }
        public new Object this[int index] 
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new Exception("stack indexing error");
                return GetBase()[(Count - 1) - index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new Exception("stack indexing error");
                GetBase()[(Count - 1) - index] = value;
            }
        }
        public Object Push(Object x)
        {
            Add(x);
            return x;
        }
        public Object PushFront(Object x)
        {
            Insert(0, x);
            return x;
        }
        public Object PushTo(int n, Object x)
        {
            Insert(Count - 1 - n, x);
            return x;
        }
        public Object Pop()
        {
            Object x = Peek();
            RemoveAt(Count - 1);
            return x;
        }
        public Object PopFrom(int n)
        {
            Object x = this[Count - 1 - n];
            RemoveAt(Count - 1 - n);
            return x;
        }
        public Object PopFront()
        {
            Object x = this[0];
            RemoveAt(0);
            return x;
        }
        public bool IsEmpty()
        {
            return Count == 0;
        }

        /// <summary>
        /// Used for debugging and error reporting. 
        /// </summary>
        /// <returns></returns>
        public string GetTopTypesAsString()
        {
            string ret = "";
            int n = Math.Min(3, Count);
            for (int i = 0; i < n; ++i)
            {
                ret = GetType(i).ToString() + " " + ret;
            }
            if (n < Count)
                ret = "... " + ret;
            return ret;
        }

        #region ITypeArray Members

        public Type GetType(int n)
        {
            return this[n].GetType();
        }

        #endregion

        public CForEach ToList()
        {
            object[] a = new object[Count];
            for (int i = 0; i < Count; ++i)
            {
                a[Count - i - 1] = this[i];
            }
            return new CArray(a);
        }
    }
}
