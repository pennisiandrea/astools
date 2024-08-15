TYPE
    FB2InternalType :     STRUCT  (*Template of a structure of internal parameters for an enable function block *) 
        ExecuteOld : BOOL; (*Variable to detect rising edge on Execute input*) 
        Executing : BOOL; (*Executing flag*) 
        ParametersValid : BOOL; (*All parameters valid flag *) 
        Parameter1 : REAL; (*Internal parameter required for computing*) 
        Parameter2 : REAL; (*Internal parameter required for computing*)          		
    END_STRUCT; 
END_TYPE