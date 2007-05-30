// Public domain, by Christopher Diggins
// http://www.cdiggins.com
//
// This is where the YARD parser does its magic. 

#ifndef YARD_PARSER_HPP
#define YARD_PARSER_HPP

namespace yard
{
	template<typename Iter_T, typename Value_T>
    struct Parser
    {   
        Parser(const char* first, const char* last) 
            : begin(first), end(last), iter(first), tree(first)
        { 
		}

		template<typename Rule_T>
		bool Match()
		{
			try
			{
				return Rule_T::Match(*this);
			}
			catch(...)
			{
				OutputParsingErrorLocation(*this);
				return false;
			}
		}

        // these typedefs are neccessary for a compatible YARD parser 
        typedef Iter_T iterator;
        typedef Value_T value_type; 
		typedef Ast<iterator> tree_type;
		typedef typename tree_type::Node node_type;
                
        // The following functions are for manipulating the pointer.
        value_type GetElem() { return *iter; }  
        void GotoNext() { assert(iter < end); ++iter; }  
        iterator GetPos() { return iter; }  
        void SetPos(iterator pos) { iter = pos; }  
        bool AtEnd() { return GetPos() >= End(); }  
        iterator Begin() { return begin; }    
        iterator End() { return end; }  

		// AST accessor function
		node_type* GetTree() { return tree.GetRoot(); }

		// AST construction functions
		void StartNode(int type) { tree.StartNode(type, *this); }  		
		void CompleteNode(int type) { tree.CompleteNode(type, *this); }
		void AbandonNode(int type) { tree.AbandonNode(type, *this); }

	private:

		iterator begin;
		iterator end;
        iterator iter;
        tree_type tree;
		bool mbSuccess;
    };  
 }

#endif 
