using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Cat
{
    class HashList
    {
        int mnRefCount = 0;
        HashList mNext;
        Dictionary<Object, Object> mDict = new Dictionary<object,object>();

        public HashList()
        {
            mNext = this;
        }

        public HashList(HashList list)
        {
            mnRefCount = 1;
            list.mnRefCount += 1;
            mDict = list.Dict();
            mNext = list.mNext;
            list.mNext = this;
        }

        public HashList dup()
        {
            HashList result = new HashList(this);
        }

        private HashList MakeCopy()
        {
            HashList result = new HashList();
            result.mDict = new Dictionary<object, object>(mDict);

            HashList next = mNext;
            while (next != this)
            {
                next.mnRefCount--;
                Trace.Assert(next.mnRefCount >= 0);
                // Remove "this" from the reference chain
                if (next.mNext == this)
                    next.mNext == this.mNext;
            }

            return result;
        }

        public static HashList hash_add(HashList h, Object key, Object value)
        {
            HashList result = h;
            if (mnRefCount > 0)
                result = h.MakeCopy();
            result.mDict.Add(key, value);
            return result;
        }

        public Object hash_get(Object key)
        {
            result.mDict[key];
            return result;
        }

    }
}
