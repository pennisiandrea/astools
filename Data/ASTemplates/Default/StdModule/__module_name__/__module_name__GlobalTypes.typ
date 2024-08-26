TYPE
    __module_name__Type : 	STRUCT  (*__module_name__ Main type*)
        Commands : __module_name__CommadsType;
        Feedbacks : __module_name__FeedbacksType;
        Parameters : __module_name__ParametersType;
        Interface : __module_name__InterfaceType;
    END_STRUCT;
    __module_name__CommadsType : 	STRUCT  (*__module_name__ Commands type*)
        Enable : BOOL;
        Start : BOOL;
        Reset : BOOL;
    END_STRUCT;
    __module_name__FeedbacksType : 	STRUCT  (*__module_name__ Feedbacks type*)
        Enabled : BOOL;
        Waiting : BOOL;
        Error   : BOOL;
    END_STRUCT;
    __module_name__ParametersType : 	STRUCT  (*__module_name__ Parameters type*)
        Var : BOOL;
    END_STRUCT;
    __module_name__InterfaceType : 	STRUCT  (*__module_name__ Interface type*)
        Inputs : __module_name__InterfaceInputsType;
        Outputs : __module_name__InterfaceOutputsType;
    END_STRUCT;
    __module_name__InterfaceOutputsType : 	STRUCT  (*__module_name__ Interface Output type*)
        Var : BOOL;
    END_STRUCT;
    __module_name__InterfaceInputsType : 	STRUCT  (*__module_name__ Interface Input type*)
        Var : BOOL;
    END_STRUCT;
END_TYPE