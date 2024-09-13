(*********************************************************************************
 * Copyright: -
 * Author:    Pennisi Andrea 
 * Created:   September 13, 2024/8:18 AM 
 * Description:
		The purpose of this program is to provide a way to export data from the PLC
		to an external USB device. To accomplish this, a dialog for mappView is
		included as part of this template. 
		How does it work? After inserting the USB device into the PLC, the dialog  
		automatically appears. The operator can choose between 3 options:
			- Diagnostics, which exports a system dump and the alarm historian.
			- Recipes, which exports all recipes.
			- Settings, which exports the settings of the machine. 
		Once the USB is removed, the dialog disappears.
		In case of error, gUSBExport.Commands.Reset must be used to reset the machine state. 
 * Requirements:
		- The USB device must be formatted as FAT32.
		- mappAlarmX is used to throw error conditions. A link to mappAlarmX core must
		be assigned in the PROGRAM _INIT.
		- The export of alarm historian must be managed by an external program, the 
		interface (Export/ExportFails/ExportDone) must be assigned in the PROGRAM _INIT.
		- Recipes and Settings are expected to already be available like files in the PLC 
		memory, stored in different folders and file devices, which must be assigned in the PROGRAM _INIT. 
		The same approach applies to the alarm historian export file.
		- Assign usb_link and USBExportProgram to Cyclics. Usb_link shall be assigned to a fast task, 
		USBExportProgram can run in a slower one with a good tolerance value.
 *********************************************************************************)

