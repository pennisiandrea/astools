TYPE
    MachineStateType : 	STRUCT  (*Machine state main type*)
        OldState : MachineStateEnum; (*Actual state*)
        ActualState : MachineStateEnum; (*Actual state*)
        NextState : MachineStateEnum; (*Next state*)
        NewTriggerState : BOOL; (*Trigger state change*)
        TimeoutTimer : TON; (*State timeout*)
        StepByStepEnable : BOOL; (*Enable of Step by Step mode*)
        StepByStepTrigger : BOOL; (*Trigger to change step when Step by Step mode is active*)
    END_STRUCT;
    MachineStateEnum : 
        ( (*Machine State enumeration*)
        INIT, (*INIT state*)
        WAITING, (*WAITING state*)
        ERROR, (*ERROR state*)
        WAITING_STEP_TRIGGER (*WAITING trigger in StepByStep mode*)
        );
END_TYPE