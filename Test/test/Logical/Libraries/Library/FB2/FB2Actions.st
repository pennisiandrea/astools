ACTION FB2CheckParameters:
    IF  Parameter1 <= 0.0 THEN
        StatusID := 12345; (*add StatusID constant here*)
        FB2SetError;
    ELSIF  Parameter2 < 0.0 THEN
        StatusID := 54321; (*add StatusID constant here*)
        FB2SetError;
    ELSE
        Internal.Parameter1 := Parameter1;
        Internal.Parameter2 := Parameter2;
        Active := TRUE;
        Busy := TRUE;
        Error := FALSE;
        StatusID := 0;		
        Internal.ParametersValid := TRUE;	
        Internal.Executing := TRUE;
    END_IF;
END_ACTION

ACTION FB2SetError:
    Active := FALSE;
    Busy := TRUE;
    Done := FALSE;
    Error := TRUE;			
END_ACTION

ACTION FB2CyclicCode:
    (* add code here, when execution is complete call action FBname_SetDone *)
END_ACTION

ACTION FB2SetDone:
    Internal.Executing := FALSE;
    Done := TRUE;
    Busy := FALSE;
    Active := FALSE;			
END_ACTION

ACTION FB2ResetInternal:
    Internal.Parameter1 := 0.0;
    Internal.Parameter2 := 0.0;		
    Internal.ExecuteOld := FALSE;
    Internal.Executing := FALSE;
    Internal.ParametersValid := FALSE;	
END_ACTION

ACTION FB2ResetOutputs:
    Out := 0.0;
    Done := FALSE;
    Busy := FALSE;
    Active := FALSE;
    Error := FALSE;
    StatusID := 0;	
END_ACTION