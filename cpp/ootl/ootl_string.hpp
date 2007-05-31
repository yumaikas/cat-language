// Public Domain by Christopher Diggins 
// http://www.ootl.org 

#ifndef OOTL_STRING_HPP
#define OOTL_STRING_HPP

#include "ootl_stack.hpp"

namespace ootl
{

	struct cstring
	{
		cstring() : m("") { }
		cstring(const char* x) : m(x) { }
		cstring(const cstring& x) : m(x.m) { }
		operator const char*() { return to_ptr(); }
		const char* to_ptr() { return m; }
		bool operator==(const cstring& x) const 
		{
			const char* left = m;
			const char* right = x.m;
			while ((*left != '\0') && (*right != '\0'))
				if (*left++ != *right++) return false;
			return (*left == *right); 
		}
	private:
		const char* m;
	};

	struct string 
	{  
		struct dyn_buffer
		{
			dyn_buffer(const dyn_buffer& x)
			{
				size_t n = strlen(x.m);
				m = new char[n];
				strcpy_s(m, n, x.m);
			}
			dyn_buffer(const string& s) 
			{
				size_t n = s.count();
				m = new char[n + 1];
				s.copy_to_array(m);
				m[n] = '\0';
			}
			~dyn_buffer()
			{
				delete[] m;
			}
			operator const char*() const 
			{ 
				return m; 
			}
		private:
			dyn_buffer() { }
			char* m;
		};

		typedef string self;
	  
		string(const char* x) {
			assign(x);
		}
		string(const self& x) : m(x.m) {
		}
		self& assign(const char* x) {
			clear();
			return concat(x);
		}  
		self& assign(const self& x) {
			clear();
			return concat(x);
		}  
		char& operator[](int n) {
			return m[n];
		}
		const char& operator[](int n) const {
			return m[n];
		}
		size_t count() const {
			return m.count();
		}
		template<typename T>
		self& concat(const T& x) {
			x.foreach(stacker(*this));  
			return *this;
		}
		self& concat(const char* x) {  
			if (x == NULL) return *this;
			while (*x != 0) {
				m.push(*x++);
			}
			return *this;
		}
		void push(char x) {
			m.push(x);
		}  
		char pop() {
			return m.pull();
		}
		void clear() {
			m.clear();
		}
		template<typename Proc>
		void foreach(Proc& x) {
			m.foreach(x);
		}
		template<typename Proc>
		void foreach(Proc& x) const {
			m.foreach(x);
		}
		self& operator=(const self& x) {
			return assign(x);
		}	
		self& operator=(const char* x) {
			return assign(x);
		}
		self& operator+=(const self& x) {
			return concat(x);
		}
		self& operator+=(const char* x) {
			return concat(x);
		}
		const self& operator+(const self& x) const {
			return self(*this).concat(x);
		}
		const self& operator+(const char* x) const {
			return self(*this).concat(x);
		}
		void copy_to_array(char* x) const 
		{
			m.copy_to_array(x);
		}
		dyn_buffer to_char_ptr() const
		{
			return dyn_buffer(*this);
		}
	private:    
		stack<char> m;
	};

}

#endif 
