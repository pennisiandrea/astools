

FUNCTION_BLOCK FB1 (*FB template for Enable FB*) 
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
        Internal : FB1InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK
FUNCTION_BLOCK FB2 (*FB template for Execute FB*)
    VAR_INPUT
        Execute : BOOL; (*Execute the function block*)
        Parameter1 : REAL; (*Parameter required for computing*) 
        Parameter2 : REAL; (*Parameter required for computing*) 
        In : REAL; (*Input variable*)
    END_VAR
    VAR_OUTPUT
        Done : BOOL; (*Execute is done*)
        Busy : BOOL; (*Function block is busy*)
        Active : BOOL; (*Function block is active*)
        Error : BOOL; (*Indicates an error*)
        StatusID : DINT; (*Status information*)
        Out : REAL; (*Output variable*)
    END_VAR
    VAR
        Internal : FB2InternalType; (*Data for internal use*)
    END_VAR
END_FUNCTION_BLOCK
FUNCTION_BLOCK FB3 (*FB template for Enable FB*) 
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
        Internal : FB3InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK
FUNCTION_BLOCK FB3 (*FB template for Enable FB*) 
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
        Internal : FB3InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK
FUNCTION_BLOCK FB4 (*FB template for Enable FB*) 
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
        Internal : FB4InternalType; (*Data for internal use*) 
    END_VAR 
END_FUNCTION_BLOCK