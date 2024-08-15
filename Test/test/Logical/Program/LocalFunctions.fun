
{REDUND_ERROR} FUNCTION BitToBOOL : BOOL (*TODO: Add your comment here*) (*$GROUP=User,$CAT=User,$GROUPICON=User.png,$CATICON=User.png*)
	VAR_INPUT
		BitMemory : UDINT;
		BitMemorySize : UDINT;
		BoolMemory : UDINT;
		BoolMemorySize : UDINT;
	END_VAR
	VAR
		BitIndex : USINT;
		ByteIndex : UDINT;
		SelectedByte : USINT;
	END_VAR
END_FUNCTION
