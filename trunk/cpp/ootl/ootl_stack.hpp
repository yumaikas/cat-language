// Public Domain by Christopher Diggins 
// http://www.ootl.org 
//
// The file contains the implementation for a persistent stack: a stack class that maintains its memory layout
// even when more memory needs to be allocated. This means that adding items always has O(1) complexity, instead
// of O(n) in the worst case as with most common stack implementations. The implementation is based on a vlist which 
// also provides an average case of O(1) complexity for item indexing. A vlist is a list of buffers, each twice 
// as big as the previous. The O(1) complexity has to do with the fact that most items occur in the bigger buffers,
// There is no "begin" and "end" like STL collections, but for iteration over the collection, you can use the "foreach" 
// member function (or implement your own special iterator)

#ifndef OOTL_STACK_HPP
#define OOTL_STACK_HPP

#include "ootl_vlist.hpp"

namespace ootl 
{
	/////////////////////////////////////////////////////////
	// utility functions for dealing with stack.

	// used for generating stack functors
	template<typename Stack>
	struct stacker_proc {
		stacker_proc(Stack& s) : m(s) { }
		template<typename T>
		void operator()(const T& x) { m.push(x); }
		Stack& m;
	};

	// used for constructing stack copies
	template<typename Stack>
	stacker_proc<Stack> stacker(Stack& s) { 
		return stacker_proc<Stack>(s);
	}

	/////////////////////////////////////////////////////////
	// ootl::stack implementation

	template < typename T >
	struct stack : protected vlist<T>
	{
	public:
		
		//////////////////////////////////////////////////////
		// public type defs 

		typedef stack self;
		typedef T value_type;  

		//////////////////////////////////////////////////////
		// constructor/destructors 

		stack() : vlist(), cnt(0), ptop(NULL) { 
			ptop = get_first_buffer()->begin;
		}
		stack(const self& x) : vlist(), cnt(0), ptop(NULL) { 
			ptop = get_first_buffer()->begin;
			x.foreach(stacker(*this));
		}
		stack(size_t nsize, const T& x = T()) : vlist(), cnt(0), ptop(NULL) { 
			initialize(nsize);
			ptop = get_first_buffer()->begin;
			while (count() < nsize) {
			  push(x);
			}
		}
		~stack() { 
			while (count() > 0) {      
			  pop();
			}
		} 

		//////////////////////////////////////////////////////
		// implementation of OOTL Indexable concept
		// 
		// Note: ootl::stack[0] is the top of the stack.

		const T& get_at(size_t n) {
			return operator[](n);
		}
		void set_at(size_t n, const T& x) {
			*get_pointer(cnt - 1 - n) = x;
		}
		T& operator[](size_t n) {
			return *get_pointer(cnt - n - 1);
		}
		const T& operator[](size_t n) const {
			return *get_pointer(cnt - n - 1);
		}
		size_t count() const {
			return cnt;
		}

		///////////////////////////////////////////////////
		// implementation of OOTL Stack concept

		void push(const T& x) {
			push_nocreate();
			new(ptop - 1) T(x);
		}
		void push() {
			push_nocreate();
			new(ptop - 1) T();
		}
		// adds space for an object, without constructing
		void push_nocreate() {
			ootl_assert(ptop >= get_last_buffer()->begin);
			ootl_assert(ptop <= get_last_buffer()->end);
			if (ptop == get_last_buffer()->end) {
				add_buffer();
				ptop = get_last_buffer()->begin;
			}
			++ptop;
			++cnt;
			ootl_assert(ptop > get_last_buffer()->begin);
			ootl_assert(ptop <= get_last_buffer()->end);
		}
		void pop() {        
			top().~T();          
			pop_nodestroy();
		}
		// removes an object without calling destructor
		void pop_nodestroy() {        
			ootl_assert(cnt > 0);
			ootl_assert(ptop > get_last_buffer()->begin);
			ootl_assert(ptop <= get_last_buffer()->end);
			--ptop;
			--cnt;
			if ((ptop == get_last_buffer()->begin) && (cnt != 0)) {
				ptop = get_last_buffer()->prev->end;
				remove_buffer();
			}
		}
		bool is_empty() {
			return count() == 0;
		}
		T& top() {
			ootl_assert(cnt > 0);
			ootl_assert(ptop > get_last_buffer()->begin);
			ootl_assert(ptop <= get_last_buffer()->end);
			return *(ptop - 1);
		}
		const T& top() const {
			ootl_assert(cnt > 0);
			ootl_assert(ptop > get_last_buffer()->begin);
			ootl_assert(ptop <= get_last_buffer()->end);
			return *(ptop - 1);
		}
		T pull() {
			ootl_assert(cnt > 0);
			T ret = top();
			pop();
			return ret;    
		}
		void clear() {
			while (!is_empty()) {
				pop();
			}
			ootl_assert(cnt == 0);
		}
		void clear_nodestroy() {
			while (!is_empty()) {
				pop_nodestroy();
			}
		}

		//////////////////////////////////////////////////////
		// implementation of OOTL Iterable concept 

		template<typename Procedure>
		void foreach(Procedure& proc) const {
			const buffer* cur = get_first_buffer();    
			size_t n = 0;
			while (n < count()) {
				T* p = cur->begin;
				while ((p != cur->end) && (n < count())) {
					if (p == ptop) return;
					proc(*p++);
					++n;
				}
				cur = cur->next;
			} 
		}  

		//////////////////////////////////////////////////////
		// implementation of OOTL Growable concept

		void grow(size_t n = 1, const value_type& x = value_type()) {      
			while (n--) push(x);
		} 

		//////////////////////////////////////////////////////
		// implementation of OOTL Shrinkable concept

		void shrink(size_t n = 1) {      
			while (n--) pop();
		}  

		//////////////////////////////////////////////////////
		// implementation of OOTL Resizable concept  

		void resize(size_t n, const value_type& x = value_type()) {      
			while (n > count()) push(x);
			while (n < count()) pop();
		}    

		//////////////////////////////////////////////////////
		// Utility functions

		template<typename T>
		void copy_to_array(T* arr) const
		{
			struct copy_proc
			{
				T* mp;
				copy_proc(T* p) : mp(p) { }
				void operator()(const T& x) { *mp++ = x; }
			};

			foreach(copy_proc(arr));
		}

		// todo: this should be generalized as a zip function 
		// or I could just use iterators
		bool operator==(const self& x) const 
		{
			if (count() != x.count()) 
				return false;
			const buffer* cur1 = get_first_buffer();    
			const buffer* cur2 = x.get_first_buffer();    
			size_t n = 0;
			while (n < count()) {
				T* p1 = cur1->begin;
				T* p2 = cur2->begin;
				while ((p1 != cur1->end) && (p2 != cur2->end) && (n < count())) {
					if (!(*p1 == *p2)) 
						return false;
					n++;
				}
				if (p1 == cur1->end) cur1 = cur1->next;
				if (p2 == cur2->end) cur2 = cur2->next;
			} 
			return true;
		}

		//////////////////////////////////////////////////////////////
		// fields 

	private:

		// hide the assignment operator
		void operator=(const self& x) { };

		size_t cnt;
		T* ptop;
	};
}

#endif
