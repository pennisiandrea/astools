
ACTION __fb_name__CheckParameters:
    IF  Parameter1 <= 0.0 THEN
        StatusID := 12345; (*add StatusID constant here*)
        __fb_name__SetError;
    ELSIF  Parameter2 < 0.0 THEN
        StatusID := 54321; (*add StatusID constant here*)
        __fb_name__SetError;
    ELSE
        Internal.Parameter1 := Parameter1;
        Internal.Parameter2 := Parameter2;
        Busy := TRUE;
        Error := FALSE;
        StatusID := 0;		
        Internal.ParametersValid := TRUE;		
        Internal.Executing := TRUE;
    END_IF;
END_ACTION

ACTION __fb_name__SetError:
    Busy := TRUE;
    Done := FALSE;
    Error := TRUE;			
END_ACTION

ACTION __fb_name__CyclicCode:
    (*add main functionality of FB here*)	
END_ACTION
    
ACTION __fb_name__SetDone:
    Internal.Executing := FALSE;
    Done := TRUE;
    Busy := FALSE;		
END_ACTION

ACTION __fb_name__ResetInternal:
    Internal.Parameter1 := 0.0;
    Internal.Parameter2 := 0.0;		
    Internal.ExecuteOld := FALSE;
    Internal.Executing := FALSE;
    Internal.ParametersValid := FALSE;	
END_ACTION

ACTION __fb_name__ResetOutputs:
    Out := 0.0;
    Done := FALSE;
    Busy := FALSE;
    Error := FALSE;
    StatusID := 0;
END_ACTION