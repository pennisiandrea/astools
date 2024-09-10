
TYPE
	__module_name__Type : 	STRUCT  (*__module_name__ Main type*)
		Commands : __module_name__CommadsType;
		Feedbacks : __module_name__FeedbacksType;
		Settings : __module_name__SettingsType;
		Recipe : __module_name__RecipeType;
		HardwareIO : __module_name__HardwareIOType;
	END_STRUCT;
	__module_name__CommadsType : 	STRUCT  (*__module_name__ Commands type*)
		Enable : BOOL;
		Start : BOOL;
		Reset : BOOL;
	END_STRUCT;
	__module_name__FeedbacksType : 	STRUCT  (*__module_name__ Feedbacks type*)
		Enabled : BOOL;
		Waiting : BOOL;
		Error : BOOL;
	END_STRUCT;
	__module_name__RecipeType : 	STRUCT  (*__module_name__ Recipe type*)
		Var : BOOL;
	END_STRUCT;
	__module_name__SettingsType : 	STRUCT  (*__module_name__ Settings type*)
		Var : BOOL;
	END_STRUCT;
	__module_name__HardwareIOType : 	STRUCT  (*__module_name__ Hardware IO type*)
		Inputs : __module_name__HardwareIType;
		Outputs : __module_name__HardwareOType;
	END_STRUCT;
	__module_name__HardwareOType : 	STRUCT  (*__module_name__ Hardware Output type*)
		Var : BOOL;
	END_STRUCT;
	__module_name__HardwareIType : 	STRUCT  (*__module_name__ Hardware Input type*)
		Var : BOOL;
	END_STRUCT;
END_TYPE
