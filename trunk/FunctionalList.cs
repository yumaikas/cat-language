/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;

namespace Cat
{
    #region delegate types
    public delegate void Accessor(object o);
    public delegate object FoldFxn(object x, object y);
    public delegate object MapFxn(object o);
    public delegate object RangeGenFxn(int n);
    public delegate bool FilterFxn(object o);
    #endregion

    /// <summary>
    /// This is a quasi-functional list. It implements common functional list operations. 
    /// Lists that inherit from it should be non-mutable, and can customize how they implement particular 
    /// operations. The key behavior here is that functions generate optimized types to deal with
    /// particular cases. This approach would be much easier to implement if we could assign values to 
    /// the "this" pointer. 
    /// </summary>
    public abstract class CForEach
    {
        #region delegate functions
        public static FilterFxn ComposeFilters(FilterFxn f, FilterFxn g)
        {
            return delegate(object x) { return f(x) && g(x); };
        }
        #endregion

        #region static data
        static CEmpty nil = new CEmpty();
        #endregion

        #region static functions
        public static CForEach Concat(CForEach first, CForEach second) 
        {
            if (second.IsEmpty())
                return first;
            if (first.IsEmpty())
                return second;
            return new CConcatPair(first, second); 
        }

        public static CForEach Gen(object o, MapFxn next, FilterFxn cond)
        {
            return new CGenerator(o, next, cond);
        }

        public static CForEach RangeGen(RangeGenFxn f, int first, int count)
        {
            return new CRangeGenerator(f, first, count);
        }

        public static CForEach Repeater(Object o)
        {
            return new CRepeater(o);
        }

        public static CForEach Nil()
        {
            return nil;
        }

        public static CForEach Unit(object x)
        {
            return new CUnit(x);
        }

        public static CForEach Pair(object x, object y)
        {
            return new CPair(x, y);
        }

        public static CForEach Cons(object x, CForEach list)
        {
            return new CConsCell(x, list);
        }
        #endregion

        #region abstract functions
        public abstract void ForEach(Accessor a);
        #endregion

        #region virtual functions
        public virtual bool IsEmpty()
        {
            bool ret = true;
            ForEach(delegate(object o) { ret = false; });
            return ret;
        }

        public virtual int Count() 
        {
            int result = 0;
            ForEach(delegate(object o) { result += 1; });
            return result;
        }

        public virtual object Fold(object init, FoldFxn f)
        {
            ForEach(delegate(object o) { init = f(init, o); });
            return init;
        }

        public virtual CForEach Map(MapFxn f)
        {
            return new CMappedForEach(this, f);
        }

        public virtual object Nth(int n)
        {
            Object result = null;
            ForEach(delegate(Object o) { if (n-- == 0) result = o; });
            return result;
        }

        public virtual Object First()
        {
            return Nth(0);
        }

        public virtual CForEach Rest()
        {
            return DropN(1);
        }

        public virtual Object Last()
        {
            Object result = null;
            ForEach(delegate(Object o) { result = o; });
            return result;
        }

        public virtual CForEach Filter(FilterFxn f)
        {
            return new CFilteredForEach(this, f);
        }

        public virtual CForEach DropN(int n)
        {
            int cur = 0;
            FilterFxn f = delegate(Object o) { return (cur++ < n); };
            return DropWhile(f);
        }

        public virtual CForEach TakeN(int n)
        {
            int cur = 0;
            FilterFxn f = delegate(Object o) { return (cur++ < n); };
            return TakeWhile(f);
        }

        public virtual CForEach TakeRange(int first, int count)
        {
            return DropN(first).TakeN(count);
        }

        public virtual CForEach TakeWhile(FilterFxn f)
        {
            bool b = true;
            FilterFxn g = delegate(object x)
            {
                if (b) { 
                    if (f(x))
                    {
                        return true; 
                    }
                    else 
                    { 
                        b = false; 
                        return false;
                    } 
                } 
                else 
                { 
                    return false; 
                }
            };
            return Filter(g); 
        }

