// Public domain, by Christopher Diggins
// http://www.cdiggins.com

#ifndef OOTL_HASH_HPP
#define OOTL_HASH_HPP

#include "ootl_stack.hpp"

namespace ootl
{

typedef unsigned char u1;
typedef unsigned short u2;
typedef unsigned long u4;

#define get16bits(d) (*((const u2*)(d)))

// Paul Hseih's hash algorithm
// from http://www.azillionmonkeys.com/qed/hash.html

u4 hseih_hash(const char* key, u4 len) 
{  
  u4 hash = len;
  u4 tmp;
  int rem;
  if (len <= 0 || key == NULL) return 0;
  rem = len & 3;
  len >>= 2;

  // Main loop 
  for (;len > 0; --len) {
      hash  += get16bits (key);
      tmp    = (get16bits (key+2) << 11) ^ hash;
      hash   = (hash << 16) ^ tmp;
      key  += 4;
      hash  += hash >> 11;
  }

  // Handle end cases 
  switch (rem) {
      case 3: hash += get16bits (key);
              hash ^= hash << 16;
              hash ^= key[2] << 18;
              hash += hash >> 11;
              break;
      case 2: hash += get16bits(key);
              hash ^= hash << 11;
              hash += hash >> 17;
              break;
      case 1: hash += *key;
              hash ^= hash << 10;
              hash += hash >> 1;
  }

  // Force "avalanching" of final bits 
  hash ^= hash << 3;
  hash += hash >> 5;
  hash ^= hash << 2;
  hash += hash >> 15;
  hash ^= hash << 10;
  
  return hash;
}

#undef get16bits

	template<typename T>
	struct hasher {
	  u4 operator()(const T& x) const { 
		return hseih_hash(reinterpret_cast<const char*>(&x), sizeof(T));
	  }  
	};

	template<>
	struct hasher<const char*> {
	  u4 operator()(const char* x) const { 
		return hseih_hash(x, (u4)strlen(x));
	  }  
	};

	template<typename first_T, typename second_T>
	struct pair 
	{
		first_T mFirst;
		second_T mSecond;

		pair(const first_T& f, const second_T& s)
			: mFirst(f), mSecond(s)
		{ }
	};

	template<typename key_T, typename value_T, typename hash_T = hasher<key_T> >
	struct hash_map : vlist<pair<key_T, value_T> >
	{
		typedef pair<key_T, value_T> hash_pair;

		stack<hash_pair> mData;
		size_t mnCount;
		key_T unused_key;

		hash_map() : mnCount(0), vlist(), unused_key()
		{
		}
	
		hash_pair* find_slot(u4 hash_code, const key_T& key, buffer* buff)
		{
			if (buff == NULL) 
				throw new std::exception("could not find key");
			
			// try first hash-result 
			size_t nIndex = hash_code % buff->size;
			hash_pair* ret = &(buff->begin[nIndex]);
			if (ret->mFirst == key) 
				return ret;
			
			// quadratic probing (x+1, x+5, x+14, x+30, x+55, ...)
			size_t nStep = 1;
			while (ret->mFirst != unused_key) 
			{
				nIndex = (nIndex + (nStep * nStep)) % buff->size;
				ret = &(buff->begin[nIndex]);		
				if (ret->mFirst == key) 
					return ret;

				// TEMP: this assertion is just for testing.
				//assert(nStep <= 5 && "too many collisions");

				++nStep;
			}
			
			// recursive call
			ret = find_slot(hash_code, key, buff->prev);
			assert(ret->mFirst == key);
			return ret;
		}	

		bool saturated()
		{
			if (get_last_buffer() == NULL)
				return true;
			return (get_last_buffer()->size / 5 * 3) < mnCount;
		}

		void add(const key_T& k, const value_T& v)
		{
			assert(k != unused_key);
			++mnCount;
			if (saturated())
			{
				mnCount = 1;
				add_buffer();
			}
			hash_pair* tmp = find_slot(hash_T()(k), unused_key, get_last_buffer());
			assert(tmp->mFirst == unused_key);
			tmp->mFirst = k; 
			tmp->mSecond = v;
			//assert(operator[](k) == v);			
		}
		
		u4 hash(const key_T& key)
		{
			static hash_T hasher;
			return hasher(key);
		}

		value_T& operator[](const key_T& key)
		{
			return find_slot(hash(key), key, get_last_buffer())->mSecond;
		}
	};
}

#endif
