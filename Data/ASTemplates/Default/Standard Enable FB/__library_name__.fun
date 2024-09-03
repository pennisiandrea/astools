FUNCTION_BLOCK __fb_name__ (*FB template for Enable FB*) 
    VAR_INPUT 
        Enable : BOOL; (*Enable the function block*) 
        Parameter1 : REAL; (*Parameter required for computing*) 
        Parameter2 : REAL; (*Parameter required for computing*) 
        In : REAL; (*Input variable*) 
    END_VAR 
    VAR_OUTPUT 
        Active : BOOL; (*Function block is active*) 
        Error : BOOL; (*Indicates an error*) 
        StatusID : DINT; (*Status information*) 
        Out : REAL; (*Output variable*) 
    END_VAR 
    VAR 
        Internal : __fb_name__InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK