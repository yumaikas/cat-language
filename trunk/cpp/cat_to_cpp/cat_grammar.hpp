// Public domain 
// YARD grammar for the Cat languages
// by Christopher Diggins 
// http://www.cat-language.com

#ifndef CAT_GRAMMAR_HPP
#define CAT_GRAMMAR_HPP

namespace cat_grammar
{	
	using namespace yard;
	using namespace text_grammar;

	// Forward declarations, used for recursive definitions
	struct FxnType;
	struct Expr;

	// AST node labels
	struct LiteralLabel		{ static const int id = 0; };
	struct QuotationLabel	{ static const int id = 1; };
	struct KindVarLabel		{ static const int id = 2; };
	struct NamedTypeLabel	{ static const int id = 3; };
	struct FxnTypeLabel		{ static const int id = 4; };
	struct CatWordLabel		{ static const int id = 5; };
	struct ExprLabel		{ static const int id = 6; };
	struct DefLabel			{ static const int id = 7; };
	struct ArrowLabel		{ static const int id = 8; };
	struct TypeVectorLabel	{ static const int id = 9; };

	// Character sets 
	struct CatWordSymbolCharSet : CharSet<'~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '_', '+', '-', '=', '\\', ':', ';', '<', '>', '.', '?', '/'> { };
	struct CatWordFirstCharSet : CharSetUnion<CatWordSymbolCharSet, IdentFirstCharSet> { };
	struct CatWordNextCharSet : CharSetUnion<CatWordSymbolCharSet, IdentNextCharSet> { };

	// Grammar rules
	struct LineComment : FinaoIf<CharSeq<'/', '/'>, UntilPast<Char<'\n'> > > { };
	struct FullComment : FinaoIf<CharSeq<'/', '*'>, UntilPast<CharSeq<'*', '/'> > > { };
	struct Comment : Or<LineComment, FullComment> { };
	struct WS : Star<Or<CharSetParser<WhiteSpaceCharSet>, Comment> > { };
	struct StringCharLiteral : Or<Seq<Char<'\\'>, AnyChar>, NotChar<'\''> > { };
	struct CharLiteral : FinaoIf<Char<'\''>, Seq<StringCharLiteral, ExpectChar<'\''> > > { };
	struct StringLiteral : FinaoIf<Char<'\"'>, Seq<Star<StringCharLiteral>, ExpectChar<'\"'> > > { };
	struct BinaryDigit : Or<Char<'0'>, Char<'1'> > { };
	struct BinNumber : Seq<CharSeq<'0', 'b'>, Plus<BinaryDigit>, NotAlphaNum, WS> { };
	struct HexNumber : Seq<CharSeq<'0', 'x'>, Plus<HexDigit>, NotAlphaNum, WS> { };
	struct DecNumber : Seq<Opt<Char<'-'> >, Plus<Digit>, Opt<Seq<Char<'.'>, Star<Digit> > >, NotAlphaNum, WS> { };
	struct Literal : Seq<Store<LiteralLabel, Or<BinNumber, HexNumber, DecNumber, CharLiteral, StringLiteral > >, WS> { };		
	struct TypeConstraint : FinaoIf<Char<'='>, Seq<WS, FxnType> > { };
	struct NamedType : StoreIf<NamedTypeLabel, At<IdentFirstChar>, Seq<Ident, Opt<TypeConstraint> > > { };	
	struct CatWordFirstChar : CharSetParser<CatWordFirstCharSet> { };
	struct CatWordNextChar : CharSetParser<CatWordNextCharSet> { };	
	struct SingleCharCatWord : Or<Char<'('>, Char<')'>, Char<','> > { };
	struct MultiCharCatWord : Seq<CatWordFirstChar, Star<CatWordNextChar> > { };
	struct CatWord : Store<CatWordLabel, Or<SingleCharCatWord, MultiCharCatWord> > { };
	struct Quotation : StoreIf<QuotationLabel, At<Char<'['> >, Seq<Char<'['>, Star<Expr>, ExpectChar<']'> > > { };
	struct Expr : Store<ExprLabel, Seq<Or<Quotation, Literal, CatWord>, WS> > { };
	struct KindVar : StoreIf<KindVarLabel, Char<'\''>, Ident> { };
	struct Type : Seq<Or<KindVar, NamedType, FxnType>, WS> { };
	struct FxnBody : FinaoIf<Char<'{'>, Seq<WS, Star<Expr>, WS, ExpectChar<'}'>, WS > > { };
	struct Arrow : Store<ArrowLabel, Or<CharSeq<'~', '>'>, CharSeq<'-', '>'> > > { };
	struct TypeVector : Store<TypeVectorLabel, Star<Type> > { };
	struct FxnType : StoreIf<FxnTypeLabel, Char<'('>, Seq<WS, TypeVector, Arrow, WS, TypeVector, ExpectChar<')'>, WS > > { };
	struct FxnTypeDecl : FinaoIf<Char<':'>, Seq<WS, FxnType> > { };
	struct DefineKeyword : Seq<Word<CharSeq<'d','e','f','i','n','e'> >, WS> { };
	struct Def : StoreIf<DefLabel, DefineKeyword, Seq<CatWord, WS, Opt<FxnTypeDecl>, FxnBody> > { };
	struct SourceFile : Star<Seq<WS, Def> > { };
}

#endif // CAT_GRAMMAR_HPP