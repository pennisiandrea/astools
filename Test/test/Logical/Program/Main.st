
PROGRAM _INIT
	(* Insert code here *)
	 
END_PROGRAM

PROGRAM _CYCLIC
	//BitToBOOL(ADR(MemoryArea),SIZEOF(MemoryArea),ADR(Booleans),SIZEOF(Booleans));
	
	Byte2BitFB.byteadr := ADR(MemoryArea);
	Byte2BitFB.length := SIZEOF(MemoryArea);
	Byte2BitFB();
		
	IF Byte2BitFB.byteadr <> 0 THEN
		brsmemcpy(ADR(Booleans),Byte2BitFB.bitadr,SIZEOF(Booleans));
	END_IF
END_PROGRAM

PROGRAM _EXIT
	(* Insert code here *)
	 
END_PROGRAM

