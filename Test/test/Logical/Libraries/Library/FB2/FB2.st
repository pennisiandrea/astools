FUNCTION_BLOCK FB2 

    IF Execute AND NOT Internal.ExecuteOld THEN
        FB2CheckParameters;
        IF Internal.ParametersValid THEN
            Internal.Executing := TRUE;
        END_IF;
    END_IF;
    
    Internal.ExecuteOld := Execute;
    
    IF Internal.Executing THEN
        FB2CyclicCode;
    END_IF;
        
    IF Done AND NOT Execute THEN
        FB2ResetInternal;
        FB2ResetOutputs;
    END_IF;
    
END_FUNCTION_BLOCK