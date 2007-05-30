// Public Domain 
// by Christopher Diggins 
// http://www.ootl.org 

#ifndef OOTL_MISC_HPP
#define OOTL_MISC_HPP

#include <stdexcept> 
#include <typeinfo>
#include <cassert>

#include <sys/stat.h>
#include <sys/types.h>

namespace ootl
{

// useful constants
const double pi = 3.14159265358979;

const int thousand = 1000;
const int million = thousand * thousand;
const int billion = thousand * million;  
const int kb = 1024;
const int mb = kb * kb;
const int gb = kb * mb;    

// type shortcuts  
typedef std::runtime_error E;
typedef std::type_info TI;

char sErrMsgBuf[255];

void err(const std::string& x) {
  int i = 0;
  while ((i < 254) && (i < static_cast<int>(x.size()))) {
    sErrMsgBuf[i] = x[i++];      
  }
  sErrMsgBuf[i] = '\0';
  throw E(sErrMsgBuf); 
}

int file_size(const std::string& file_name) {
  struct _stat tmp;
  if (_stat(file_name.c_str(), &tmp ) != 0) {
    return 0;
  }  
  return tmp.st_size;
}

bool file_exists(const std::string& file_name) {
  return file_size(file_name) > 0;
}

char* alloc_from_file(const std::string& file_name)
{
  int nSize = file_size(file_name);
  if (nSize <= 0) {
    err("failed to compute file size for " + file_name);
  }
  FILE* f = fopen(file_name.c_str(), "rt");
  if (f == NULL) {
    err("failed to open file");
  } 
  char* ret = NULL;     
  try {
    ret = static_cast<char*>(calloc(nSize + 1, 1));
    if (ret == NULL) {
      err("failed to allocate buffer");
    }
    if (fread(ret, 1, nSize, f) <= 0) {        
      err("failed to read data from file");
    }
    fclose(f);
    return ret;
  }
  catch(...) {
    free(ret);
    fclose(f);
    throw;
  }    
  assert(false && "should be unreachable");   
  return NULL;
} 

template<typename T, typename U>
struct types_equal {
  static const bool result = false;
};

template<typename T>
struct types_equal<T, T> {
  static const bool result = true;
};

template<typename T, typename U>
struct value_types_equal {
  static const bool result = types_equal<typename T::value_type, typename U::value_type>::result;
};

bool is_power_of_two(int n) {
  if (n == 0) return true;
  if (n < 0) n = -n;
  // scan bits
  while (n > 0) {
    // n is a power of two iff it has exactly one or zero bits set
    if (n & 1) return n == 1;
    n >>= 1;
  }    
  assert(false); 
  return false;
}

template<typename T>
const T& min(const T& x, const T& y) {
  return x < y ? x : y;
}

template<typename T>
const T& max(const T& x, const T& y) {
  return x > y ? x : y;
}

template<typename T>
T div_round_up(const T& x, const T& y) {
  return x / y + (T)(x % y > 0);
}

char random_char_from(const char* x) {
  return *(x + rand() % strlen(x));
}

template<typename T, typename U>
struct pair {  
  pair() : first(), second() { }
  pair(const pair& x) : first(x.first), second(x.second) { }
  pair(const T& x, const U& y) : first(x), second(y) { }
  T first;
  U second;
};

template<typename T, typename U> 
pair<T, U> make_pair(const T& x, const U& y) {
  return pair<T, U>(x, y);
}

////////////////////////////////////////////////////////////////////////////////////
// Stack specific algorithms

template<typename Iter, typename Stack>
void copy_iter_range_to_stack(Iter i, Iter j, Stack& x) {
  while (i != j) {
    x.push(*i++);
  }
}

template<typename Stack>
void copy_stream_to_stack(std::istream& in, Stack& x) {
  typename Stack::value_type y;
  in >> y;
  while (!in.eof()) {
    x.push(y);
    in >> y;
  }
}


}
    
#endif
