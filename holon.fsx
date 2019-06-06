type HolonID = int

type Rate = High | Medium | Low

type RoleIn = 
    | Member of HolonID

type MessageType = 
    | Tax of HolonID * float                      // Tax(Agent, Amount)
    | Subsidy of HolonID * float 

type Holon =
    { 
        ID : HolonID;
        Name : string;
        mutable Resources : float;
        mutable SupraResources : float;
        ResourceCap : float;
        mutable MessageQueue : MessageType list;
        RefillRate : Rate list;
        mutable RoleOf : RoleIn option;
        MonitoringCost : float;
    }




