PROGRAM _INIT
                
    ResetOutputsAction;
    ResetFeedbacksAction;
    
    //MpRecipeRegParFB(MpLink := ADR(gRecipeXmlMpLink), Enable := TRUE, PVName := ADR('gprgTest.Parameters'));
    
    MachineState.NextState := INIT;
    
END_PROGRAM

PROGRAM _CYCLIC
    // Recipe
    //MpRecipeRegParFB(ErrorReset := gprgTest.Commands.Reset AND MpRecipeRegParFB.Error);

    // Alarms
    AlarmsAction;

    // Enable module
    IF NOT gprgTest.Commands.Enable THEN
        ResetOutputsAction;
        ResetFeedbacksAction;		
        RETURN;
    END_IF
    
    // Machine state
    MachineStateManagementAction;
    CASE MachineState.ActualState OF
        
        INIT:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            MachineState.NextState := WAITING;
            
        WAITING:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            IF gprgTest.Commands.Start THEN
            //    MachineState.NextState := <NEW_STATE>;
            END_IF 
        
        ERROR:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            IF MachineState.NewTriggerState THEN
                ResetOutputsAction;
            ELSE
                IF gprgTest.Commands.Reset THEN
                    gprgTest.Commands.Reset := FALSE;
                    MachineState.NextState := MachineState.OldState;
                END_IF
            END_IF
            
        WAITING_STEP_TRIGGER:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            // Just wait the step trigger. The logic is managed in MachineStateManagementAction action.

        ELSE
            MachineState.NextState := INIT;			
        
    END_CASE
    
    FeedbacksUpdateAction;
    
END_PROGRAM

PROGRAM _EXIT
    //MpRecipeRegParFB(Enable := FALSE);
    
END_PROGRAM