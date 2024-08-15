FUNCTION_BLOCK __fb_name__ 

    IF Enable THEN     
        IF NOT Internal.EnableOld THEN 
            __fb_name__CheckParameters;
        END_IF;

        IF Internal.ParametersValid THEN
            __fb_name__CyclicCode;
        END_IF;
        
        Internal.EnableOld := Enable;		
    ELSE
        __fb_name__ResetInternal;
        __fb_name__ResetOutputs;		
    END_IF;

END_FUNCTION_BLOCK