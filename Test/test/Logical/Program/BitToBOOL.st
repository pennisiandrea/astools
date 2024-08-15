FUNCTION BitToBOOL
	(*
	A memory area made up by bits should be converted to bool:
	
	01100100
	11011001
	00100110
	01110001
	00100100
		||
	FALSE
	TRUE
	TRUE
	FALSE
	FALSE
	TRUE
	FALSE
	FALSE
	...	
	
	*)
	
	//Check compatibility of memory areas:
	IF BoolMemorySize < 8*BitMemorySize THEN
		BitToBOOL := FALSE;
		
	ELSE
		
		FOR ByteIndex := 0 TO BitMemorySize-1 DO // 1 cycle foreach byte in the BitMemory
			
			brsmemcpy(ADR(SelectedByte),BitMemory+ByteIndex,1);
			
			FOR BitIndex := 0 TO 7 DO // 1 cycle foreach bit in the selected byte
				
				brsmemset(BoolMemory+ByteIndex*8+BitIndex,(SelectedByte MOD 2),1);				
				SelectedByte := SelectedByte / 2; // Shift byte 
				
			END_FOR
			
		END_FOR
		
		BitToBOOL := TRUE;
		
	END_IF


END_FUNCTION