        public virtual CForEach DropWhile(FilterFxn f)
        {
            bool b = false;
            FilterFxn g = delegate(object x)
            {
                if (!b)
                {
                    if (f(x))
                    {
                        return false;
                    }
                    else
                    {
                        b = true;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            };
            return Filter(g);
        }
        #endregion
    }

    public class CEmpty : CForEach
    {
        public override void ForEach(Accessor a)
        {
        }

        public override bool IsEmpty()
        {
            return true;
        }

        public override int Count()
        {
            return 0;
        }

        public override CForEach TakeN(int n)
        {
            return this;
        }

        public override CForEach DropN(int n)
        {
            return this;
        }

        public override CForEach TakeRange(int first, int count)
        {
            return this;
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            return this;
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            return this;
        }

        public override CForEach Filter(FilterFxn f)
        {
            return this;
        }

        public override CForEach Map(MapFxn f)
        {
            return this;
        }

        public override object Fold(object init, FoldFxn f)
        {
            return init;
        }

        public override object Nth(int n)
        {
            throw new Exception("empty list");
        }

        public override object Last()
        {
            throw new Exception("empty list");
        }

        public override object First()
        {
            throw new Exception("empty list");
        }

        public override CForEach Rest()
        {
            return this;
        }
    }

    public class CUnit : CForEach
    {
        object m;

        public CUnit(object x)
        {
            m = x;
        }
        
        public override void ForEach(Accessor a)
        {
            a(m);
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override int Count()
        {
            return 1;
        }

        public override object First()
        {
            return m;
        }

        public override object Last()
        {
            return m;
        }

        public override CForEach Rest()
        {
            return Nil();
        }

        public override CForEach Map(MapFxn f)
        {
            return Unit(f(m));
        }

        public override CForEach Filter(FilterFxn f)
        {
            if (f(m))
            {
                return this;
            }
            else
            {
                return Nil();
            }
        }

        public override object Fold(object init, FoldFxn f)
        {
            return f(init, m);
        }

        public override CForEach DropN(int n)
        {
            if (n == 0) return this;
            else return Nil();
        }
    
        public override CForEach TakeN(int n)
        {
            if (n == 0) return Nil();
            else return this;
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            if (f(m)) return Nil();
            else return this;
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            if (f(m)) return this;
            else return Nil();
        }

        public override object Nth(int n)
        {
            if (n != 0) throw new Exception("out of range");
            return m;
        }
    }

    public class CPair : CForEach
    {
        public object mFirst;
        public object mSecond;

        public CPair(object first, object second)
        {
            mFirst = first;
            mSecond = second;
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override object First()
        {
            return mFirst;
        }

        public override object Last()
        {
            return mSecond;
        }

        public override CForEach Rest()
        {
            return Unit(mSecond);
        }

        public override int Count()
        {
            return 2;
        }

        public override void ForEach(Accessor a)
        {
            a(mFirst);
            a(mSecond);
        }
        
        public override CForEach Map(MapFxn f)
        {
            return Pair(f(mFirst), f(mSecond));
        }

        public override CForEach Filter(FilterFxn f)
        {
            if (f(mFirst))
            {
                if (f(mSecond))
                    return this;
                else
                    return Unit(mFirst);
            }
            else
            {
                if (f(mSecond))
                    return Unit(mSecond);
                else
                    return Nil();
            }
        }

        public override object Fold(object init, FoldFxn f)
        {
            return f(f(init, mFirst), mSecond);
        }

        public override CForEach DropN(int n)
        {
            switch (n)
            {
                case (0) : 
                    return this;
                case (1) :
                    return Unit(mSecond);
                default :
                    return Nil();
            }
        }

        public override CForEach TakeN(int n)
        {
            switch (n)
            {
                case (0):
                    return Nil();
                case (1):
                    return Unit(mFirst);
                default:
                    return this;
            }
        }

        public override object Nth(int n)
        {
            switch (n)
            {
                case (0):
                    return mFirst;
                case (1):
                    return mSecond;
                default:
                    throw new Exception("out of range");
            }
        }


        public override CForEach DropWhile(FilterFxn f)
        {
            if (!f(mFirst)) return this;
            if (!f(mSecond)) return Unit(mSecond);
            return Nil();
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            if (!f(mFirst)) return Nil();
            if (!f(mSecond)) return Unit(mFirst);
            return this;
        }

    }

    public class CConcatPair : CForEach
    {
        CForEach mFirst;
        int mFirstCount;
        CForEach mSecond;

        public CConcatPair(CForEach first, CForEach second)
        {
            mFirst = first;
            // Caching the count of the first half could be expensive during construction, 
            // but it optimizes a lot of algorithms. Infinite lists are not really a problem, 
            // because concatenating an infinite list with something else is generally a bad idea.
            mFirstCount = first.Count();
            mSecond = second;
        }

        public override void ForEach(Accessor a)
        {
            mFirst.ForEach(a);
            mSecond.ForEach(a);
        }

        public override bool IsEmpty()
        {
            return mFirst.IsEmpty() && mSecond.IsEmpty();
        }

        public override int Count()
        {
            return mFirstCount + mSecond.Count();
        }

        public override object Nth(int n)
        {
            if (n < mFirstCount)
                return mFirst.Nth(n);
            else
                return mSecond.Nth(n - mFirstCount);
        }

        public override Object First()
        {
            return mFirst.First();
        }

        public override CForEach Rest()
        {
            if (mFirst.IsEmpty())
                return mSecond.Rest();
            return Concat(mFirst.Rest(), mSecond);
        }

        public override Object Last()
        {
            return mSecond.Last();
        }

        /// <summary>
        /// Overriding Filter allows concatenation to retain some of its optimizations.
        /// </summary>
        public override CForEach Filter(FilterFxn f)
        {
            return new CConcatPair(new CFilteredForEach(mFirst, f), new CFilteredForEach(mSecond, f));
        }

        /// <summary>
        /// Overriding Map allows concatenation to retain some of its optimizations.
        /// </summary>
        public override CForEach Map(MapFxn f)
        {
            return Concat(new CMappedForEach(mFirst, f), new CMappedForEach(mSecond, f));
        }

        public override CForEach DropN(int n)
        {
            if (n >= mFirstCount)
            {
                return mSecond.DropN(n - mFirstCount);
            }
            else
            {
                return Concat(mFirst.DropN(n), mSecond);
            }
        }

        public override CForEach TakeN(int n)
        {
            if (n >= mFirstCount)
            {
                return Concat(mFirst, mSecond.TakeN(mFirstCount));
            }
            else
            {
                return mFirst.TakeN(n);
            }
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            CForEach tmp = mFirst.TakeWhile(f);
            if (tmp.Count() == mFirstCount)
            {
                return Concat(mFirst, mSecond.TakeWhile(f));
            }
            else
            {
                return tmp;
            }

        }

        public override CForEach DropWhile(FilterFxn f)
        {
            CForEach tmp = mFirst.DropWhile(f);
            if (tmp.IsEmpty())
            {
                return mSecond.DropWhile(f);
            }
            else
            {
                return Concat(tmp, mSecond);
            }
        }
    }

    public class CArray : CForEach
    {
        object[] m;
        
        public CArray(CForEach list, int n)
        {
            m = new object[n];
            int i = 0;
            list.ForEach(delegate(Object x) { if (i < n) m[i++] = x; });
        }

        public CArray(CForEach list)
            : this(list, list.Count())
        {
        }

        public CArray(object[] x)
        {
            m = x;
        }

        public override void ForEach(Accessor a)
        {
            foreach (object o in m)
                a(o);
        }

        public override bool IsEmpty()
        {
            return Count() > 0;
        }

        public override int Count()
        {
            return m.Length;
        }

        public override object Nth(int n)
        {
            return m[n];
        }

        public override Object First()
        {
            return m[0];
        }

        public override Object Last()
        {
            return m[m.Length - 1];
        }

        public override CForEach DropN(int n)
        {
            return TakeRange(n, Count() - n);
        }

        public override CForEach TakeN(int n)
        {
            return TakeRange(0, n);
        }

        public override CForEach TakeRange(int first, int count)
        {
            if ((first == 0) && (count == Count())) 
                return this;

            if (first + count > Count())
                count = Count() - first;

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return Unit(Nth(first));
                case (2):
                    return Pair(Nth(first), Nth(first + 1));
                default: 
                    return new CRangedArray(m, first, count);
            }
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            int i = 0;
            while (i < m.Length)
                if (!f(m[i++]))
                    break;
            if (i == m.Length) return Nil();
            if (i == 1) return this;
            return new CRangedArray(m, i, Count() - i);
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            int i = 0;
            while (i < m.Length)
                if (!f(m[i++]))
                    break;
            if (i == m.Length) return this;
            if (i == 1) return Nil();
            return new CRangedArray(m, 0, i);
        }
    }

    public class CRangedArray : CForEach
    {
        object[] mArray;
        int mFirst;
        int mCount; 

        public CRangedArray(object[] a, int first, int count)
        {
            if (count < 2) 
                throw new Exception("Invalid range, count must be more than two (otherwise use pair, unit, or nil)");             
            if (first < 0)
                throw new Exception("Invalid range, first index must be non-negative");
            if (first + count > a.Length)
                count = a.Length - first;
            mArray = a;
            mFirst = first;
            mCount = count;
        }

        public override void ForEach(Accessor a)
        {
            for (int i = mFirst; i < mFirst + mCount; ++i)
            {
                a(mArray[i]);
            }
        }

        public override bool IsEmpty()
        {
            return Count() > 0;
        }

        public override int Count()
        {
            return mCount;
        }

        public override object Nth(int n)
        {
            return mArray[n + mFirst];
        }

        public override Object First()
        {
            return mArray[mFirst];
        }

        public override Object Last()
        {
            return mArray[mFirst + mCount - 1];
        }

        public override CForEach DropN(int n)
        {
            return TakeRange(n, Count() - n);
        }

        public override CForEach TakeN(int n)
        {
            return TakeRange(0, n);
        }

        public override CForEach TakeRange(int first, int count)
        {
            if (first + count > mCount)
                throw new Exception("Invalid range");

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return Unit(Nth(first + mFirst));
                case (2):
                    return Pair(Nth(first + mFirst), Nth(first + mFirst + 1));
                default:
                    return new CRangedArray(mArray, mFirst, count);
            }
        }
    }

    public class CMappedForEach : CForEach
    {
        MapFxn mMap;
        CForEach mList;

        public CMappedForEach(CForEach list, MapFxn map)
        {
            mList = list;
            mMap = map;
        }

        public override void ForEach(Accessor a)
        {
            mList.ForEach(delegate(object x) { a(mMap(x)); });
        }

        public override bool IsEmpty()
        {
            return mList.IsEmpty();
        }

        public override int Count()
        {
            return mList.Count();
        }

        public override object First()
        {
            return mMap(mList.First());
        }

        public override object Last()
        {
            return mMap(mList.Last());
        }

        public override object Nth(int n)
        {
            return mMap(mList.Nth(n));
        }

        public override CForEach DropN(int n)
        {
            return new CMappedForEach(mList.DropN(n), mMap);
        }

        public override CForEach TakeN(int n)
        {
            return new CMappedForEach(mList.TakeN(n), mMap);
        }

        public override CForEach TakeRange(int first, int count)
        {
            return new CMappedForEach(mList.TakeRange(first, count), mMap);
        }
    }

    public class CFilteredForEach : CForEach
    {
        FilterFxn mFilter;
        CForEach mList;

        public CFilteredForEach(CForEach list, FilterFxn filter)
        {
            mList = list;
            mFilter = filter;
        }

        public override void ForEach(Accessor a)
        {
            mList.ForEach(delegate(object x) { if (mFilter(x)) a(x); });
        }
    }

    public class CConsCell : CForEach
    {
        object mHead;
        CForEach mRest;

        public CConsCell(object head, CForEach rest)
        {
            mHead = head;
            mRest = rest;
        }

        public override void ForEach(Accessor a)
        {
            a(mHead);
            mRest.ForEach(a);
        }

        public override bool IsEmpty()
        {
            return false;
        }

        public override int Count()
        {
            return 1 + mRest.Count();
        }

        public override object First()
        {
            return mHead;
        }

        public override object Nth(int n)
        {
            if (n == 0) 
                return mHead;
            else
                return mRest.Nth(n - 1);
        }

        public override CForEach DropN(int n)
        {
            if (n == 0)
                return this;
            else
                return mRest.DropN(n - 1);
        }
    }

    public class CRepeater : CForEach
    {
        object mObject;

        public CRepeater(Object o)
        {
            mObject = o;
        }

        public override void ForEach(Accessor a)
        {
            while (true) {
                a(mObject);
            }
        }

        #region override functions
        public override bool IsEmpty()
        {
            return false;
        }

        public override object Nth(int n)
        {
            return mObject;
        }

        public override Object First()
        {
            return mObject;
        }

        public override CForEach Rest()
        {
            return this;
        }

        public override Object Last()
        {
            return mObject;
        }

        public override CForEach Filter(FilterFxn f)        
        {
            if (f(mObject))
                return this;
            else 
                return Nil();
        }

        public override int Count()
        {
            throw new Exception("infinite list");
        }

        public override object Fold(object init, FoldFxn f)
        {
            throw new Exception("infinite list");
        }

        public override CForEach Map(MapFxn f)
        {
            return new CRepeater(f(mObject));
        }

        public override CForEach DropN(int n)
        {
            return this;
        }

        public override CForEach TakeN(int n)
        {
            return RangeGen(delegate(int i) { return mObject; }, 0, n);
        }

        public override CForEach TakeRange(int first, int count)
        {
            return TakeN(count);
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            return Gen(mObject, delegate(Object x) { return mObject; }, f);
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            return this;
        }

        #endregion
    }

    public class CRangeGenerator : CForEach
    {
        RangeGenFxn mFxn;
        int mFirst;
        int mCount;

        public CRangeGenerator(RangeGenFxn f, int first, int count)
        {
            mFxn = f;
            mFirst = first;
            mCount = count;
        }

        public override void ForEach(Accessor a)
        {
            for (int i = mFirst; i < mCount; ++i)
            {
                a(mFxn(i));
            }
        }

        #region override functions
        public override bool IsEmpty()
        {
            return mCount == 0;
        }

        public override int Count()
        {
            return mCount;
        }

        public override object Nth(int n)
        {
            return mFxn(n + mFirst);
        }

        public override Object First()
        {
            return mFxn(mFirst);
        }

        public override CForEach Rest()
        {
            return DropN(1);
        }

        public override Object Last()
        {
            return mFxn(mFirst + mCount - 1);
        }

        public override CForEach DropN(int n)
        {
            return TakeRange(mFirst + n, mCount - n);
        }

        public override CForEach TakeN(int n)
        {
            return TakeRange(mFirst, n);
        }

        public override CForEach TakeRange(int first, int count)
        {
            if (count + first > mCount)
                count = mCount - first;

            switch (count)
            {
                case (0):
                    return Nil();
                case (1):
                    return Unit(mFxn(mFirst + first));
                case (2):
                    return Pair(mFxn(mFirst + first), mFxn(mFirst + first + 1));
                default:
                    return new CRangeGenerator(mFxn, mFirst, count);
            }
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            int cur = mFirst;
            while (cur < mFirst + mCount)
            {
                if (!f(mFxn(cur)))
                {
                    int n = cur - mFirst;
                    if (n == 0) return Nil();
                    return new CRangeGenerator(mFxn, mFirst, n);                   
                }
                cur++;
            }
            return this;
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            int cur = mFirst;
            while (cur < mFirst + mCount)
            {
                if (!f(mFxn(cur)))
                {
                    int n = mCount - (cur - mFirst);
                    if (cur == mFirst) return this;
                    return new CRangeGenerator(mFxn, cur, n);
                }
                cur++;
            }
            return Nil();
        }
        #endregion
    }

    public class CGenerator : CForEach
    {
        object mFirst;
        MapFxn mNext;
        FilterFxn mCond;

        public CGenerator(object first, MapFxn next, FilterFxn cond)
        {
            mFirst = first;
            mNext = next;
            mCond = cond;
        }

        public override object First()
        {
            return mFirst;
        }

        public override CForEach Rest()
        {
            return Gen(mNext(mFirst), mNext, mCond);
        }

        public override void ForEach(Accessor a)
        {
            object cur = mFirst;
            while (mCond(cur))
            {
                a(cur);
                cur = mNext(cur);
            }
        }

        public override bool IsEmpty()
        {
            return !mCond(mFirst);
        }

        public override CForEach TakeWhile(FilterFxn f)
        {
            if (!mCond(mFirst) || !f(mFirst)) return Nil();
            return Gen(mFirst, mNext, ComposeFilters(f, mCond));
        }

        public override CForEach DropWhile(FilterFxn f)
        {
            object cur = mFirst;
            while (mCond(cur) && f(cur))
            {
                cur = mNext(cur);
            }
            if (mCond(cur)) return Nil();
            return Gen(cur, mNext, mCond);
        }
    }
}
