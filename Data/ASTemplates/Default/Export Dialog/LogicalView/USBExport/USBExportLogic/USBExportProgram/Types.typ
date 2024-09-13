
TYPE
	MachineStateEnum : 
		( (*Machine State enumeration*)
		INIT, (*INIT state*)
		WAITING_STEP_TRIGGER,
		ERROR, (*ERROR state*)
		WAITING_USB, (*WAITING state*)
		WAITING_COMMAND,
		WAITING_DIAGNOSTICS_EXPORT,
		OPEN_SRC_FOLDER,
		GET_SRC_FOLDER_FILE_NAMES,
		CLOSE_SRC_FOLDER,
		CHECK_DEST_FOLDER,
		TRANSFER_FILES,
		DELETE_FILES
		);
	MachineStateType : 	STRUCT  (*Machine state main type*)
		OldState : MachineStateEnum; (*Actual state*)
		ActualState : MachineStateEnum; (*Actual state*)
		NextState : MachineStateEnum; (*Next state*)
		NewStateTrigger : BOOL; (*Trigger state change*)
		TimeoutTimer : TON; (*State timeout*)
		StepByStepEnable : BOOL;
		StepByStepTrigger : BOOL;
	END_STRUCT;
END_TYPE
