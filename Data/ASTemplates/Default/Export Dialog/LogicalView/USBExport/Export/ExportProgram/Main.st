PROGRAM _INIT
	Internal.USBFileDevice := 'USB0';
	
    ResetFeedbacksAction;

	MachineState.NextState := INIT;
    
	gExport.Commands.Enable := TRUE;
END_PROGRAM

PROGRAM _CYCLIC
    // Alarms
    AlarmsAction;

    // Enable module
    IF NOT gExport.Commands.Enable THEN
        ResetFeedbacksAction;		
        RETURN;
    END_IF
	
	Internal.USBDeviceConnected := usb_link.status.total_valid_USB_connected>=1 AND usb_link.status.instance[0].usb_valid=USB_LINK_VALID;
	IF NOT Internal.USBDeviceConnected AND MachineState.ActualState <> ERROR THEN
		MachineState.NextState := WAITING_USB;
	END_IF
	
    // Machine state
    MachineStateManagementAction;
    CASE MachineState.ActualState OF
        
        INIT:
            MachineState.NextState := WAITING_USB;
            
		WAITING_STEP_TRIGGER:
			MachineState.TimeoutTimer.PT := T#0S;
			// Just wait the step trigger. The logic is managed in MachineStateManagementAction action.
		
		ERROR:
			MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
			IF gExport.Commands.Reset THEN
				MachineState.NextState := INIT;
			END_IF
        	
        WAITING_USB:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            IF Internal.USBDeviceConnected THEN
                MachineState.NextState := WAITING_COMMAND;
            END_IF 
        
		WAITING_COMMAND:
			MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
			IF gExport.Commands.ExportAlarms THEN
				gExport.Commands.ExportAlarms := FALSE;
				Internal.TransferWithDelete := TRUE;
				Internal.SourceFileDevice := ALARMS_FILE_DEVICE;
				MachineState.NextState := WAITING_ALARM_EXPORT;
				
			ELSIF gExport.Commands.ExportRecipes THEN
				gExport.Commands.ExportRecipes := FALSE;
				Internal.TransferWithDelete := FALSE;
				Internal.DestinationFolder := RECIPES_FOLDER;
				Internal.SourceFileDevice := RECIPES_FILE_DEVICE;
				MachineState.NextState := OPEN_SRC_FOLDER;
				
			ELSIF gExport.Commands.ExportSettings THEN
				gExport.Commands.ExportSettings := FALSE;
				Internal.TransferWithDelete := FALSE;
				Internal.DestinationFolder := SETTINGS_FOLDER;
				Internal.SourceFileDevice := SETTINGS_FILE_DEVICE;
				MachineState.NextState := OPEN_SRC_FOLDER;
				
			END_IF 
			
		WAITING_ALARM_EXPORT:
			MachineState.TimeoutTimer.PT := T#30S;
			IF MachineState.NewTriggerState THEN
				gAlarms.Commands.Export := FALSE;
			ELSE
				
				IF NOT gAlarms.Commands.Export AND NOT gAlarms.Feedbacks.ExportDone THEN
					gAlarms.Commands.Export := TRUE;
					
				ELSIF gAlarms.Commands.Export THEN
					
					IF gAlarms.Feedbacks.ExportFails THEN
						MpAlarmXSet(gAlarmXCore,'ExportPrg_WAITING_ALARM_EXPORT_Fails');
						
					ELSIF gAlarms.Feedbacks.ExportDone THEN
						gAlarms.Commands.Export := FALSE;
						Internal.DestinationFolder := ALARMS_FOLDER;
						MachineState.NextState := OPEN_SRC_FOLDER;
				
					END_IF
					
				END_IF
				
			END_IF
		
		OPEN_SRC_FOLDER:
			MachineState.TimeoutTimer.PT := T#10S;
			IF MachineState.NewTriggerState THEN
				Internal.DirOpenFB.enable := FALSE;
				Internal.DirOpenFB.pDevice := ADR(Internal.SourceFileDevice);
				
			ELSE
				Internal.DirOpenFB.enable := TRUE;
				
				IF Internal.DirOpenFB.status = ERR_OK THEN
					Internal.DirOpenFB.enable := FALSE;
					FolderIdent := Internal.DirOpenFB.ident;
					MachineState.NextState := GET_SRC_FOLDER_FILE_NAMES;
				ELSIF Internal.DirOpenFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF Internal.DirOpenFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE	
					MpAlarmXSet(gAlarmXCore,'ExportPrg_OPEN_SRC_FOLDER_Fails');	
					Internal.DirOpenFB.enable := FALSE;
				END_IF
			
			END_IF
			
		GET_SRC_FOLDER_FILE_NAMES:
			MachineState.TimeoutTimer.PT := T#120S;
			IF MachineState.NewTriggerState THEN
				brsmemset(ADR(Internal.FileList),0,SIZEOF(Internal.FileList));
				brsmemset(ADR(DirReadExData),0,SIZEOF(DirReadExData));
				Internal.FileNum := 0;
				
				Internal.DirReadExFB.enable := FALSE;
				Internal.DirReadExFB.ident := FolderIdent;
				Internal.DirReadExFB.pData := ADR(DirReadExData);
				Internal.DirReadExFB.data_len := SIZEOF(DirReadExData);
				
			ELSE
				Internal.DirReadExFB.enable := TRUE;
				
				IF Internal.DirReadExFB.status = ERR_OK THEN 
					IF DirReadExData.Mode = fiFILE THEN
						brsmemcpy(ADR(Internal.FileList[Internal.FileNum]),ADR(DirReadExData.Filename),SIZEOF(Internal.FileList[Internal.FileNum]));
						Internal.FileNum := Internal.FileNum + 1;
						IF Internal.FileNum >= MAX_FILE_IDX THEN
							Internal.DirReadExFB.enable := FALSE;
							MachineState.NextState := CLOSE_SRC_FOLDER;
						END_IF
					END_IF
					
				ELSIF Internal.DirReadExFB.status = fiERR_NO_MORE_ENTRIES THEN
					Internal.DirReadExFB.enable := FALSE;
					MachineState.NextState := CLOSE_SRC_FOLDER;
					
				ELSIF Internal.DirReadExFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF Internal.DirReadExFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(gAlarmXCore,'ExportPrg_GET_SRC_FOLDER_FILE_NAMES_Fails');
					Internal.DirReadExFB.enable := FALSE;
				END_IF
			
			END_IF
			
		CLOSE_SRC_FOLDER:
			MachineState.TimeoutTimer.PT := T#10S;
			IF MachineState.NewTriggerState THEN
				Internal.DirCloseFB.enable := FALSE;
				Internal.DirCloseFB.ident := FolderIdent;
				
			ELSE
				Internal.DirCloseFB.enable := TRUE;
				
				IF Internal.DirCloseFB.status <> ERR_FUB_ENABLE_FALSE AND Internal.DirCloseFB.status <> ERR_FUB_BUSY THEN 
					Internal.DirCloseFB.enable := FALSE;
					IF Internal.FileNum = 0 THEN
						MachineState.NextState := WAITING_COMMAND;
					ELSE
						MachineState.NextState := CHECK_DEST_FOLDER;
					END_IF
				END_IF
					
			END_IF
			
		CHECK_DEST_FOLDER:
			MachineState.TimeoutTimer.PT := T#15S;
			IF MachineState.NewTriggerState THEN
				Internal.DirCreateFB.enable := FALSE;
				Internal.DirCreateFB.pName := ADR(Internal.DestinationFolder);
				
			ELSE
				Internal.DirCreateFB.enable := TRUE;
				
				IF Internal.DirCreateFB.status = ERR_OK OR Internal.DirCreateFB.status = fiERR_DIR_ALREADY_EXIST THEN
					Internal.DirCreateFB.enable := FALSE;
					MachineState.NextState := TRANSFER_FILES;
				ELSIF Internal.DirCreateFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF Internal.DirCreateFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(gAlarmXCore,'ExportPrg_CHECK_DEST_FOLDER_Fails');
					Internal.DirCreateFB.enable := FALSE;
				END_IF
					
			END_IF
			
		TRANSFER_FILES:
			IF MachineState.NewTriggerState THEN
				MachineState.TimeoutTimer.PT := MAX(T#5S,T#500ms * Internal.FileNum);
				ActFileNum := 1;
				Internal.DestinationFolder := CONCAT(Internal.DestinationFolder,'\');
				DestinationFileName := CONCAT(Internal.DestinationFolder,Internal.FileList[ActFileNum-1]);
				
				Internal.FileCopyFB.enable := FALSE;
				Internal.FileCopyFB.pSrcDev := ADR(Internal.SourceFileDevice);
				Internal.FileCopyFB.pSrc := ADR(Internal.FileList[ActFileNum-1]);
				Internal.FileCopyFB.pDestDev := ADR(Internal.USBFileDevice);
				Internal.FileCopyFB.pDest := ADR(DestinationFileName);
				Internal.FileCopyFB.option := fiOVERWRITE;
				
			ELSE
				Internal.FileCopyFB.enable := TRUE;
				
				IF Internal.FileCopyFB.status = ERR_OK THEN
					Internal.FileCopyFB.enable := FALSE;
					ActFileNum := ActFileNum + 1;
					IF ActFileNum > Internal.FileNum THEN 
						IF Internal.TransferWithDelete THEN
							MachineState.NextState := DELETE_FILES;
						ELSE
							MachineState.NextState := WAITING_COMMAND;
						END_IF
					ELSE
						Internal.FileCopyFB.pSrc := ADR(Internal.FileList[ActFileNum-1]);
						DestinationFileName := CONCAT(Internal.DestinationFolder,Internal.FileList[ActFileNum-1]);
					END_IF
				ELSIF Internal.FileCopyFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF Internal.FileCopyFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(gAlarmXCore,'ExportPrg_TRANSFER_FILES_Fails');
					Internal.FileCopyFB.enable := FALSE;
				END_IF
					
			END_IF
			
		DELETE_FILES:
			IF MachineState.NewTriggerState THEN
				MachineState.TimeoutTimer.PT := MAX(T#5S,T#500ms * Internal.FileNum);
				ActFileNum := 1;
				
				Internal.FileDeleteFB.enable := FALSE;
				Internal.FileDeleteFB.pDevice := ADR(Internal.SourceFileDevice);
				Internal.FileDeleteFB.pName := ADR(Internal.FileList[0]);
				
			ELSE
				Internal.FileDeleteFB.enable := TRUE;
				
				IF Internal.FileDeleteFB.status = ERR_OK THEN
					Internal.FileDeleteFB.enable := FALSE;
					ActFileNum := ActFileNum + 1;
					IF ActFileNum > Internal.FileNum THEN 
						IF Internal.FileNum >= MAX_FILE_IDX THEN
							MachineState.NextState := OPEN_SRC_FOLDER; // Repeat the entire process -> The src folder had more then the max number of manageble files
						ELSE
							MachineState.NextState := WAITING_COMMAND;
						END_IF
					ELSE
						Internal.FileDeleteFB.pName := ADR(Internal.FileList[ActFileNum-1]);
					END_IF
					
				ELSIF Internal.FileDeleteFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF Internal.FileDeleteFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(gAlarmXCore,'ExportPrg_DELETE_FILES_Fails');
					Internal.FileDeleteFB.enable := FALSE;
				END_IF
					
			END_IF
			
			
        ELSE
            MachineState.NextState := INIT;			
        
    END_CASE
    
    FeedbacksUpdateAction;
    
	IF MachineState.ActualState <> CHECK_DEST_FOLDER THEN
		Internal.DirCreateFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> OPEN_SRC_FOLDER THEN
		Internal.DirOpenFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> CLOSE_SRC_FOLDER THEN
		Internal.DirCloseFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> GET_SRC_FOLDER_FILE_NAMES THEN
		Internal.DirReadExFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> TRANSFER_FILES THEN
		Internal.FileCopyFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> DELETE_FILES THEN
		Internal.FileDeleteFB.enable := FALSE;
	END_IF
	
	Internal.DirCreateFB(pDevice := ADR(Internal.USBFileDevice));
	Internal.DirCloseFB();
	Internal.FileCopyFB();
	Internal.FileDeleteFB();
	Internal.DirOpenFB();
	Internal.DirReadExFB();
	
END_PROGRAM

PROGRAM _EXIT
    
END_PROGRAM