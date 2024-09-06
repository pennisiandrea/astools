
TYPE
	MachineStateEnum : 
		( (*Machine State enumeration*)
		INIT, (*INIT state*)
		WAITING_STEP_TRIGGER,
		ERROR, (*ERROR state*)
		WAITING_USB, (*WAITING state*)
		WAITING_COMMAND,
		WAITING_ALARM_EXPORT,
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
		NewTriggerState : BOOL; (*Trigger state change*)
		TimeoutTimer : TON; (*State timeout*)
		StepByStepEnable : BOOL;
		StepByStepTrigger : BOOL;
	END_STRUCT;
	InternalType : 	STRUCT 
		USBDeviceConnected : BOOL;
		DirCreateFB : DirCreate;
		FileCopyFB : FileCopy;
		FileDeleteFB : FileDelete;
		DirOpenFB : DirOpen;
		DirReadExFB : DirReadEx;
		DirCloseFB : DirClose;
		DestinationFolder : STRING[80];
		DestinationFileDevice : STRING[80];
		SourceFileDevice : STRING[80];
		USBFileDevice : STRING[80];
		TransferWithDelete : BOOL;
		FileList : ARRAY[0..MAX_FILE_IDX]OF STRING[80];
		FileNum : UINT;
	END_STRUCT;
END_TYPE
