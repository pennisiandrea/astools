ACTION FeedbacksUpdateAction: 

    g__module_name__.Feedbacks.Enabled := TRUE;
    g__module_name__.Feedbacks.Waiting := MachineState.ActualState = WAITING; 
    g__module_name__.Feedbacks.Error := MachineState.ActualState = ERROR;

END_ACTION

ACTION ResetOutputsAction: 

    memset(ADR(g__module_name__.Interface.Outputs),0,SIZEOF(g__module_name__.Interface.Outputs));

END_ACTION

ACTION ResetFeedbacksAction: 

    memset(ADR(g__module_name__.Feedbacks),0,SIZEOF(g__module_name__.Feedbacks));

END_ACTION

ACTION MachineStateManagementAction: 

    // Machine state timeout check
    MachineState.TimeoutTimer(IN := MachineState.TimeoutTimer.PT <> T#0S AND NOT MachineState.NewTriggerState);
    IF MachineState.TimeoutTimer.Q THEN
        
        // Throw here timeout alarms
        //CASE MachineState.ActualState OF
        //    <STATE_1_WITH_TIMEOUT>: MpAlarmXSet(gAlarmXCoreMpLink,'<AlarmName>'); // Edge alarm!
        //    <STATE_2_WITH_TIMEOUT>: MpAlarmXSet(gAlarmXCoreMpLink,'<AlarmName>'); // Edge alarm!
        //END_CASE

        MachineState.NextState := ERROR;		
    END_IF            
    
    // Machine state change state logic
    MachineState.NewTriggerState := (MachineState.ActualState <> MachineState.NextState);
    IF MachineState.NewTriggerState AND MachineState.ActualState <> WAITING_STEP_TRIGGER THEN
        MachineState.OldState := MachineState.ActualState;
        
        IF MachineState.StepByStepEnable THEN
            MachineState.ActualState := WAITING_STEP_TRIGGER;
        END_IF
    END_IF
    IF NOT MachineState.StepByStepEnable OR MachineState.StepByStepTrigger THEN
        MachineState.ActualState := MachineState.NextState;
        MachineState.StepByStepTrigger := FALSE;	
    END_IF

END_ACTION

ACTION AlarmsAction:             
    // Throw here alarms which should be checked continuously
    // Machine state timeout alarms are managed in MachineStateManagementAction

    // IF <Condition> AND g__module_name__.Commands.Enable THEN
    //      MpAlarmXSet(gAlarmXCoreMpLink,'<AlarmName>');
    //      MachineState.NextState := ERROR;
    // ELSE
    //      MpAlarmXReset(gAlarmXCoreMpLink,'<AlarmName>'); // Reset only for persistent alarms
    // END_IF

END_ACTION