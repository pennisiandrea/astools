ACTION FeedbacksUpdateAction: 

    gExport.Feedbacks.Enabled := TRUE; 
    gExport.Feedbacks.Error := MachineState.ActualState = ERROR AND NOT gExport.Commands.Reset;
	gExport.Feedbacks.USBDeviceConnected := Internal.USBDeviceConnected;
	gExport.Feedbacks.ActFileNum := ActFileNum;
	gExport.Feedbacks.TotFileNum := Internal.FileNum;
	
	IF MachineState.ActualState = WAITING_USB THEN
		gExport.Feedbacks.StateDescriptionID := 0;
	ELSIF MachineState.ActualState = WAITING_COMMAND THEN
		gExport.Feedbacks.StateDescriptionID := 1;
	ELSIF MachineState.ActualState = ERROR THEN
		gExport.Feedbacks.StateDescriptionID := 2;
	ELSIF MachineState.ActualState = TRANSFER_FILES THEN
		gExport.Feedbacks.StateDescriptionID := 3;
	ELSIF MachineState.ActualState = DELETE_FILES THEN
		gExport.Feedbacks.StateDescriptionID := 4;
	ELSE
		gExport.Feedbacks.StateDescriptionID := 5;
	END_IF
	
END_ACTION

ACTION ResetFeedbacksAction: 

    memset(ADR(gExport.Feedbacks),0,SIZEOF(gExport.Feedbacks));

END_ACTION

ACTION MachineStateManagementAction: 

    // Machine state timeout check
    MachineState.TimeoutTimer(IN := MachineState.TimeoutTimer.PT <> T#0S AND NOT MachineState.NewTriggerState);
	IF MachineState.TimeoutTimer.Q THEN
		MpAlarmXSet(gAlarmXCore,'ExportPrg_Timeout'); // Edge alarm!        

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

    // IF <Condition> AND gExport.Commands.Enable THEN
    //      MpAlarmXSet(gAlarmXCoreMpLink,'<AlarmName>');
    //      MachineState.NextState := ERROR;
    // ELSE
    //      MpAlarmXReset(gAlarmXCoreMpLink,'<AlarmName>'); // Reset only for persistent alarms
    // END_IF

	IF MpAlarmXCheckReaction(gAlarmXCore,'STOP_SUB_PROCESS') THEN
		MachineState.NextState := ERROR;		
		
	END_IF
	
END_ACTION