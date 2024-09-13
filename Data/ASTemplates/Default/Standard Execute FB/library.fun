FUNCTION_BLOCK __fb_name__ (*FB template for Execute FB*) 
    VAR_INPUT 
        Execute : BOOL; (*Execute the function block*)
        Parameter1 : REAL; (*Parameter required for computing*) 
        Parameter2 : REAL; (*Parameter required for computing*) 
        In : REAL; (*Input variable*) 
    END_VAR 
    VAR_OUTPUT 
        Done : BOOL; (*Execute is done*)
        Busy : BOOL; (*Function block is busy*)
        Error : BOOL; (*Indicates an error*) 
        StatusID : DINT; (*Status information*) 
        Out : REAL; (*Output variable*) 
    END_VAR 
    VAR 
        Internal : __fb_name__InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK