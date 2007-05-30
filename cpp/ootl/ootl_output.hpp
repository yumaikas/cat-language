// Public domain, by Christopher Diggins
// http://www.cdiggins.com

#ifndef OOTL_OUTPUT_HPP
#define OOTL_OUTPUT_HPP

#include <ostream>

#include "ootl_iterators.hpp"
#include "ootl_procedure.hpp"

namespace ootl
{

void write(const char* x) {
  printf("%s", x);
}

void write(int x) {
  printf("%n", x);
}

void write(char x) {
  putchar(x);
}

void write(double x) {
  printf("%d", x);
}

void write(bool b) {
  if (b) {
    printf("true");
  }
  else {
    printf("false");
  }
}

template<typename T>
void writeln(T x) {
  write(x);
  write("\n");
}

void writeln() {
  write("\n");
}

template<typename InputIterator>
void output_iterator_range(InputIterator i, InputIterator j, std::ostream& o = std::cout, const char* delim = " ") {
  copy(i, j, output_iter(o, delim));
}

template<typename Iterable>
void output(Iterable i, std::ostream& o = std::cout, const char* delim = " ") {
  i.foreach(output_proc(o, delim));
}

}

#endif
