:NONAME GET-CURRENT WORDLIST DUP SET-CURRENT
>R GET-ORDER 1+ R> SWAP SET-ORDER ;
DUP EXECUTE SWAP DEFER __NM-BEGIN IS __NM-BEGIN
32 CELLS CREATE __NM-STACK ALLOT
__NM-STACK ! __NM-STACK VALUE __NM-SPTR
: NM-BEGIN __NM-BEGIN __NM-SPTR CELL+ >R R@ ! R> TO __NM-SPTR ;
: NM-PUB __NM-SPTR >R R@ @ R> 1 CELLS - TO __NM-SPTR SET-CURRENT ;
: NM-END PREVIOUS ;

	VARIABLE T=NIL
	VARIABLE T=PAIR
	VARIABLE T=SYMBOL
	VARIABLE T=BOOL
	VARIABLE T=CHAR
	VARIABLE T=FIXNUM
	VARIABLE T=~FIXNUM
	VARIABLE T=~FLONUM
	VARIABLE T=PROC
	VARIABLE T=FPROC
	VARIABLE T=CONT
	VARIABLE T=VECTOR
	2 CELLS CONSTANT TSIZE

	: TVAL ( "<SPACES>NAME" -- )
		TSIZE CREATE ALLOT ;

	: TVAL-TAG ( TVAL -- TVAL-TAG )
		0 CELLS + ;

	: TVAL-VAL ( TVAL -- TVAL-VAL )
		1 CELLS + ;

	: ~TVAL ( TVAL -- )
		DUP ." TVAL( T=" @ . CELL+ ." D=" @ . ." )" ;

	: PUT-TVAL ( T V TVAL -- )
		DUP ROT SWAP CELL+ ! ! ;

	: COPY-TVAL ( SRC DST -- )
		SWAP DUP @ SWAP CELL+ @ ROT PUT-TVAL ;

	TVAL SYMBOLS
	TVAL NIL
	T=NIL 0 NIL PUT-TVAL
	NIL SYMBOLS COPY-TVAL

	: PUT-PAIR ( A B O -- )
		TVAL-VAL @ ROT OVER COPY-TVAL TSIZE + COPY-TVAL ;

	: CAR ( V -- CAR )
		CELL+ @ TSIZE 0 * + ;

	: CDR ( V -- CDR )
		CELL+ @ TSIZE 1 * + ;

	: EQ? ( A B -- FLAG )
		2DUP @ SWAP @ = ROT ROT CELL+ @ SWAP CELL+ @ = AND ;

	: CONS {: A B O | TMP -- :}
		HERE TSIZE ALLOT TO TMP
		TSIZE 2* ALLOCATE ABORT" ALLOC FAIL"
		T=PAIR SWAP TMP PUT-TVAL
		A B TMP PUT-PAIR
		TMP O COPY-TVAL
		TSIZE NEGATE ALLOT ;

	: MAKE-SYM {: C-ADDR LEN SYM | ISYMBOLS -- :}
		SYMBOLS TO ISYMBOLS
		BEGIN ISYMBOLS NIL EQ? 0= WHILE
			ISYMBOLS CAR TVAL-VAL @ CELL+
			ISYMBOLS CAR TVAL-VAL @ @
			C-ADDR LEN COMPARE 0=
			IF ISYMBOLS CAR SYM COPY-TVAL EXIT THEN
			ISYMBOLS CDR TO ISYMBOLS REPEAT
		LEN CELL+ ALLOCATE ABORT" ALLOC FAIL"
		DUP DUP LEN SWAP ! CELL+ C-ADDR SWAP LEN MOVE
		T=SYMBOL SWAP SYM PUT-TVAL
		SYM SYMBOLS SYMBOLS CONS ;

	NM-BEGIN

		TRUE VALUE READ?

		: AT-EOL ( -- FLAG )
			SOURCE SWAP DROP >IN @ = ;

		: IN-ADDR ( -- FLAG )
			SOURCE DROP >IN @ + ;

		: REFILL ( -- )
			REFILL TO READ? ;

		: >IN1+ ( -- )
			AT-EOL IF REFILL ELSE 1 >IN +! THEN ;

	NM-PUB

		-1 CONSTANT EOF
		10 CONSTANT EOL

		: PEEK ( -- C )
			READ? IF AT-EOL IF EOL ELSE IN-ADDR C@ THEN ELSE EOF THEN ;

		: GETC ( -- C )
			PEEK READ? IF >IN1+ ELSE TRUE TO READ? THEN ;

	NM-END

	: WHITESPACE? ( C -- FLAG )
		>R R@ 8 > R@ 14 < AND R> 32 = OR ;

	: DELIMITER? ( C -- FLAG )
		>R R@ WHITESPACE?
		[CHAR] ( R@ = OR
		[CHAR] ) R@ = OR
		[CHAR] " R@ = OR
		[CHAR] ; R@ = OR
		 EOF     R@ = OR
		R> DROP ;

	: LOWER ( C -- C )
		DUP [CHAR] A [CHAR] Z 1+ WITHIN
		IF [CHAR] a [CHAR] A - + THEN ;

	: UPPER ( C -- C )
		DUP [CHAR] a [CHAR] z 1+ WITHIN
		IF [CHAR] A [CHAR] a - + THEN ;

	3 CELLS CONSTANT VECSIZE

	: VEC-LEN ( VEC -- ADDR )
		CELL+ ;

	: VEC-CAP ( VEC -- ADDR )
		2 CELLS + ;

	: VEC-INIT ( VEC -- )
		DUP DUP 0 ALLOCATE ABORT" ALLOC FAIL"
		SWAP ! VEC-LEN 0 SWAP ! VEC-CAP 0 SWAP ! ;

	: VEC-DEINIT ( VEC -- )
		DUP DUP DUP @ FREE ABORT" FREE FAIL"
		0 SWAP ! VEC-LEN 0 SWAP ! VEC-CAP 0 SWAP ! ;

	: VEC-ELEM ( I VEC STRIDE -- ADDR )
		2>R R> * R> @ + ;

	: VEC-DATA ( VEC STRIDE -- ADDR )
		2>R 0 2R> VEC-ELEM ;

	: ~VEC ( VEC -- )
		DUP DUP ." VEC( ADDR=" @ .
		." LEN=" VEC-LEN @ .
		." CAP=" VEC-CAP @ . ." )" ;

	: VEC-SETCAP ( NEW-CAP VEC STRIDE -- )
		2>R DUP R> * R@ @
		SWAP RESIZE ABORT" ALLOC FAIL"
		R@ ! R> VEC-CAP ! ;

	: VEC-SETLEN ( NEW-LEN VEC STRIDE -- )
		2DUP DROP VEC-CAP @ {: VEC STRIDE CAP :}
		DUP CAP > IF
			BEGIN
				CAP 4 MAX 2* TO CAP
				DUP CAP > 0= UNTIL
			CAP VEC STRIDE VEC-SETCAP THEN
		VEC VEC-LEN ! ;

	: VEC-PUSH ( DATA VEC STRIDE -- )
		2>R 2R@ DROP VEC-LEN @ DUP 1+ 2R@ VEC-SETLEN
		2R@ VEC-ELEM R> MOVE R> DROP ;

	: SKIP-SPACE ( -- SUCC )
		BEGIN
			BEGIN PEEK WHITESPACE? WHILE GETC DROP REPEAT
			PEEK [CHAR] ; = IF
				BEGIN PEEK EOL <> WHILE
					GETC DROP REPEAT
				FALSE ELSE
				TRUE THEN UNTIL ;

	: READ-HASHTAG ( O -- SUCC )
		PEEK [CHAR] # <> IF DROP FALSE EXIT THEN GETC DROP GETC UPPER
		DUP [CHAR] T = IF DROP >R T=BOOL TRUE  R> PUT-TVAL TRUE EXIT THEN
		DUP [CHAR] F = IF DROP >R T=BOOL FALSE R> PUT-TVAL TRUE EXIT THEN
		DROP ABORT" SYNTAX?" ;

	: READ-NUMBER ( O -- SUCC )
		DROP FALSE \ TODO
		;
	
	: READ-SYMBOL ( O -- SUCC )
		PEEK DELIMITER? IF DROP FALSE EXIT THEN
		HERE VECSIZE ALLOT HERE 1 ALLOT
		{: O BUF CH :} BUF VEC-INIT
		BEGIN PEEK DELIMITER? 0= WHILE
			GETC CH ! CH BUF 1 VEC-PUSH REPEAT
		BUF 1 VEC-DATA BUF VEC-LEN @ O MAKE-SYM
		VECSIZE NEGATE ALLOT TRUE ;

	TVAL S-DOT S-DOT
		READ-SYMBOL . DROP
	TVAL S-LAMBDA S-LAMBDA
		READ-SYMBOL lambda DROP

	: READ-SEXTERM ( -- SUCC )
		SKIP-SPACE PEEK [CHAR] ) <> IF
			FALSE EXIT THEN
		GETC DROP TRUE ;
	
	: SEXTERM ( -- TRUE )
		TSIZE NEGATE ALLOT TRUE ;

	DEFER READ-EXP

	: READ-SEX ( O -- SUCC )
		PEEK [CHAR] ( <> IF DROP FALSE EXIT THEN
		GETC DROP HERE TSIZE ALLOT OVER {: O M G :}
		NIL O COPY-TVAL BEGIN
			READ-SEXTERM IF SEXTERM EXIT THEN
			M READ-EXP 0= ABORT" SYNTAX?"
			S-DOT M EQ? IF
				M READ-EXP 0= ABORT" SYNTAX?"
				READ-SEXTERM 0= ABORT" SYNTAX?"
				M G COPY-TVAL SEXTERM EXIT THEN
			M NIL G CONS G CDR TO G AGAIN ;

	:NONAME ( O -- SUCC )
		SKIP-SPACE
		DUP READ-HASHTAG IF DROP TRUE EXIT THEN
		DUP READ-NUMBER IF DROP TRUE EXIT THEN
		DUP READ-SYMBOL IF DROP TRUE EXIT THEN
		DUP READ-SEX IF DROP TRUE EXIT THEN
		DROP FALSE ; IS READ-EXP

NM-PUB

\ NM-END

