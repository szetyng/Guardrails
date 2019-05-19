type HolonID = int

type Rate = High | Medium | Low

type RoleIn = 
    | Member of HolonID

type MessageType = 
    | Tax of HolonID * int                      // Tax(Agent, Amount)
    | Subsidy of HolonID * int 

type Holon =
    { 
        ID : HolonID;
        Name : string;
        mutable Resources : int;
        ResourceCap : int;
        mutable MessageQueue : MessageType list;
        RefillRate : Rate list;
        mutable RoleOf : RoleIn option;
    }




