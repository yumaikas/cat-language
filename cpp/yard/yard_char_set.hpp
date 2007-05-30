// Public domain, by Christopher Diggins
// http://www.cdiggins.com
//
// These are classes for defining set of characters at compile-time, and checking membership
// with maximum efficiency. 

#ifndef YARD_CHAR_SET_HPP
#define YARD_CHAR_SET_HPP

namespace yard
{    
  template<typename char_type>
  struct BasicCharSet
  {    
    static const unsigned int size = 1 << (sizeof(char_type) * 8);
        
    BasicCharSet() {
      for (int i=0; i < size; i++) a[i] = false;        
    }
    BasicCharSet(const char& x) {
      for (int i=0; i < size; i++) a[i] = false;        
      a[x] = true;
    }
    BasicCharSet(const BasicCharSet& x) {
      for (int i=0; i < size; i++) a[i] = x[i];        
    }
    BasicCharSet(const char& beg, const char& end) {
      for (int i=0; i < size; i++) a[i] = false;        
      for (int i=beg; i < end; i++) a[i] = true;        
    }
    BasicCharSet& operator|=(const BasicCharSet& x) {
      for (int i=0; i < size; i++) a[i] = a[i] || x[i];      
      return *this;
    }
    BasicCharSet& operator|=(const char& x) {
      a[x] = true;
      return *this;
    }
    BasicCharSet& operator&=(const BasicCharSet& x) {
      for (int i=0; i < size; i++) a[i] = a[i] && x[i];      
      return *this;
    }
    BasicCharSet& operator=(const BasicCharSet& x) {
      for (int i=0; i < size; i++) a[i] = x[i];      
      return *this;
    }
    bool operator==(const BasicCharSet& x) {
      for (int i=0; i < size; i++) if (x[i] != a[i]) return false;
      return true;
    }
    bool operator!=(const BasicCharSet& x) {
      for (int i=0; i < size; i++) if (x[i] != a[i]) return true;
      return false;
    }
    BasicCharSet operator!() {
      BasicCharSet x;
      for (int i=0; i < size; i++) x.a[i] = !a[i];      
      return x;
    }
    bool& operator[](const char& c) { 
      return a[c]; 
    }
    const bool& operator[](const char& c) const { 
      return a[c]; 
    }
    friend BasicCharSet operator&(const BasicCharSet& x, 
      const BasicCharSet& y) 
    { 
      return BasicCharSet(x) &= y; 
    }
    friend BasicCharSet operator|(const BasicCharSet& x, 
      const BasicCharSet& y) 
    { 
      return BasicCharSet(x) |= y; 
    }
    bool a[size];
  };

  typedef BasicCharSet<char> CharSetBase;
    
  template
  <
    char T0=0, char T1=0, char T2=0, char T3=0, char T4=0, 
    char T5=0, char T6=0, char T7=0, char T8=0, char T9=0, 
    char T10=0, char T11=0, char T12=0, char T13=0, char T14=0, 
    char T15=0, char T16=0, char T17=0, char T18=0, char T19=0, 
    char T20=0, char T21=0, char T22=0, char T23=0, char T24=0, 
    char T25=0, char T26=0, char T27=0, char T28=0, char T29=0, 
    char T30=0, char T31=0
  >
  struct CharSet : CharSetBase
  {
    CharSet() : CharSetBase() {
      a[T0]=true; a[T1]=true; a[T2]=true; a[T3]=true; a[T4]=true; 
      a[T5]=true; a[T6]=true; a[T7]=true; a[T8]=true; a[T9]=true; 
      a[T10]=true; a[T11]=true; a[T12]=true; a[T13]=true; a[T14]=true; 
      a[T15]=true; a[T16]=true; a[T17]=true; a[T18]=true; a[T19]=true; 
      a[T20]=true; a[T21]=true; a[T22]=true; a[T23]=true; a[T24]=true; 
      a[T25]=true; a[T26]=true; a[T27]=true; a[T28]=true; a[T29]=true; 
      a[T30]=true; a[T31]=true;                  
      a[0] = false; // ?? 
    }
  };

  template
  <
    char T0, char T1
  >
  struct CharSetRange : CharSetBase
  {
    CharSetRange() : CharSetBase() {
      for (char c=T0; c <= T1; c++) {
        a[c] = true;
      }
    }
  };

  template
  <
    typename T0, typename T1
  >
  struct CharSetUnion : CharSetBase 
  {
    CharSetUnion() : CharSetBase() {
      const T0 x0;
      const T1 x1;
      for (int i=0; i<size; i++) {
        a[i] = x0[i] || x1[i];
      }
    }
  };

  template
  <
    typename T0, typename T1
  >
  struct CharSetIntersection : CharSetBase 
  {
    CharSetIntersection() : CharSetBase() {
      const T0 x0;
      const T1 x1;
      for (int i=0; i<size; i++) {
        a[i] = x0[i] && x1[i];
      }
    }
  };
  
  template
  <
    typename T
  >
  struct CharSetNot : CharSetBase 
  {
    CharSetNot() : CharSetBase() {
      const T x;
      for (int i=0; i<size; i++) {
        a[i] = !x[i];
      }
    }
  };

  typedef CharSetRange<'a', 'z'> 
	  LowerCaseLetterCharSet;    

  typedef CharSetRange<'A', 'Z'> 
	  UpperCaseLetterCharSet;    

  typedef CharSetUnion<LowerCaseLetterCharSet, UpperCaseLetterCharSet> 
	  LetterCharSet;  

  typedef CharSetRange<'0', '9'> 
	  DigitCharSet;

  typedef CharSetRange<'0', '7'> 
	  OctDigitCharSet;

  typedef CharSetUnion<DigitCharSet,CharSetUnion<CharSetRange<'a', 'f'>, CharSetRange<'A', 'F'> > > 
	  HexDigitCharSet;    

  typedef CharSetUnion<LetterCharSet, DigitCharSet> 
	  AlphaNumCharSet;    

  typedef CharSetUnion<LetterCharSet, CharSet<'_'> > 
	  IdentFirstCharSet;  
  
  typedef CharSetUnion<LetterCharSet, CharSet<'\''> > 
	  WordLetterCharSet;

  typedef CharSetUnion<IdentFirstCharSet, DigitCharSet> 
	  IdentNextCharSet;

  typedef CharSet<' ','\t','\n','\r'> 
	  WhiteSpaceCharSet;
}

#endif // #ifndef YARD_CHAR_SET_HPP

