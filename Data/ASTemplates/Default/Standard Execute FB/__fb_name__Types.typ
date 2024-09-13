
TYPE
    __fb_name__InternalType :     STRUCT  (*Template of a structure of internal parameters for an execute function block *) 
        ExecuteOld : BOOL; (*Variable to detect rising edge on Execute input*) 
        Executing : BOOL; (*Executing flag*) 
        ParametersValid : BOOL; (*All parameters valid flag*) 
        Parameter1 : REAL; (*Internal parameter for computing*)
        Parameter2 : REAL; (*Internal parameter for computing*) 
    END_STRUCT; 
END_TYPE