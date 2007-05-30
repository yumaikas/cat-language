// Public Domain by Christopher Diggins 
// http://www.ootl.org

#ifndef OOTL_OBJECT_HPP
#define OOTL_OBJECT_HPP

#include <typeinfo>
#include <memory>

#include "ootl_string.hpp"

namespace ootl
{  
	typedef const std::type_info& TI;
	  
	// can hold a copy of any copy constructible class
	struct object 
	{   
		// this represents the maximum size of an Object for its copy / etc. to be optimized 
		static const int buffer_size = sizeof(void*); 
	  
		// used to identify empty object types 
		struct empty {};

		// used to hold an Object or a pointer to an Object
		union holder {
			// used fields
			char buffer[buffer_size];
			void* pointer; 	    
			// the following fields exist to help assure alignment 
			double unused_double;      	    
			long unused_long;
		};	      	  
		
		// function pointer table
		struct fxn_ptr_table {
			TI (*type_info)();
			void* (*get_ptr)(holder&);
			const void* (*get_const_ptr)(const holder&);
			void  (*destructor)(holder&);
			void  (*deleter)(holder&);
			void  (*clone)(holder&, const holder&);
		};

		// static functions for optimized types (stored in holder::buffer)
		template<typename T, bool can_optimize>
		struct fxns 
		{
			static const bool optimized = true;
			static TI type_info() { return typeid(T); }
			static T* cast(holder& x) { return reinterpret_cast<T*>(x.buffer); }
			static const T* cast(const holder& x) { return reinterpret_cast<const T*>(x.buffer); }
			static void* get_ptr(holder& x) { return x.buffer; } 
			static const void* get_const_ptr(const holder& x) { return x.buffer; } 
			static void  destructor(holder& x) { cast(x)->~T();  }
			static void  deleter(holder& x) { destructor(x); x.pointer = NULL; }
			static void  clone(holder& x, const holder& y) {  new(x.buffer) T(*cast(y)); }
		};

		// static functions for unoptimized types (pointed to by holder::pointer)
		template<typename T>
		struct fxns<T, false> 
		{
			static const bool optimized = false;
			static T* cast(holder& x) { return reinterpret_cast<T*>(x.pointer); }
			static const T* cast(const holder& x) { return reinterpret_cast<const T*>(x.pointer); }
			static TI type_info() { return typeid(T); }
			static void* get_ptr(holder& x) { return x.pointer; } 
			static const void* get_const_ptr(const holder& x) { return x.pointer; } 
			static void  destructor(holder& x) { cast(x)->~T(); }
			static void  deleter(holder& x) { delete(cast(x)); }
			static void  clone(holder& x, const holder& y) { x.pointer = new T(*cast(y)); }
		};  
		
		// this creates a function pointer table which points to functions for dealing with
		// either optimized or unoptimized types. 	
		template<typename T> 
		static fxn_ptr_table* get_table() {
		  const bool optimize = sizeof(T) <= buffer_size;
			static fxn_ptr_table static_table = {
				&fxns<T, optimize>::type_info
			  , &fxns<T, optimize>::get_ptr
			  , &fxns<T, optimize>::get_const_ptr
			  , &fxns<T, optimize>::destructor
			  , &fxns<T, optimize>::deleter
			  , &fxns<T, optimize>::clone
			};
			return &static_table;
		}	

		struct bad_object_cast {
		  bad_object_cast(TI x, TI y) :
			from(x), to(y)
		  { }
		  
		  TI from;
		  TI to;
		};
	  
		// constructors   
		object() {
			table = get_table<empty>();
			held.pointer = NULL;
		}
		object(const object& x) {
			table = get_table<empty>();
			held.pointer = NULL;
			assign(x);
		}  
		template <typename T>
		object(const T& x) {
			table = get_table<empty>();
			held.pointer = NULL;
			initialize(x);
		}    
		object(const char* x) {
			table = get_table<empty>();
			held.pointer = NULL;
			initialize(cstring(x));
		}    
		~object() {
			release();
		}    
		// assignment
		template<typename T>
		void initialize(const T& x) {
			table = get_table<T>();
			if (sizeof(T) <= buffer_size) 
				new(held.buffer) T(x);
			else 
				held.pointer = new T(x); 
		}
		object& assign(const object& x) {
			release();
			table = x.table;	  
			table->clone(held, x.held);
			return *this;
		}
		template<typename T>
		object& operator=(const T& x) {
			return assign(object(x));
		}
		object& operator=(const char* x) {
			return assign(object(cstring(x)));
		}
		object& operator=(const object& x) {
			return assign(x);
		}
		// member functions
		TI type_info() const {
			return table->type_info();
		}
		template<typename T>
		bool is() const {
			return type_info() == typeid(T) ? true : false;
		}
		template<typename T>
		T& to() {
			if (!is<T>()) 
				throw bad_object_cast(type_info(), typeid(T)); 
			return *to_ptr<T>();
		}
		template<typename T>
		const T& to() const {
			if (!is<T>()) 
				throw bad_object_cast(type_info(), typeid(T)); 
			return *to_ptr<T>();
		}
		template<typename T>
		T* to_ptr() {
			return reinterpret_cast<T*>(table->get_ptr(held));
		}
		template<typename T>
		const T* to_ptr() const {
			return reinterpret_cast<const T*>(table->get_const_ptr(held));
		}
		bool is_empty() const {
			return table == get_table<empty>();
		}
		void move_to(object& o)
		{
			memcpy(&o, this, sizeof(*this));
			table = get_table<empty>();
		}
		void release() {
			if (is_empty()) return; 
			table->deleter(held);
			table = get_table<empty>();
		}
		void release_nodestroy()
		{
			table = get_table<empty>();
		}		
		object* operator->() {
			return this;
		}
		
		// fields 
		fxn_ptr_table* table;
		holder held;
	};
}

#endif
