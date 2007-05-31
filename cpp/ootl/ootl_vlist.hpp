// Public Domain by Christopher Diggins 
// http://www.ootl.org 

#ifndef OOTL_VLIST_HPP
#define OOTL_VLIST_HPP

#include <cstdlib>
#include <memory>

void ootl_assert(bool b) {
	if (!b) 
		throw 1;
}

namespace ootl 
{
	template<typename T, int Factor_N = 2, int Initial_N = 2>
	struct vlist
	{
		vlist() 
		{ 
			mFirst = new buffer(Initial_N);
			mLast = mFirst;
			mCap = Initial_N;
		}

		//////////////////////////////////////////////////////
		// typedefs 

		typedef vlist self;
		typedef T value_type;

		//////////////////////////////////////////////////////
		// buffer data structure

		struct buffer  
		{
			buffer(size_t n, size_t i = 0) : 
				size(n),
				index(i), 
				prev(NULL), 
				next(NULL),
				begin((T*)malloc(n * sizeof(T))),
				end(begin + n)
			{ 
				ootl_assert(n >= Initial_N);
				memset(begin, 0, size * sizeof(T));
			}

			size_t index; 
			size_t size;
			T* begin;
			T* end;
			buffer* prev;
			buffer* next;
		};    

		//////////////////////////////////////////////////////
		// implementation functions

		buffer* get_first_buffer() 
		{
			return mFirst;
		}
		const buffer* get_first_buffer() const 
		{
			return mFirst;
		}
		buffer* get_last_buffer() 
		{
			return mLast;
		}
		const buffer* get_last_buffer() const 
		{
			return mLast;
		}
		T* get_pointer(size_t n) 
		{
			ootl_assert(n >= 0);
			ootl_assert(n < mCap);
			if (n >= mLast->index) 
			{ 
				return mLast->begin + (n - mLast->index); 
			}
			buffer* curr = mLast->prev;    
			ootl_assert(curr != NULL);
			while (n < curr->index) 
			{
				curr = curr->prev;    
				ootl_assert(curr != NULL);
			}
			return curr->begin + (n - curr->index);
		}
		const T* get_pointer(size_t n) const 
		{
			return const_cast<self*>(this)->get_pointer(n);
		}
		size_t capacity() const 
		{
			return mCap;     
		}
		void add_buffer() 
		{
			if (mLast == NULL) 
			{
				mLast = mFirst;
			}
			else 
			{
				add_buffer(new buffer(mLast->size * Factor_N, mCap));
			}
			ootl_assert(mFirst->size >= Initial_N);
			ootl_assert(mLast->size >= Initial_N);
		}
		void add_buffer(buffer* x) 
		{
			ootl_assert(mLast != NULL);
			ootl_assert(x != NULL);
			x->prev = mLast;
			mLast->next = x;
			mLast = x;
			mCap += mLast->size;
		}    
		void remove_buffer() 
		{
			ootl_assert(mLast != NULL);
			if (mLast == mFirst)
			{
				mLast = NULL;
			}
			else
			{
				buffer* tmp = mLast;
				mCap -= mLast->size;
				mLast = mLast->prev;
				ootl_assert(mLast != NULL);
				mLast->next = NULL;
				delete(tmp);
			}
		}    

	private:

		//////////////////////////////////////////////////////
		// fields
				
		buffer* mFirst; 
		buffer* mLast;
		size_t mCap;
	};
}

#endif 