PROGRAM _INIT
                
    ResetOutputsAction;
    ResetFeedbacksAction;
    
    //MpRecipeRegParFB(MpLink := ADR(gRecipeXmlMpLink), Enable := TRUE, PVName := ADR('g__module_name__.Parameters'));
    
    MachineState.NextState := INIT;
    
END_PROGRAM

PROGRAM _CYCLIC
    // Recipe
    //MpRecipeRegParFB(ErrorReset := g__module_name__.Commands.Reset AND MpRecipeRegParFB.Error);

    // Alarms
    AlarmsAction;

    // Enable module
    IF NOT g__module_name__.Commands.Enable THEN
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
            IF g__module_name__.Commands.Start THEN
            //    MachineState.NextState := <NEW_STATE>;
            END_IF 
        
        ERROR:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            IF MachineState.NewTriggerState THEN
                ResetOutputsAction;
            ELSE
                IF g__module_name__.Commands.Reset THEN
                    g__module_name__.Commands.Reset := FALSE;
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