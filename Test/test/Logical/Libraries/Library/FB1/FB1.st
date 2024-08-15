FUNCTION_BLOCK FB1 

    IF Enable THEN     
        IF NOT Internal.EnableOld THEN 
            FB1CheckParameters;
        END_IF;

        IF Internal.ParametersValid THEN
            FB1CyclicCode;
        END_IF;
        
        Internal.EnableOld := Enable;		
    ELSE
        FB1ResetInternal;
        FB1ResetOutputs;		
    END_IF;

END_FUNCTION_BLOCK