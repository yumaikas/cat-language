// Public Domain by Christopher Diggins 
// http://www.ootl.org 

#ifndef OOTL_VLIST_HPP
#define OOTL_VLIST_HPP

#include <cstdlib>
#include <cassert>
#include <memory>

namespace ootl 
{
	template<typename T, int Factor_N = 2, int Initial_N = 8>
	struct vlist
	{
		vlist() : mbuff(NULL), cap(0) 
		{ }

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
				size_minus_one(n-1),
				index(i), 
				prev(NULL), 
				next(NULL),
				begin((T*)malloc(n * sizeof(T))),
				end(begin + n)
			{ 
				memset(begin, 0, size * sizeof(T));
			}

			size_t index; 
			size_t size_minus_one;
			size_t size;
			T* begin;
			T* end;
			buffer* prev;
			buffer* next;
		};    

		//////////////////////////////////////////////////////
		// implementation functions

		void initialize(size_t cap = Initial_N) {  
			cap = cap > Initial_N ? cap : Initial_N;
			assert(mbuff == NULL);
			add_buffer(new buffer(cap, 0));			
		}
		buffer* get_first_buffer() {
			buffer* cur = mbuff;
			if (cur == NULL) return NULL;
			while (cur->prev != NULL) {
				cur = cur->prev;
			}
			return cur;
		}
		const buffer* get_first_buffer() const {
			return const_cast<self*>(this)->get_first_buffer();
		}
		buffer* get_last_buffer() {
			return mbuff;
		}
		const buffer* get_last_buffer() const {
			return mbuff;
		}
		T* get_pointer(size_t n) {
			assert(n >= 0);
			assert(n < cap);
			if (n >= mbuff->index) { 
				return mbuff->begin + (n - mbuff->index); 
			}
			buffer* curr = mbuff->prev;    
			assert(curr != NULL);
			while (n < curr->index) {
				curr = curr->prev;    
				assert(curr != NULL);
			}
			return curr->begin + (n - curr->index);
		}
		const T* get_pointer(size_t n) const {
			return const_cast<self*>(this)->get_pointer(n);
		}
		size_t capacity() const {
			return cap;     
		}
		void add_buffer() {
			if (mbuff == NULL) {
				add_buffer(new buffer(Initial_N, 0));
			} else {
				add_buffer(new buffer(mbuff->size * Factor_N, cap));
			}   
		}
		void add_buffer(buffer* x) {
			assert(x != NULL);
			x->prev = mbuff;
			if (mbuff != NULL) mbuff->next = x;
			mbuff = x;
			cap += mbuff->size;
		}    
		void remove_buffer() {
			assert(mbuff != NULL);
			buffer* tmp = mbuff;
			cap -= mbuff->size;
			mbuff = mbuff->prev;
			delete(tmp);
		}    

	private:

		//////////////////////////////////////////////////////
		// fields
		
		buffer* mbuff; // the top-most active buffer 
		size_t cap;
	};
}

#endif 