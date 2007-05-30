// Public Domain by Christopher Diggins 
// http://www.ootl.org 

#ifndef OOTL_TIMER_HPP
#define OOTL_TIMER_HPP

// for clock() and CLOCKS_PER_SEC
#include <time.h>

namespace ootl
{  

struct second_timer {
  second_timer() : accumulate(0.0), cnt(0), last(click()) {
  }

  void start() {
    last = click();  
  }
  
  void stop() {
    accumulate += last_elapsed();
    ++cnt;
  }
  
  int count() {
    return cnt;
  }
  
  void pause() {
    accumulate += last_elapsed();
  }
  
  void resume() {
    last = click();  
  }
  
  double total_elapsed() {
    return last_elapsed() + accumulate;
  }
  
  double avg_elapsed() {
    return total_elapsed() / count();
  }
  
  double last_elapsed() {
    return click() - last;
  }
  
  double clear() {
    accumulate = 0.0;
    cnt = 0;
  }
  
  double click() {
    return (double)clock() / (double)CLOCKS_PER_SEC;
  }
  
private:
  double last;
  double accumulate;
  int cnt;    
};

} 

#endif
    

            
