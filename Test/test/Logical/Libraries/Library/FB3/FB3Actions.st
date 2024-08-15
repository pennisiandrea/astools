
ACTION FB3CheckParameters:
    IF  Parameter1 <= 0.0 THEN
        StatusID := 12345; (*add StatusID constant here*)
        FB3SetError;
    ELSIF  Parameter2 < 0.0 THEN
        StatusID := 54321; (*add StatusID constant here*)
        FB3SetError;
    ELSE
        Internal.Parameter1 := Parameter1;
        Internal.Parameter2 := Parameter2;
        Active := TRUE;
        Error := FALSE;
        StatusID := 0;		
        Internal.ParametersValid := TRUE;	
    END_IF;
END_ACTION

ACTION FB3SetError:
    Active := FALSE;
    Error := TRUE;			
END_ACTION

ACTION FB3CyclicCode:
    (*add main functionality of FB here*)	
END_ACTION
    
ACTION FB3ResetInternal:
    Internal.Parameter1 := 0.0;
    Internal.Parameter2 := 0.0;		
    Internal.EnableOld := FALSE;
    Internal.ParametersValid := FALSE;	
END_ACTION

ACTION FB3ResetOutputs:
    Out := 0.0;
    Active := FALSE;
    Error := FALSE;
    StatusID := 0;	
END_ACTION