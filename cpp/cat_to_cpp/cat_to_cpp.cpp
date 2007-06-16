// public domain by Christopher Diggins
// Cat to C++ translator 
// http://www.cat-language.com 

#define _CRT_SECURE_NO_DEPRECATE

#include "..\yard\yard.hpp"
#include "..\ootl\ootl_stack.hpp"
#include "..\ootl\ootl_hash.hpp"

#include "cat_grammar.hpp"

using namespace cat_grammar;

typedef yard::Parser<char> CatParser;
typedef CatParser::Node Node;
typedef CatParser::Iterator Iterator;

ootl::hash_map<Node*, int> anon_fxns;

void printch(char c)
{
	switch (c)
	{
		case '_' : printf("__"); break;
		case '~': printf("_tilde_"); break;
		case '`': printf("_backquote_"); break;
		case '!': printf("_exclaim_"); break;
		case '@': printf("_apos_"); break;
		case '#': printf("_hash_"); break;
		case '$': printf("_dollar_"); break;
		case '%': printf("_percent_"); break;
		case '^': printf("_caret_"); break;
		case '&': printf("_amp_"); break;
		case '*': printf("_star_"); break;
		case '(': printf("_lparan_"); break;
		case ')': printf("_rparan_"); break;
		case '-': printf("_minus_"); break;
		case '+': printf("_plus_"); break;
		case '=': printf("_eq_"); break;
		case '|': printf("_pipe_"); break;
		case '\\': printf("_bslash_"); break;
		case ':': printf("_colon_"); break;
		case '<': printf("_lt_"); break;
		case '>': printf("_gt_"); break;
		case ',': printf("_comma_"); break;
		case '.': printf("_dot_"); break;
		case '?': printf("_question_"); break;
		case '/': printf("_slash_"); break;			
		default: 
			assert((c >= '1' && c <= '9') || (c == '0') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
			putchar(c);
			break;
	}		
}

void OutputNodeText(Node* p)
{
	Iterator i = p->GetFirstToken();	
	while (i != p->GetLastToken())
		putchar(*i++);
}

void OutputName(Node* p)
{
	assert(p->GetLabelId() == CatWordLabel::id);
	printf("_"); 
	Iterator i = p->GetFirstToken();	
	while (i != p->GetLastToken())
		printch(*i++);
}

void OutputFxnSig(Node* p)
{
	assert(p->GetLabelId() == DefLabel::id);
	Node* pName = p->GetFirstChild();
	assert(pName != NULL);
	printf("void ");
	OutputName(pName);
	printf("()");
}

void OutputForwardDecls(Node* p)
{
	assert(p->GetLabelId() == DefLabel::id);
	OutputFxnSig(p);
	printf(";\n");
}

void OutputQuotationForwardDecls(Node* p)
{
	assert(p->GetLabelId() == QuotationLabel::id);
	static int nId = 0;
	anon_fxns.add(p, nId);
	printf("void _cat_anon%d();\n", nId);
	++nId;
}

void OutputQuotation(Node* p)
{
	assert(p->GetLabelId() == QuotationLabel::id);
	int nId = anon_fxns[p];
	printf("    push_function(_cat_anon%d); //", nId);
	OutputNodeText(p);
	printf("\n");
}

void OutputWord(Node* p)
{
	assert(p->GetLabelId() == CatWordLabel::id);
	printf("    call(");
	OutputName(p);
	printf(");\n");
}

void OutputLiteral(Node* p)
{
	assert(p->GetLabelId() == LiteralLabel::id);	
	printf("    push_literal(");
	OutputNodeText(p);
	printf(");\n");
}

void OutputExpr(Node* p)
{
	assert(p->GetLabelId() == ExprLabel::id);	
	assert(p->HasChildren());
	Node* pChild = p->GetFirstChild();
	assert(!pChild->HasSibling());	
	switch (pChild->GetLabelId())
	{
	case QuotationLabel::id :
		OutputQuotation(pChild);
		break;
	case CatWordLabel::id :
		OutputWord(pChild);
		break;
	case LiteralLabel::id :
		OutputLiteral(pChild);
		break;
	default:
		assert(false && "unrecognized expression type");
	}
}

void OutputFunctionDefs(Node* p)
{
	OutputFxnSig(p);
	printf("\n{\n");
	Node* pTmp = p->GetFirstChild();
	while (pTmp != NULL) {
		if (pTmp->GetLabelId() == ExprLabel::id)
		{
			OutputExpr(pTmp);
		}
		pTmp = pTmp->GetSibling();
	}

	printf("}\n");
}

void OutputQuotationDefs(Node* p)
{
	int nId = anon_fxns[p];
	printf("void _cat_anon%d()\n{\n", nId);
	Node* pTmp = p->GetFirstChild();
	while (pTmp != NULL) {
		assert(pTmp->GetLabelId() == ExprLabel::id);
		OutputExpr(pTmp);
		pTmp = pTmp->GetSibling();
	}
	printf("}\n");
}

void test_hash()
{
	ootl::hash_map<int, int> h;
	
	int nSize = 10;
	for (int i=1; i < nSize; ++i)
		h.add(i, i);
	for (int i=1; i < nSize; ++i)
		assert(h[i] == i);
}

int main(int argc, char* argv[])
{	
	//test_hash();

	FILE* in = stdin; 
	FILE* out = stdout;
	
	// redirect standard in if requested
	if (argc > 1)
	{
		errno_t err = freopen_s(&in, argv[1], "r", stdin);
		if (err != 0)
		{
    		fprintf(stderr, "unable to open file for reading: %s, %s", argv[1], strerror(err));
			exit(1);
		}
	}

	// redirect standard out if requested
	if (argc > 2)
	{
		errno_t err = freopen_s(&out, argv[2], "w", stdout);
		if (err != 0)
		{
			fprintf(stderr, "standard out to '%s' failed with message: %s", argv[2], strerror(err));
			exit(2);
		} 
	}

	// get data from file
	ootl::stack<char> char_stk; 
	int c; 	
	while ((c = getchar()) != EOF)
		char_stk.push(c);

	// close standard in, does nothing if not redirected
	fclose(in);
	
	// allocate a buffer of characters
	size_t n = char_stk.count();
	char* char_buf = new char[n];
	char_stk.copy_to_array(char_buf);
	
	CatParser p(char_buf, char_buf + n);
		
	if (!p.Parse<SourceFile>())
	{
		perror("parsing failed: invalid input");
	}
	else
	{
		try
		{
			printf("// C++ file generated from Cat input\n");
			printf("// created using the Cat to C++ tool\n");
			printf("// by Christopher Diggins\n\n");
			printf("// http://www.cat-language.com\n");
			printf("\n");
			p.GetAstRoot()->Visit(OutputForwardDecls, DefLabel::id);
			p.GetAstRoot()->Visit(OutputQuotationForwardDecls, QuotationLabel::id);
			p.GetAstRoot()->Visit(OutputFunctionDefs, DefLabel::id);
			p.GetAstRoot()->Visit(OutputQuotationDefs, QuotationLabel::id);
		}
		catch(...)
		{
			perror("untrapped exception occured");
		}
	}

	fflush(stdout);
	delete[] char_buf;
}

