
TYPE
	USBExportType : 	STRUCT  (*Export Main type*)
		Commands : USBExportCommadsType;
		Feedbacks : USBExportFeedbacksType;
	END_STRUCT;
	USBExportCommadsType : 	STRUCT  (*Export Commands type*)
		Enable : BOOL;
		Reset : BOOL;
		ExportDiagnostics : BOOL;
		ExportRecipes : BOOL;
		ExportSettings : BOOL;
		DialogOpened : BOOL;
	END_STRUCT;
	USBExportFeedbacksType : 	STRUCT  (*Export Feedbacks type*)
		Enabled : BOOL;
		USBDeviceConnected : BOOL;
		Error : BOOL;
		StateDescriptionID : USINT;
		ActFileNum : UINT;
		TotFileNum : UINT;
	END_STRUCT;
END_TYPE
