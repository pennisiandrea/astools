
TYPE
	ExportType : 	STRUCT  (*Export Main type*)
		Commands : ExportCommadsType;
		Feedbacks : ExportFeedbacksType;
	END_STRUCT;
	ExportCommadsType : 	STRUCT  (*Export Commands type*)
		Enable : BOOL;
		Reset : BOOL;
		ExportAlarms : BOOL;
		ExportRecipes : BOOL;
		ExportSettings : BOOL;
	END_STRUCT;
	ExportFeedbacksType : 	STRUCT  (*Export Feedbacks type*)
		Enabled : BOOL;
		USBDeviceConnected : BOOL;
		Error : BOOL;
		StateDescriptionID : USINT;
		ActFileNum : UINT;
		TotFileNum : UINT;
	END_STRUCT;
END_TYPE
