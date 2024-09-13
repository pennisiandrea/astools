FUNCTION_BLOCK __fb_name__ 

    IF Execute AND NOT Internal.ExecuteOld THEN
        __fb_name__CheckParameters;
        IF Internal.ParametersValid THEN
            Internal.Executing := TRUE;
        END_IF;
    END_IF;
    
    Internal.ExecuteOld := Execute;
    
    IF Internal.Executing THEN
        __fb_name__CyclicCode;
    END_IF;
        
    IF Done AND NOT Execute THEN
        __fb_name__ResetInternal;
        __fb_name__ResetOutputs;
    END_IF;

END_FUNCTION_BLOCK