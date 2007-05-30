// Public domain, by Christopher Diggins
// http://www.cdiggins.com
//
// This files is for the construction of parse trees. Every node in a parse tree is derived 
// from Node and provides access to the rule it represents through a function:
// virtual const type_info& GetRule(). Children are arranged by type. 
// Once you have the first child node, you can repeatedly call GetSibling() to get the 
// next node of the same type. 
//
// Parse trees are created automatically by inserting Store<T> productions into a grammar. 

#ifndef YARD_TREE_HPP
#define YARD_TREE_HPP

namespace yard
{   
	/////////////////////////////////////////////////////////////////////
	// Abstract Syntax Tree 

	// The nodes in a ParseTree are generated using Store<Rule_T> production rules		
	// The allocation/dellocation of nodes could be optimized if you need extra performance,
	// by using a custom allocator and deallocator.
	template<typename Iter_T>
	struct Ast
	{
		Ast(Iter_T begin) 
			: current(NULL), root(-1, begin, NULL)
		{ 
			current = &root;
		}

		void AddNode()
		{
			assert(this != NULL);
			Node* x = new ParseNode<Rule_T>(pos, this);
			mChildren.push_back(x);
			assert(*FindChild(x) == x); 
			return x;    
		}
	
		struct Node
		{
			// public types 
			typedef Iter_T TokenIter;     
		        
			// 'tors
			Node(int id, TokenIter pos, Node* parent) 
			{  
				mnId = id;
				mpFirst = pos;
				mpLast = pos;
				mpNext = NULL;
				mpChild = NULL;
				mpLastChildPtr = &mpChild;
				mpParent = parent;
				mbCompleted = false;
			}		    
			~Node()
			{
				if (mpNext)
					delete mpNext;
				if (mpChild)
					delete mpChild;
			}
			void AddChild(Node* child)
			{
				assert(!IsCompleted());
				assert(mpNext == NULL);
				assert(*mpLastChildPtr == NULL);
				*mpLastChildPtr = child;
				mpLastChildPtr = &(child->mpNext);
			}    
			Node* NewChild(int nType, TokenIter pos)
			{
				assert(!IsCompleted());
				Node* ret = new Node(nType, pos, this);
				AddChild(ret);
				return ret;
			}  
			void DeleteChild(Node* p)
			{
				// This function only allows deletion of the last child
				// in a list.
				assert(p->mpNext == NULL);
				assert(&(p->mpNext) == mpLastChildPtr);
				assert(mpChild != NULL);

				if (p == mpChild) 
				{
					delete p;
					mpChild = NULL;
					mpLastChildPtr = &mpChild;				
				}
				else
				{
					// Start at first child
					Node* pBeforeLast = mpChild;					

					// iterate through siblings, until we reach the child before 
					// the last one
					while (pBeforeLast->mpNext != p)
					{
						pBeforeLast = pBeforeLast->mpNext;
						assert(pBeforeLast != NULL);
					}
					delete p;
					pBeforeLast->mpNext = NULL;
					mpLastChildPtr = &(pBeforeLast->mpNext);				
				}
			}
			bool HasChildren() 
			{
				return mpChild != NULL;
			}
			Node* GetFirstChild()
			{
				return mpChild;
			}
			bool HasSibling() 
			{
				return mpNext != NULL;
			}
			Node* GetSibling()
			{
				return mpNext;
			}	 
			Node* GetParent()
			{
				return mpParent;
			}
			void Complete(TokenIter pos) {      
				assert(!IsCompleted());
				mpLast = pos;      
				mbCompleted = true;
				assert(IsCompleted());
			}
			bool IsCompleted() {
				return mbCompleted;
			}    		    
			TokenIter GetFirstToken() {
				assert(IsCompleted());
				return mpFirst;
			}    
			TokenIter GetLastToken() {
				assert(IsCompleted());
				return mpLast;
			}    
			int GetLabelId()
			{
				return mnId;
			}
			template<typename Function_T> 
			void Visit(Function_T fxn, int id = -1)
			{
				Node* pChild = GetFirstChild();
				while (pChild != NULL)
				{
					pChild->Visit(fxn, id);
					pChild = pChild->GetSibling();
				}
				if (id != -1 && GetLabelId() != id) return;
				fxn(this);	
			}

		    
		private:

			// fields
			bool mbCompleted;
			int mnId;
			Node* mpParent;
			Node* mpChild;
			Node* mpNext;
			Node** mpLastChildPtr;
			TokenIter mpFirst;
			TokenIter mpLast;
		};	    

		// acccess the root node
		Node* GetRoot() {
			return &root;
		} 
	    
		// StartNode is called when an attempt is made to match a 
		// Store production rule
		template<typename Parser_T>
		void StartNode(int type, Parser_T& p) { 
			assert(current != NULL);
			typename Parser_T::iterator pos = p.GetPos();
			current = current->NewChild(type, pos);
			assert(current != NULL);
		}

		// StartNode is called when a Store production rule
		// is successfully matched 
		template<typename Parser_T>
		void CompleteNode(int type, Parser_T& p) {
			assert(current != NULL);
			assert(current->GetLabelId() == type);
			typename Parser_T::iterator pos = p.GetPos();
			current->Complete(pos);
			assert(current->IsCompleted());       
			current = current->GetParent();
			assert(current != NULL);
		}

		// AbandonNode is called when a Store<Rule_T> production rule
		// fails to match
		template<typename Parser_T>
		void AbandonNode(int type, Parser_T& p) {    
			assert(current != NULL);
			assert(current->GetLabelId() == type);
			Node* tmp = current;
			assert(!tmp->IsCompleted());
			current = current->GetParent();
			assert(current != NULL);
			current->DeleteChild(tmp);
		}

		// deletes the current tree. 
		void Clear() {
			assert(current == &root);
			root.Clear();
		}
	    
	private:
		 
		Node root;       
		Node* current; 
	};
}

#endif 
