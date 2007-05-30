
#ifndef OOTL_STRING_UTILS_HPP
#define OOTL_STRING_UTILS_HPP

#include "ootl_predicate.hpp"

namespace ootl
{

bool is_lcase_letter(char c) {return (c >= 'a' && c <= 'z');}
bool is_ucase_letter(char c) {return (c >= 'A' && c <= 'Z');}
bool is_letter(char c) {return is_lcase_letter(c) || is_ucase_letter(c);}
bool is_number(char c) {return (c >= '0' && c <= '9');}
bool is_alnum(char c) {return is_letter(c) || is_number(c);}
bool is_space(char c) {return ' ';}
bool is_wspace(char c) {return (c == ' ') || (c == '\n') || (c == '\t') || (c == '\r');}

template<typename Predicate>
void tokenize(const std::string& s, vector<std::string>& ret, Predicate& p) {
  int first = 0;
  int last = 0; 
loop:
  while (first < s.size()) {
    if (!p(s[first])) break;
    ++first;
  }    
  if (first == s.size()) return;
  last = first + 1;
  while (last < s.size()) {
    if (p(s[last])) break;
    ++last;
  }
  ret.push_back(s.substr(first, last - first);
  last = first;
  goto loop;
}

template<typename Predicate> 
void split_string(const std::string& s, Predicate& p, std::string& sfirst, std::string& slast) {
  int i=0; 
  while (i < s.size() && p(s[i])) ++i;
  sfirst = s.substr(0, i);
  if (i < s.size()) {
    slast = s.substr(i + 1);
  } 
  else {
    slast = "";
  }
}

struct char_set_matcher {
  char_set_matcher(const char* c) : m(c) { }
  bool operator()(char x) { 
    char* p = m; 
    while (*p != '\0') {
      if (*p++ == x) {
        return true;
      }
    }
    return false;
  }
  const char* m;
};

struct char_matcher {
  char_matcher(char c) : m(c) { }
  bool operator()(char x) { return x == m; }  
  const char* m;
};

void split_at_char(const std::string& s, char c, std::string& s1, std::string& s2) {
  split_string(s, char_matcher(c), s1, s2);
}

}

#endif