PROGRAM _INIT	
	(* Links to other project's modules*)
	AlarmXCoreMpLink ACCESS ADR(gAlarmXCore); // Link to mappAlarmXCore MpLink.
	AlarmXHistoryExportCommand ACCESS ADR(Export); // Command to export alarm history. Could be linked to MpAlarmXHistory.Export.
	AlarmXHistoryExportError ACCESS ADR(ExportFails); // Feedback of export failed. Could be linked to MpAlarmXHistory.Error.
	AlarmXHistoryExportDone ACCESS ADR(ExportDone); // Feedback of export done. Could be linked to MpAlarmXHistory.CommandDone.
	
	// These file devices must be available in the system and must be linked to different folders. 
	DiagnosticsFileDevice := 'DIAGNOSTICS'; //File device where alarm historian exports. 
	RecipesFileDevice := 'RECIPES'; //File device where recipes are saved.
	SettingsFileDevice := 'SETTINGS'; //File device where settings are saved.
	
	// Init
	ResetFeedbacksAction;
	MachineState.NextState := INIT;    
	gUSBExport.Commands.Enable := TRUE;
END_PROGRAM

PROGRAM _CYCLIC
    // Enable module
    IF NOT gUSBExport.Commands.Enable THEN
        ResetFeedbacksAction;		
        RETURN;
    END_IF
	
	USBDeviceConnected := usb_link.status.total_valid_USB_connected>=1 AND usb_link.status.instance[0].usb_valid=USB_LINK_VALID;
	IF NOT USBDeviceConnected THEN
		ResetCommandsAction;
		MachineState.NextState := WAITING_USB;
	END_IF
	
    // Machine state
    MachineStateManagementAction;
    CASE MachineState.ActualState OF
        
		INIT:
			ResetCommandsAction;
            MachineState.NextState := WAITING_USB;
        	
        WAITING_USB:
            MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
            IF USBDeviceConnected THEN
                MachineState.NextState := WAITING_COMMAND;
            END_IF 
        
		WAITING_COMMAND:
			MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
			
			IF gUSBExport.Commands.ExportDiagnostics THEN
				gUSBExport.Commands.ExportDiagnostics := FALSE;
				TransferWithDelete := TRUE;
				SourceFileDevice := DiagnosticsFileDevice;
				MachineState.NextState := WAITING_DIAGNOSTICS_EXPORT;
				
			ELSIF gUSBExport.Commands.ExportRecipes THEN
				gUSBExport.Commands.ExportRecipes := FALSE;
				TransferWithDelete := FALSE;
				DestinationFolder := RECIPES_FOLDER;
				SourceFileDevice := RecipesFileDevice;
				MachineState.NextState := OPEN_SRC_FOLDER;
				
			ELSIF gUSBExport.Commands.ExportSettings THEN
				gUSBExport.Commands.ExportSettings := FALSE;
				TransferWithDelete := FALSE;
				DestinationFolder := SETTINGS_FOLDER;
				SourceFileDevice := SettingsFileDevice;
				MachineState.NextState := OPEN_SRC_FOLDER;
				
			END_IF 
			
		WAITING_DIAGNOSTICS_EXPORT:
			MachineState.TimeoutTimer.PT := T#30S;
			IF MachineState.NewStateTrigger THEN
				SdmSystemDumpFB.enable := TRUE;
				AlarmXHistoryExportCommand := TRUE;
				
			ELSE
				
				IF SdmSystemDumpFB.status = ERR_OK THEN
					SdmSystemDumpFB.enable := FALSE;
					
				ELSIF SdmSystemDumpFB.status <> ERR_FUB_BUSY AND SdmSystemDumpFB.status <> ERR_FUB_ENABLE_FALSE THEN
					SdmSystemDumpFB.enable := FALSE;
					AlarmXHistoryExportCommand := FALSE;
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_WAITING_DIAGNOSTICS_EXPORT_Fails');
					MachineState.NextState := ERROR;
				END_IF
								
				IF AlarmXHistoryExportDone THEN
					AlarmXHistoryExportCommand := FALSE;
					
				ELSIF AlarmXHistoryExportError THEN
					SdmSystemDumpFB.enable := FALSE;
					AlarmXHistoryExportCommand := FALSE;
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_WAITING_DIAGNOSTICS_EXPORT_Fails');
					MachineState.NextState := ERROR;
				END_IF
				
				IF NOT SdmSystemDumpFB.enable AND NOT AlarmXHistoryExportCommand AND MachineState.NextState <> ERROR THEN
					DestinationFolder := DIAGNOSTICS_FOLDER;
					MachineState.NextState := OPEN_SRC_FOLDER;					
				END_IF
								
			END_IF
		
		OPEN_SRC_FOLDER:
			MachineState.TimeoutTimer.PT := T#10S;
			IF MachineState.NewStateTrigger THEN
				DirOpenFB.enable := FALSE;
				DirOpenFB.pDevice := ADR(SourceFileDevice);
				
			ELSE
				DirOpenFB.enable := TRUE;
				
				IF DirOpenFB.status = ERR_OK THEN
					DirOpenFB.enable := FALSE;
					FolderIdent := DirOpenFB.ident;
					MachineState.NextState := GET_SRC_FOLDER_FILE_NAMES;
				ELSIF DirOpenFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF DirOpenFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE	
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_OPEN_SRC_FOLDER_Fails');
					DirOpenFB.enable := FALSE;
					MachineState.NextState := ERROR;
				END_IF
			
			END_IF
			
		GET_SRC_FOLDER_FILE_NAMES:
			MachineState.TimeoutTimer.PT := T#120S;
			IF MachineState.NewStateTrigger THEN
				brsmemset(ADR(FileList),0,SIZEOF(FileList));
				brsmemset(ADR(DirReadExData),0,SIZEOF(DirReadExData));
				FileNum := 0;
				
				DirReadExFB.enable := FALSE;
				DirReadExFB.ident := FolderIdent;
				DirReadExFB.pData := ADR(DirReadExData);
				DirReadExFB.data_len := SIZEOF(DirReadExData);
				
			ELSE
				DirReadExFB.enable := TRUE;
				
				IF DirReadExFB.status = ERR_OK THEN 
					IF DirReadExData.Mode = fiFILE THEN
						brsmemcpy(ADR(FileList[FileNum]),ADR(DirReadExData.Filename),SIZEOF(FileList[FileNum]));
						FileNum := FileNum + 1;
						IF FileNum >= MAX_FILE_NUM_TO_EXPORT THEN
							DirReadExFB.enable := FALSE;
							MachineState.NextState := CLOSE_SRC_FOLDER;
						END_IF
					END_IF
					
				ELSIF DirReadExFB.status = fiERR_NO_MORE_ENTRIES THEN
					DirReadExFB.enable := FALSE;
					MachineState.NextState := CLOSE_SRC_FOLDER;
					
				ELSIF DirReadExFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF DirReadExFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_GET_SRC_FOLDER_FILE_NAMES_Fails');
					DirReadExFB.enable := FALSE;
					MachineState.NextState := ERROR;
				END_IF
			
			END_IF
			
		CLOSE_SRC_FOLDER:
			MachineState.TimeoutTimer.PT := T#10S;
			IF MachineState.NewStateTrigger THEN
				DirCloseFB.enable := FALSE;
				DirCloseFB.ident := FolderIdent;
				
			ELSE
				DirCloseFB.enable := TRUE;
				
				IF DirCloseFB.status <> ERR_FUB_ENABLE_FALSE AND DirCloseFB.status <> ERR_FUB_BUSY THEN 
					DirCloseFB.enable := FALSE;
					IF FileNum = 0 THEN
						MachineState.NextState := WAITING_COMMAND;
					ELSE
						MachineState.NextState := CHECK_DEST_FOLDER;
					END_IF
				END_IF
					
			END_IF
			
		CHECK_DEST_FOLDER:
			MachineState.TimeoutTimer.PT := T#15S;
			IF MachineState.NewStateTrigger THEN
				DirCreateFB.enable := FALSE;
				DirCreateFB.pName := ADR(DestinationFolder);
				
			ELSE
				DirCreateFB.enable := TRUE;
				
				IF DirCreateFB.status = ERR_OK OR DirCreateFB.status = fiERR_DIR_ALREADY_EXIST THEN
					DirCreateFB.enable := FALSE;
					MachineState.NextState := TRANSFER_FILES;
				ELSIF DirCreateFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF DirCreateFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_CHECK_DEST_FOLDER_Fails');
					DirCreateFB.enable := FALSE;
					MachineState.NextState := ERROR;
				END_IF
					
			END_IF
			
		TRANSFER_FILES:
			IF MachineState.NewStateTrigger THEN
				MachineState.TimeoutTimer.PT := MAX(T#5S,T#500ms * FileNum);
				ActFileNum := 1;
				DestinationFolder := CONCAT(DestinationFolder,'\');
				DestinationFileName := CONCAT(DestinationFolder,FileList[ActFileNum-1]);
				
				FileCopyFB.enable := FALSE;
				FileCopyFB.pSrcDev := ADR(SourceFileDevice);
				FileCopyFB.pSrc := ADR(FileList[ActFileNum-1]);
				FileCopyFB.pDestDev := ADR(USB_FILE_DEVICE);
				FileCopyFB.pDest := ADR(DestinationFileName);
				FileCopyFB.option := fiOVERWRITE;
				
			ELSE
				FileCopyFB.enable := TRUE;
				
				IF FileCopyFB.status = ERR_OK THEN
					FileCopyFB.enable := FALSE;
					ActFileNum := ActFileNum + 1;
					IF ActFileNum > FileNum THEN 
						IF TransferWithDelete THEN
							MachineState.NextState := DELETE_FILES;
						ELSE
							MachineState.NextState := WAITING_COMMAND;
						END_IF
					ELSE
						FileCopyFB.pSrc := ADR(FileList[ActFileNum-1]);
						DestinationFileName := CONCAT(DestinationFolder,FileList[ActFileNum-1]);
					END_IF
				ELSIF FileCopyFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF FileCopyFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_TRANSFER_FILES_Fails');
					FileCopyFB.enable := FALSE;
					MachineState.NextState := ERROR;
				END_IF
					
			END_IF
			
		DELETE_FILES:
			IF MachineState.NewStateTrigger THEN
				MachineState.TimeoutTimer.PT := MAX(T#5S,T#500ms * FileNum);
				ActFileNum := 1;
				
				FileDeleteFB.enable := FALSE;
				FileDeleteFB.pDevice := ADR(SourceFileDevice);
				FileDeleteFB.pName := ADR(FileList[0]);
				
			ELSE
				FileDeleteFB.enable := TRUE;
				
				IF FileDeleteFB.status = ERR_OK THEN
					FileDeleteFB.enable := FALSE;
					ActFileNum := ActFileNum + 1;
					IF ActFileNum > FileNum THEN 
						IF FileNum >= MAX_FILE_NUM_TO_EXPORT THEN
							MachineState.NextState := OPEN_SRC_FOLDER; // Repeat the entire process -> The src folder had more then the max number of manageble files
						ELSE
							MachineState.NextState := WAITING_COMMAND;
						END_IF
					ELSE
						FileDeleteFB.pName := ADR(FileList[ActFileNum-1]);
					END_IF
					
				ELSIF FileDeleteFB.status = ERR_FUB_ENABLE_FALSE THEN 
					// Wait
				ELSIF FileDeleteFB.status = ERR_FUB_BUSY THEN 
					// Wait
				ELSE		
					MpAlarmXSet(AlarmXCoreMpLink,'USBExport_DELETE_FILES_Fails');
					FileDeleteFB.enable := FALSE;
					MachineState.NextState := ERROR;
				END_IF
					
			END_IF
				            
		WAITING_STEP_TRIGGER:
			MachineState.TimeoutTimer.PT := T#0S;
			// Just wait the step trigger. The logic is managed in MachineStateManagementAction action.
		
		ERROR:
			MachineState.TimeoutTimer.PT := T#0S; // Timeout disabled in this state
			IF gUSBExport.Commands.Reset THEN
				MachineState.NextState := INIT;
			END_IF
			
        ELSE
            MachineState.NextState := INIT;			
        
    END_CASE
    
    FeedbacksUpdateAction;
    
	IF MachineState.ActualState <> CHECK_DEST_FOLDER THEN
		DirCreateFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> OPEN_SRC_FOLDER THEN
		DirOpenFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> CLOSE_SRC_FOLDER THEN
		DirCloseFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> GET_SRC_FOLDER_FILE_NAMES THEN
		DirReadExFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> TRANSFER_FILES THEN
		FileCopyFB.enable := FALSE;
	END_IF
	IF MachineState.ActualState <> DELETE_FILES THEN
		FileDeleteFB.enable := FALSE;
	END_IF	
	IF MachineState.ActualState <> WAITING_DIAGNOSTICS_EXPORT THEN
		SdmSystemDumpFB.enable := FALSE;
		AlarmXHistoryExportCommand := FALSE;
	END_IF
	
	DirCreateFB(pDevice := ADR(USB_FILE_DEVICE));
	DirCloseFB();
	FileCopyFB();
	FileDeleteFB();
	DirOpenFB();
	DirReadExFB();
	SdmSystemDumpFB(configuration := sdm_SYSTEMDUMP_DATA, pDevice := ADR(DiagnosticsFileDevice));
	
END_PROGRAM

PROGRAM _EXIT	
END_PROGRAM