TYPE
    prgTestType : 	STRUCT  (*prgTest Main type*)
        Commands : prgTestCommadsType;
        Feedbacks : prgTestFeedbacksType;
        Parameters : prgTestParametersType;
        Interface : prgTestInterfaceType;
    END_STRUCT;
    prgTestCommadsType : 	STRUCT  (*prgTest Commands type*)
        Enable : BOOL;
        Start : BOOL;
        Reset : BOOL;
    END_STRUCT;
    prgTestFeedbacksType : 	STRUCT  (*prgTest Feedbacks type*)
        Enabled : BOOL;
        Waiting : BOOL;
        Error   : BOOL;
    END_STRUCT;
    prgTestParametersType : 	STRUCT  (*prgTest Parameters type*)
        Var : BOOL;
    END_STRUCT;
    prgTestInterfaceType : 	STRUCT  (*prgTest Interface type*)
        Inputs : prgTestInterfaceInputsType;
        Outputs : prgTestInterfaceOutputsType;
    END_STRUCT;
    prgTestInterfaceOutputsType : 	STRUCT  (*prgTest Interface Output type*)
        Var : BOOL;
    END_STRUCT;
    prgTestInterfaceInputsType : 	STRUCT  (*prgTest Interface Input type*)
        Var : BOOL;
    END_STRUCT;
END_TYPE