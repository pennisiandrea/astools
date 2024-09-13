ACTION FeedbacksUpdateAction: 

    gUSBExport.Feedbacks.Enabled := TRUE; 
    gUSBExport.Feedbacks.Error := MachineState.ActualState = ERROR AND NOT gUSBExport.Commands.Reset;
	gUSBExport.Feedbacks.USBDeviceConnected := USBDeviceConnected;
	gUSBExport.Feedbacks.ActFileNum := ActFileNum;
	gUSBExport.Feedbacks.TotFileNum := FileNum;
	
	IF MachineState.ActualState = WAITING_USB THEN
		gUSBExport.Feedbacks.StateDescriptionID := 0;
	ELSIF MachineState.ActualState = WAITING_COMMAND THEN
		gUSBExport.Feedbacks.StateDescriptionID := 1;
	ELSIF MachineState.ActualState = ERROR THEN
		gUSBExport.Feedbacks.StateDescriptionID := 2;
	ELSIF MachineState.ActualState = TRANSFER_FILES THEN
		gUSBExport.Feedbacks.StateDescriptionID := 3;
	ELSIF MachineState.ActualState = DELETE_FILES THEN
		gUSBExport.Feedbacks.StateDescriptionID := 4;
	ELSE
		gUSBExport.Feedbacks.StateDescriptionID := 5;
	END_IF
	
END_ACTION

ACTION ResetFeedbacksAction: 

	brsmemset(ADR(gUSBExport.Feedbacks),0,SIZEOF(gUSBExport.Feedbacks));

END_ACTION

ACTION ResetCommandsAction: 

	gUSBExport.Commands.ExportDiagnostics := FALSE;
	gUSBExport.Commands.ExportRecipes := FALSE;
	gUSBExport.Commands.ExportSettings := FALSE;

END_ACTION

ACTION MachineStateManagementAction: 

    // Machine state timeout check
	MachineState.TimeoutTimer(IN := MachineState.TimeoutTimer.PT <> T#0S AND NOT MachineState.NewStateTrigger);
	IF MachineState.TimeoutTimer.Q THEN
		MpAlarmXSet(AlarmXCoreMpLink,'USBExport_Timeout'); // Edge alarm!  
		MachineState.NextState := ERROR;		
	END_IF            
    
	// Machine state change state logic
	MachineState.NewStateTrigger := (MachineState.ActualState <> MachineState.NextState);
	IF MachineState.NewStateTrigger AND MachineState.ActualState <> WAITING_STEP_TRIGGER THEN
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