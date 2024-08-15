FUNCTION_BLOCK FB3 

    IF Enable THEN     
        IF NOT Internal.EnableOld THEN 
            FB3CheckParameters;
        END_IF;

        IF Internal.ParametersValid THEN
            FB3CyclicCode;
        END_IF;
        
        Internal.EnableOld := Enable;		
    ELSE
        FB3ResetInternal;
        FB3ResetOutputs;		
    END_IF;

END_FUNCTION_BLOCK