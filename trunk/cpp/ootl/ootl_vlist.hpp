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
	struct default_vlist_policy
	{
		static size_t initial_size() { return 8; }
		static size_t new_size(size_t old_size) { return old_size * 2; }
	};

	template<typename T, typename Policy_T = default_vlist_policy>
	struct vlist
	{
		vlist() 
		{ 
			mCap = Policy_T::initial_size();
			mFirst = new buffer(mCap);
			mLast = mFirst;
		}
		~vlist()
		{
			while (mLast != NULL)
				remove_buffer();
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
				ootl_assert(n >= Policy_T::initial_size());				
				memset(begin, 0, n * sizeof(T));
			}

			~buffer()
			{
				free(begin);
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
				add_buffer(new buffer(Policy_T::new_size(mLast->size), mCap));
			}
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

		// hide the copy constructor 
		vlist(const self& x) { };

		// hide the assignment operator
		void operator=(const self& x) { };

		//////////////////////////////////////////////////////
		// fields
				
		buffer* mFirst; 
		buffer* mLast;
		size_t mCap;
	};
}

#endif 