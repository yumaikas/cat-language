// Public Domain
// by Christopher Diggins 
// http://www.ootl.org

#ifndef OOTL_TEST_HPP
#define OOTL_TEST_HPP

#include <iostream>

void TestFailed(const char* text) {
  std::cerr << "failed : " << text << std::endl;
}

void TestPassed(const char* text) {
  std::cerr << "passed : " << text << std::endl;
}

#define OOTL_TEST(TOKEN) try { if (!(TOKEN)) { TestFailed(#TOKEN); } else \
  { TestPassed(#TOKEN); } } catch(...) { TestFailed(#TOKEN); }

#endif 
