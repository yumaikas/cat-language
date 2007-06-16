// Public domain, by Christopher Diggins
// http://www.cdiggins.com

#ifndef YARD_PARSER_HPP
#define YARD_PARSER_HPP

namespace yard
{
	template<typename Token_T, typename Iter_T = const Token_T*>
    struct Parser
    {   
		// Constructor
        Parser(Iter_T first, Iter_T last) 
            : mBegin(first), mEnd(last), mIter(first), mTree(first)
        { }

		// Parse function
		template<typename StartRule_T>
		bool Parse()
		{
			try
			{
				return StartRule_T::Match(*this);
			}
			catch(...)
			{
				OutputParsingErrorLocation(*this);
				return false;
			}
		}

        // Public typedefs 
        typedef Iter_T Iterator;
        typedef Token_T Token; 
		typedef Ast<Iterator> Tree;
		typedef typename Tree::Node Node;
                
        // Input pointer functions 
        Token GetElem() { return *mIter; }  
        void GotoNext() { assert(mIter < End()); ++mIter; }  
        Iterator GetPos() { return mIter; }  
        void SetPos(Iterator pos) { mIter = pos; }  
        bool AtEnd() { return GetPos() >= End(); }  
        Iterator Begin() { return mBegin; }    
        Iterator End() { return mEnd; }  

		// AST functions
		Node* GetAstRoot() { return mTree.GetRoot(); }
		void StartNode(int type) { mTree.StartNode(type, *this); }  		
		void CompleteNode(int type) { mTree.CompleteNode(type, *this); }
		void AbandonNode(int type) { mTree.AbandonNode(type, *this); }

	private:

		// Member fields
		Iterator	mBegin;
		Iterator	mEnd;
        Iterator	mIter;
        Tree		mTree;
    };  
 }

#endif 
