using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    class HashList
    {
        HashList mNext;
        Dictionary<Object, Object> mDict = new Dictionary<object,object>();

        public HashList()
        {
            mNext = this;
        }

        public static HashList hash_list()
        {
            return new HashList();
        }

        private HashList(HashList list)
        {
            mDict = list.mDict;
            mNext = list.mNext;
            list.mNext = this;
        }

        public HashList dup()
        {
            return new HashList(this);
        }

        private HashList MakeCopy()
        {
            HashList result = new HashList();
            result.mDict = new Dictionary<object, object>(mDict);

            HashList next = mNext;
            while (next != this)
            {
                // Remove "this" from the reference chain
                if (next.mNext == this)
                    next.mNext = this.mNext;
            }

            return result;
        }

        public static HashList hash_add(HashList h, Object key, Object value)
        {
            HashList result = h;
            if (result.mNext != result)
                result = h.MakeCopy();
            result.mDict.Add(key, value);
            return result;
        }

        public static HashList hash_set(HashList h, Object key, Object value)
        {
            HashList result = h;
            if (result.mNext != result)
                result = h.MakeCopy();
            result.mDict[key] = value;
            return result;
        }

        public Object hash_get(Object key)
        {
            Object result = mDict[key];
            return result;
        }

        public bool hash_contains(Object key)
        {
            return mDict.ContainsKey(key);
        }

        public static CatList hash_to_list(HashList h)
        {
            CatStack stk = new CatStack();
            foreach (KeyValuePair<Object, Object> pair in h.mDict)
            {
                CatList tmp = CatList.pair(pair.Value, pair.Key);
                stk.Push(tmp);
            }
            CatList result = new ListFromStack(stk);
            return result;
        }
    }
}
