void _apply();
void _apply2();
void _dip();
void _dip2();
void _b();
void _c();
void _d();
void _i();
void _k();
void _ki();
void _l();
void _m();
void _o();
void _r();
void _s();
void _t();
void _u();
void _v();
void _w();
void _y();
void _and();
void _nand();
void _nor();
void _not();
void _or();
void _eqz();
void _eqf();
void _neq();
void _neqf();
void _neqz();
void _curry();
void _curry2();
void _rcompose();
void _rcurry();
void _for();
void _for__each();
void _repeat();
void _rfor();
void _while();
void _whilen();
void _whilene();
void _whilenz();
void _cat();
void _consd();
void _count();
void _count__while();
void _drop();
void _drop__while();
void _empty();
void _filter();
void _first();
void _flatten();
void _fold();
void _gen();
void _head();
void _last();
void _map();
void _mid();
void _move__head();
void _n();
void _nth();
void _pair();
void _rev();
void _rmap();
void _set__at();
void _small();
void _split();
void _split__at();
void _swons();
void _tail();
void _take();
void _take__while();
void _unfold();
void _unpair();
void _unit();
void _bury();
void _dig();
void _dup2();
void _dupd();
void _over();
void _peek();
void _poke();
void _pop2();
void _popd();
void _swap2();
void _swapd();
void _under();
void _dec();
void _inc();
void _sub__int();
void _min__int();
void _max__int();
void _gt__int();
void _gteq__int();
void _lteq__int();
void _test();
void _run__tests();
void _cat_anon0();
void _cat_anon1();
void _cat_anon2();
void _cat_anon3();
void _cat_anon4();
void _cat_anon5();
void _cat_anon6();
void _cat_anon7();
void _cat_anon8();
void _cat_anon9();
void _cat_anon10();
void _cat_anon11();
void _cat_anon12();
void _cat_anon13();
void _cat_anon14();
void _cat_anon15();
void _cat_anon16();
void _cat_anon17();
void _cat_anon18();
void _cat_anon19();
void _cat_anon20();
void _cat_anon21();
void _cat_anon22();
void _cat_anon23();
void _cat_anon24();
void _cat_anon25();
void _cat_anon26();
void _cat_anon27();
void _cat_anon28();
void _cat_anon29();
void _cat_anon30();
void _cat_anon31();
void _cat_anon32();
void _cat_anon33();
void _cat_anon34();
void _cat_anon35();
void _cat_anon36();
void _cat_anon37();
void _cat_anon38();
void _cat_anon39();
void _cat_anon40();
void _cat_anon41();
void _cat_anon42();
void _cat_anon43();
void _cat_anon44();
void _cat_anon45();
void _cat_anon46();
void _cat_anon47();
void _cat_anon48();
void _cat_anon49();
void _cat_anon50();
void _cat_anon51();
void _cat_anon52();
void _cat_anon53();
void _cat_anon54();
void _cat_anon55();
void _cat_anon56();
void _cat_anon57();
void _cat_anon58();
void _cat_anon59();
void _cat_anon60();
void _cat_anon61();
void _cat_anon62();
void _cat_anon63();
void _cat_anon64();
void _cat_anon65();
void _cat_anon66();
void _cat_anon67();
void _cat_anon68();
void _cat_anon69();
void _cat_anon70();
void _cat_anon71();
void _cat_anon72();
void _cat_anon73();
void _cat_anon74();
void _cat_anon75();
void _cat_anon76();
void _cat_anon77();
void _cat_anon78();
void _cat_anon79();
void _cat_anon80();
void _cat_anon81();
void _cat_anon82();
void _cat_anon83();
void _cat_anon84();
void _cat_anon85();
void _cat_anon86();
void _cat_anon87();
void _cat_anon88();
void _cat_anon89();
void _cat_anon90();
void _cat_anon91();
void _cat_anon92();
void _cat_anon93();
void _cat_anon94();
void _cat_anon95();
void _cat_anon96();
void _cat_anon97();
void _cat_anon98();
void _cat_anon99();
void _cat_anon100();
void _cat_anon101();
void _cat_anon102();
void _cat_anon103();
void _cat_anon104();
void _cat_anon105();
void _cat_anon106();
void _cat_anon107();
void _cat_anon108();
void _cat_anon109();
void _cat_anon110();
void _cat_anon111();
void _cat_anon112();
void _cat_anon113();
void _cat_anon114();
void _cat_anon115();
void _cat_anon116();
void _cat_anon117();
void _cat_anon118();
void _cat_anon119();
void _cat_anon120();
void _cat_anon121();
void _cat_anon122();
void _cat_anon123();
void _cat_anon124();
void _cat_anon125();
void _cat_anon126();
void _cat_anon127();
void _cat_anon128();
void _cat_anon129();
void _cat_anon130();
void _cat_anon131();
void _cat_anon132();
void _cat_anon133();
void _cat_anon134();
void _cat_anon135();
void _cat_anon136();
void _cat_anon137();
void _cat_anon138();
void _cat_anon139();
void _cat_anon140();
void _cat_anon141();
void _cat_anon142();
void _cat_anon143();
void _cat_anon144();
void _cat_anon145();
void _cat_anon146();
void _cat_anon147();
void _apply()
{
    call(_true);
    call(_swap);
    push_function(_cat_anon0); //[]
    call(_if);
}
void _apply2()
{
    call(_under);
    call(_apply);
    push_function(_cat_anon1); //[apply]
    call(_dip);
}
void _dip()
{
    call(_swap);
    call(_quote);
    call(_compose);
    call(_apply);
}
void _dip2()
{
    call(_swap);
    push_function(_cat_anon2); //[dip]
    call(_dip);
}
void _b()
{
    push_function(_cat_anon3); //[k]
    push_function(_cat_anon5); //[[s] k]
    call(_s);
}
void _c()
{
    push_function(_cat_anon7); //[[k] k]
    push_function(_cat_anon10); //[[s] [b] b]
    call(_s);
}
void _d()
{
    push_function(_cat_anon11); //[b]
    call(_b);
}
void _i()
{
    push_function(_cat_anon12); //[k]
    push_function(_cat_anon13); //[k]
    call(_s);
}
void _k()
{
    push_function(_cat_anon14); //[pop]
    call(_dip);
}
void _ki()
{
    push_function(_cat_anon15); //[i]
    call(_k);
}
void _l()
{
    push_function(_cat_anon16); //[m]
    push_function(_cat_anon17); //[b]
    call(_c);
}
void _m()
{
    call(_dup);
    call(_apply);
}
void _o()
{
    push_function(_cat_anon18); //[i]
    call(_s);
}
void _r()
{
    push_function(_cat_anon19); //[t]
    push_function(_cat_anon20); //[b]
    call(_b);
}
void _s()
{
    call(_peek);
    call(_swap);
    push_function(_cat_anon21); //[curry]
    call(_dip2);
    call(_apply);
}
void _t()
{
    push_function(_cat_anon22); //[i]
    call(_c);
}
void _u()
{
    push_function(_cat_anon23); //[o]
    call(_l);
}
void _v()
{
    push_function(_cat_anon24); //[t]
    push_function(_cat_anon25); //[c]
    call(_b);
}
void _w()
{
    push_function(_cat_anon28); //[[r] [m] b]
    call(_c);
}
void _y()
{
    call(_dup);
    call(_quote);
    push_function(_cat_anon29); //[y]
    call(_compose);
    call(_swap);
    call(_apply);
}
void _and()
{
    call(_quote);
    push_function(_cat_anon30); //[false]
    call(_if);
}
void _nand()
{
    call(_and);
    call(_not);
}
void _nor()
{
    call(_or);
    call(_not);
}
void _not()
{
    push_function(_cat_anon31); //[false]
    push_function(_cat_anon32); //[true]
    call(_if);
}
void _or()
{
    push_function(_cat_anon33); //[true]
    call(_swap);
    call(_quote);
    call(_if);
}
void _eqz()
{
    call(_dup);
    push_literal(0 );
    call(_eq);
}
void _eqf()
{
    push_function(_cat_anon34); //[dupd eq]
    call(_curry);
}
void _neq()
{
    call(_eq);
    call(_not);
}
void _neqf()
{
    push_function(_cat_anon35); //[dupd neq]
    call(_curry);
}
void _neqz()
{
    call(_dup);
    push_literal(0 );
    call(_neq);
}
void _curry()
{
    push_function(_cat_anon36); //[quote]
    call(_dip);
    call(_compose);
}
void _curry2()
{
    call(_curry);
    call(_curry);
}
void _rcompose()
{
    call(_swap);
    call(_compose);
}
void _rcurry()
{
    call(_curry);
    call(_swap);
}
void _for()
{
    call(_swap);
    push_function(_cat_anon37); //[dip inc]
    call(_curry);
    push_function(_cat_anon38); //[dup]
    call(_rcompose);
    call(_swap);
    call(_neqf);
    push_literal(0 );
    call(_bury);
    call(_while);
    call(_pop);
}
void _for__each()
{
    push_function(_cat_anon39); //[uncons swap]
    call(_rcompose);
    push_function(_cat_anon40); //[dip]
    call(_curry);
    push_function(_cat_anon41); //[empty not]
    call(_while);
    call(_pop);
}
void _repeat()
{
    call(_swap);
    push_function(_cat_anon42); //[dip dec]
    call(_curry);
    push_function(_cat_anon43); //[neqz]
    call(_while);
    call(_pop);
}
void _rfor()
{
    call(_swap);
    push_function(_cat_anon44); //[dip dec]
    call(_curry);
    push_function(_cat_anon45); //[dup]
    call(_rcompose);
    call(_whilenz);
    call(_pop);
}
void _while()
{
    push_function(_cat_anon46); //[dip swap]
    call(_curry);
    call(_swap);
    push_function(_cat_anon47); //[dip m]
    call(_curry);
    call(_quote);
    call(_compose);
    push_function(_cat_anon49); //[[pop] if]
    call(_compose);
    call(_m);
}
void _whilen()
{
    push_function(_cat_anon50); //[not]
    call(_compose);
    call(_while);
}
void _whilene()
{
    push_function(_cat_anon51); //[empty not]
    call(_while);
}
void _whilenz()
{
    push_function(_cat_anon52); //[neqz]
    call(_while);
}
void _cat()
{
    call(_rev);
    push_function(_cat_anon53); //[cons]
    call(_fold);
}
void _consd()
{
    push_function(_cat_anon54); //[cons]
    call(_dip);
}
void _count()
{
    push_literal(0 );
    push_function(_cat_anon55); //[pop inc]
    call(_fold);
}
void _count__while()
{
    call(_dupd);
    push_literal(0 );
    call(_bury);
    push_function(_cat_anon57); //[[inc] dip]
    call(_swap);
    push_function(_cat_anon58); //[uncons]
    call(_rcompose);
    call(_quote);
    push_function(_cat_anon59); //[empty not]
    call(_rcompose);
    push_function(_cat_anon61); //[[false] if]
    call(_compose);
}
void _drop()
{
    push_function(_cat_anon63); //[[tail] dip dec]
    call(_whilenz);
    call(_pop);
}
void _drop__while()
{
    call(_if);
    call(_dupd);
    call(_count__while);
    call(_drop);
}
void _empty()
{
    call(_count);
    push_literal(0 );
    call(_eq);
}
void _filter()
{
    push_function(_cat_anon64); //[rev]
    call(_dip);
    push_function(_cat_anon67); //[[cons] [] if]
    call(_compose);
    push_function(_cat_anon68); //[dup]
    call(_rcompose);
    call(_nil);
    call(_swap);
    call(_fold);
}
void _first()
{
    call(_dup);
    call(_uncons);
    call(_popd);
}
void _flatten()
{
    call(_nil);
    push_function(_cat_anon69); //[cat]
    call(_fold);
}
void _fold()
{
    call(_swapd);
    push_function(_cat_anon70); //[dip]
    call(_curry);
    push_function(_cat_anon71); //[uncons swap]
    call(_rcompose);
    call(_whilene);
    call(_pop);
}
void _gen()
{
    call(_nil);
    call(_bury);
    push_function(_cat_anon73); //[[cons] dip rcompose]
    call(_dip);
    push_function(_cat_anon74); //[dup]
    call(_rcompose);
}
void _head()
{
    call(_uncons);
    call(_popd);
}
void _last()
{
    call(_count);
    call(_dec);
    call(_nth);
}
void _map()
{
    call(_rmap);
    call(_rev);
}
void _mid()
{
    call(_count);
    push_literal(2 );
    call(_div__int);
    call(_nth);
}
void _move__head()
{
    call(_uncons);
    call(_swap);
    call(_cons);
}
void _n()
{
    call(_nil);
    call(_swap);
    call(_dup);
    push_function(_cat_anon75); //[dec cons]
    call(_swap);
    call(_repeat);
}
void _nth()
{
    call(_dupd);
    call(_take);
    call(_head);
}
void _pair()
{
    push_function(_cat_anon76); //[unit]
    call(_dip);
    call(_cons);
}
void _rev()
{
    call(_nil);
    push_function(_cat_anon77); //[cons]
    call(_fold);
}
void _rmap()
{
    call(_nil);
    push_function(_cat_anon78); //[cons]
    call(_fold);
}
void _set__at()
{
    call(_swapd);
    call(_split__at);
    push_function(_cat_anon79); //[tail swons]
    call(_dip);
    call(_cat);
}
void _small()
{
    call(_count);
    push_literal(1 );
    call(_lteq__int);
}
void _split()
{
    call(_dup2);
    push_function(_cat_anon80); //[filter]
    call(_dip2);
    push_function(_cat_anon81); //[not]
    call(_compose);
    call(_filter);
}
void _split__at()
{
    call(_nil);
    call(_bury);
    push_function(_cat_anon82); //[move_head]
    call(_swap);
    call(_repeat);
}
void _swons()
{
    call(_swap);
    call(_cons);
}
void _tail()
{
    call(_uncons);
    call(_pop);
}
void _take()
{
    call(_nil);
    call(_bury);
    push_function(_cat_anon84); //[[move_head] dip dec]
    call(_whilenz);
    call(_pop);
    call(_rev);
}
void _take__while()
{
    call(_dupd);
    call(_count__while);
    call(_take);
}
void _unfold()
{
    push_function(_cat_anon86); //[nil bury [consd] compose]
    call(_dip);
    call(_whilen);
}
void _unpair()
{
    call(_uncons);
    push_function(_cat_anon87); //[head]
    call(_dip);
}
void _unit()
{
    call(_nil);
    call(_swap);
    call(_cons);
}
void _bury()
{
    call(_swap);
    call(_swapd);
}
void _dig()
{
    call(_swapd);
    call(_swap);
}
void _dup2()
{
    call(_over);
    call(_over);
}
void _dupd()
{
    push_function(_cat_anon88); //[dup]
    call(_dip);
}
void _over()
{
    call(_dupd);
    call(_swap);
}
void _peek()
{
    push_function(_cat_anon89); //[dupd]
    call(_dip);
    call(_dig);
}
void _poke()
{
    push_function(_cat_anon90); //[popd]
    call(_dip);
    call(_swap);
}
void _pop2()
{
    call(_pop);
    call(_pop);
}
void _popd()
{
    push_function(_cat_anon91); //[pop]
    call(_dip);
}
void _swap2()
{
    push_function(_cat_anon92); //[bury]
    call(_dip);
    call(_bury);
}
void _swapd()
{
    push_function(_cat_anon93); //[swap]
    call(_dip);
}
void _under()
{
    call(_dup);
    call(_swapd);
}
void _dec()
{
    push_literal(1 );
    call(_sub__int);
}
void _inc()
{
    push_literal(1 );
    call(_add__int);
}
void _sub__int()
{
    call(_neg__int);
    call(_add__int);
}
void _min__int()
{
    call(_dup2);
    call(_gt__int);
    push_function(_cat_anon94); //[pop]
    push_function(_cat_anon95); //[popd]
    call(_if);
}
void _max__int()
{
    call(_dup2);
    call(_gt__int);
    push_function(_cat_anon96); //[popd]
    push_function(_cat_anon97); //[pop]
    call(_if);
}
void _gt__int()
{
    call(_lteq__int);
    call(_not);
}
void _gteq__int()
{
    call(_lt__int);
    call(_not);
}
void _lteq__int()
{
    call(_dup2);
    call(_eq);
    call(_lt__int);
    call(_or);
}
void _run__tests()
{
    push_function(_cat_anon100); //[1 2 add_int 3 eq]
    call(_test);
    push_function(_cat_anon103); //[[1] [inc] compose apply 2 eq]
    call(_test);
    push_function(_cat_anon104); //[nil 1 cons uncons swap pop 1 eq]
    call(_test);
    push_function(_cat_anon105); //[42 7 div_int 6 eq]
    call(_test);
    push_function(_cat_anon106); //[2 dup add_int 4 eq]
    call(_test);
    push_function(_cat_anon107); //[1 1 eq]
    call(_test);
    push_function(_cat_anon110); //[false [false] [true] if]
    call(_test);
    push_function(_cat_anon113); //[true [1] [2] if 1 eq]
    call(_test);
    push_function(_cat_anon114); //[3 5 lt_int]
    call(_test);
    push_function(_cat_anon115); //[5 3 mod_int 2 eq]
    call(_test);
    push_function(_cat_anon116); //[5 3 mul_int 15 eq]
    call(_test);
    push_function(_cat_anon117); //[5 neg_int -5 eq]
    call(_test);
    push_function(_cat_anon118); //[nil nil eq]
    call(_test);
    push_function(_cat_anon119); //[3 5 pop 3 eq]
    call(_test);
    push_function(_cat_anon120); //[true 1 quote 2 quote 1 eq]
    call(_test);
    push_function(_cat_anon121); //[1 2 swap pop 2 eq]
    call(_test);
    push_function(_cat_anon124); //[true [true] [false] if]
    call(_test);
    push_function(_cat_anon125); //[nil 2 cons 1 cons uncons pop uncons swap pop 2 eq]
    call(_test);
    push_function(_cat_anon127); //[[1] apply 1 eq]
    call(_test);
    push_function(_cat_anon129); //[1 3 [inc] apply2 pop 2 eq]
    call(_test);
    push_function(_cat_anon131); //[1 3 [inc] dip pop 2 eq]
    call(_test);
    push_function(_cat_anon133); //[1 3 5 [inc] dip2 pop pop 2 eq]
    call(_test);
    push_function(_cat_anon143); //[[42] i 42 eq]
    call(_test);
    push_function(_cat_anon144); //[true true and]
    call(_test);
    push_function(_cat_anon145); //[true false nand]
    call(_test);
    push_function(_cat_anon146); //[false false nor]
    call(_test);
    push_function(_cat_anon147); //[false not]
    call(_test);
}
void _cat_anon0()
{
}
void _cat_anon1()
{
    call(_apply);
}
void _cat_anon2()
{
    call(_dip);
}
void _cat_anon3()
{
    call(_k);
}
void _cat_anon4()
{
    call(_s);
}
void _cat_anon5()
{
    push_function(_cat_anon4); //[s]
    call(_k);
}
void _cat_anon6()
{
    call(_k);
}
void _cat_anon7()
{
    push_function(_cat_anon6); //[k]
    call(_k);
}
void _cat_anon8()
{
    call(_s);
}
void _cat_anon9()
{
    call(_b);
}
void _cat_anon10()
{
    push_function(_cat_anon8); //[s]
    push_function(_cat_anon9); //[b]
    call(_b);
}
void _cat_anon11()
{
    call(_b);
}
void _cat_anon12()
{
    call(_k);
}
void _cat_anon13()
{
    call(_k);
}
void _cat_anon14()
{
    call(_pop);
}
void _cat_anon15()
{
    call(_i);
}
void _cat_anon16()
{
    call(_m);
}
void _cat_anon17()
{
    call(_b);
}
void _cat_anon18()
{
    call(_i);
}
void _cat_anon19()
{
    call(_t);
}
void _cat_anon20()
{
    call(_b);
}
void _cat_anon21()
{
    call(_curry);
}
void _cat_anon22()
{
    call(_i);
}
void _cat_anon23()
{
    call(_o);
}
void _cat_anon24()
{
    call(_t);
}
void _cat_anon25()
{
    call(_c);
}
void _cat_anon26()
{
    call(_r);
}
void _cat_anon27()
{
    call(_m);
}
void _cat_anon28()
{
    push_function(_cat_anon26); //[r]
    push_function(_cat_anon27); //[m]
    call(_b);
}
void _cat_anon29()
{
    call(_y);
}
void _cat_anon30()
{
    call(_false);
}
void _cat_anon31()
{
    call(_false);
}
void _cat_anon32()
{
    call(_true);
}
void _cat_anon33()
{
    call(_true);
}
void _cat_anon34()
{
    call(_dupd);
    call(_eq);
}
void _cat_anon35()
{
    call(_dupd);
    call(_neq);
}
void _cat_anon36()
{
    call(_quote);
}
void _cat_anon37()
{
    call(_dip);
    call(_inc);
}
void _cat_anon38()
{
    call(_dup);
}
void _cat_anon39()
{
    call(_uncons);
    call(_swap);
}
void _cat_anon40()
{
    call(_dip);
}
void _cat_anon41()
{
    call(_empty);
    call(_not);
}
void _cat_anon42()
{
    call(_dip);
    call(_dec);
}
void _cat_anon43()
{
    call(_neqz);
}
void _cat_anon44()
{
    call(_dip);
    call(_dec);
}
void _cat_anon45()
{
    call(_dup);
}
void _cat_anon46()
{
    call(_dip);
    call(_swap);
}
void _cat_anon47()
{
    call(_dip);
    call(_m);
}
void _cat_anon48()
{
    call(_pop);
}
void _cat_anon49()
{
    push_function(_cat_anon48); //[pop]
    call(_if);
}
void _cat_anon50()
{
    call(_not);
}
void _cat_anon51()
{
    call(_empty);
    call(_not);
}
void _cat_anon52()
{
    call(_neqz);
}
void _cat_anon53()
{
    call(_cons);
}
void _cat_anon54()
{
    call(_cons);
}
void _cat_anon55()
{
    call(_pop);
    call(_inc);
}
void _cat_anon56()
{
    call(_inc);
}
void _cat_anon57()
{
    push_function(_cat_anon56); //[inc]
    call(_dip);
}
void _cat_anon58()
{
    call(_uncons);
}
void _cat_anon59()
{
    call(_empty);
    call(_not);
}
void _cat_anon60()
{
    call(_false);
}
void _cat_anon61()
{
    push_function(_cat_anon60); //[false]
    call(_if);
}
void _cat_anon62()
{
    call(_tail);
}
void _cat_anon63()
{
    push_function(_cat_anon62); //[tail]
    call(_dip);
    call(_dec);
}
void _cat_anon64()
{
    call(_rev);
}
void _cat_anon65()
{
    call(_cons);
}
void _cat_anon66()
{
}
void _cat_anon67()
{
    push_function(_cat_anon65); //[cons]
    push_function(_cat_anon66); //[]
    call(_if);
}
void _cat_anon68()
{
    call(_dup);
}
void _cat_anon69()
{
    call(_cat);
}
void _cat_anon70()
{
    call(_dip);
}
void _cat_anon71()
{
    call(_uncons);
    call(_swap);
}
void _cat_anon72()
{
    call(_cons);
}
void _cat_anon73()
{
    push_function(_cat_anon72); //[cons]
    call(_dip);
    call(_rcompose);
}
void _cat_anon74()
{
    call(_dup);
}
void _cat_anon75()
{
    call(_dec);
    call(_cons);
}
void _cat_anon76()
{
    call(_unit);
}
void _cat_anon77()
{
    call(_cons);
}
void _cat_anon78()
{
    call(_cons);
}
void _cat_anon79()
{
    call(_tail);
    call(_swons);
}
void _cat_anon80()
{
    call(_filter);
}
void _cat_anon81()
{
    call(_not);
}
void _cat_anon82()
{
    call(_move__head);
}
void _cat_anon83()
{
    call(_move__head);
}
void _cat_anon84()
{
    push_function(_cat_anon83); //[move_head]
    call(_dip);
    call(_dec);
}
void _cat_anon85()
{
    call(_consd);
}
void _cat_anon86()
{
    call(_nil);
    call(_bury);
    push_function(_cat_anon85); //[consd]
    call(_compose);
}
void _cat_anon87()
{
    call(_head);
}
void _cat_anon88()
{
    call(_dup);
}
void _cat_anon89()
{
    call(_dupd);
}
void _cat_anon90()
{
    call(_popd);
}
void _cat_anon91()
{
    call(_pop);
}
void _cat_anon92()
{
    call(_bury);
}
void _cat_anon93()
{
    call(_swap);
}
void _cat_anon94()
{
    call(_pop);
}
void _cat_anon95()
{
    call(_popd);
}
void _cat_anon96()
{
    call(_popd);
}
void _cat_anon97()
{
    call(_pop);
}
void _cat_anon98()
{
    push_literal(1 );
    call(_halt);
}
void _cat_anon99()
{
}
void _cat_anon100()
{
    push_literal(1 );
    push_literal(2 );
    call(_add__int);
    push_literal(3 );
    call(_eq);
}
void _cat_anon101()
{
    push_literal(1);
}
void _cat_anon102()
{
    call(_inc);
}
void _cat_anon103()
{
    push_function(_cat_anon101); //[1]
    push_function(_cat_anon102); //[inc]
    call(_compose);
    call(_apply);
    push_literal(2 );
    call(_eq);
}
void _cat_anon104()
{
    call(_nil);
    push_literal(1 );
    call(_cons);
    call(_uncons);
    call(_swap);
    call(_pop);
    push_literal(1 );
    call(_eq);
}
void _cat_anon105()
{
    push_literal(42 );
    push_literal(7 );
    call(_div__int);
    push_literal(6 );
    call(_eq);
}
void _cat_anon106()
{
    push_literal(2 );
    call(_dup);
    call(_add__int);
    push_literal(4 );
    call(_eq);
}
void _cat_anon107()
{
    push_literal(1 );
    push_literal(1 );
    call(_eq);
}
void _cat_anon108()
{
    call(_false);
}
void _cat_anon109()
{
    call(_true);
}
void _cat_anon110()
{
    call(_false);
    push_function(_cat_anon108); //[false]
    push_function(_cat_anon109); //[true]
    call(_if);
}
void _cat_anon111()
{
    push_literal(1);
}
void _cat_anon112()
{
    push_literal(2);
}
void _cat_anon113()
{
    call(_true);
    push_function(_cat_anon111); //[1]
    push_function(_cat_anon112); //[2]
    call(_if);
    push_literal(1 );
    call(_eq);
}
void _cat_anon114()
{
    push_literal(3 );
    push_literal(5 );
    call(_lt__int);
}
void _cat_anon115()
{
    push_literal(5 );
    push_literal(3 );
    call(_mod__int);
    push_literal(2 );
    call(_eq);
}
void _cat_anon116()
{
    push_literal(5 );
    push_literal(3 );
    call(_mul__int);
    push_literal(15 );
    call(_eq);
}
void _cat_anon117()
{
    push_literal(5 );
    call(_neg__int);
    push_literal(-5 );
    call(_eq);
}
void _cat_anon118()
{
    call(_nil);
    call(_nil);
    call(_eq);
}
void _cat_anon119()
{
    push_literal(3 );
    push_literal(5 );
    call(_pop);
    push_literal(3 );
    call(_eq);
}
void _cat_anon120()
{
    call(_true);
    push_literal(1 );
    call(_quote);
    push_literal(2 );
    call(_quote);
	call(_if);
    push_literal(1 );
    call(_eq);
}
void _cat_anon121()
{
    push_literal(1 );
    push_literal(2 );
    call(_swap);
    call(_pop);
    push_literal(2 );
    call(_eq);
}
void _cat_anon122()
{
    call(_true);
}
void _cat_anon123()
{
    call(_false);
}
void _cat_anon124()
{
    call(_true);
    push_function(_cat_anon122); //[true]
    push_function(_cat_anon123); //[false]
    call(_if);
}
void _cat_anon125()
{
    call(_nil);
    push_literal(2 );
    call(_cons);
    push_literal(1 );
    call(_cons);
    call(_uncons);
    call(_pop);
    call(_uncons);
    call(_swap);
    call(_pop);
    push_literal(2 );
    call(_eq);
}
void _cat_anon126()
{
    push_literal(1);
}
void _cat_anon127()
{
    push_function(_cat_anon126); //[1]
    call(_apply);
    push_literal(1 );
    call(_eq);
}
void _cat_anon128()
{
    call(_inc);
}
void _cat_anon129()
{
    push_literal(1 );
    push_literal(3 );
    push_function(_cat_anon128); //[inc]
    call(_apply2);
    call(_pop);
    push_literal(2 );
    call(_eq);
}
void _cat_anon130()
{
    call(_inc);
}
void _cat_anon131()
{
    push_literal(1 );
    push_literal(3 );
    push_function(_cat_anon130); //[inc]
    call(_dip);
    call(_pop);
    push_literal(2 );
    call(_eq);
}
void _cat_anon132()
{
    call(_inc);
}
void _cat_anon133()
{
    push_literal(1 );
    push_literal(3 );
    push_literal(5 );
    push_function(_cat_anon132); //[inc]
    call(_dip2);
    call(_pop);
    call(_pop);
    push_literal(2 );
    call(_eq);
}
void _cat_anon134()
{
    push_literal(1);
}
void _cat_anon135()
{
    call(_apply);
    push_literal(2 );
    call(_mul__int);
}
void _cat_anon136()
{
    call(_apply);
}
void _cat_anon137()
{
    push_function(_cat_anon134); //[1]
    push_function(_cat_anon135); //[apply 2 mul_int]
    push_function(_cat_anon136); //[apply]
    call(_b);
    push_literal(2 );
    call(_eq);
}
void _cat_anon138()
{
    call(_dup);
}
void _cat_anon139()
{
    push_literal(12);
}
void _cat_anon140()
{
    call(_pop);
}
void _cat_anon141()
{
    push_function(_cat_anon138); //[dup]
    push_function(_cat_anon139); //[12]
    push_function(_cat_anon140); //[pop]
    call(_c);
    call(_apply);
    push_literal(12 );
    call(_eq);
}
void _cat_anon142()
{
    push_literal(42);
}
void _cat_anon143()
{
    push_function(_cat_anon142); //[42]
    call(_i);
    push_literal(42 );
    call(_eq);
}
void _cat_anon144()
{
    call(_true);
    call(_true);
    call(_and);
}
void _cat_anon145()
{
    call(_true);
    call(_false);
    call(_nand);
}
void _cat_anon146()
{
    call(_false);
    call(_false);
    call(_nor);
}
void _cat_anon147()
{
    call(_false);
    call(_not);
}
