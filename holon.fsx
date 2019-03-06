open System.Web.Services.Description
type HolonID = int

type RoleIn = 
    | Member of HolonID
    | Head of HolonID
    | Gatekeeper of HolonID
    | Monitor of HolonID

type MessageType = 
    | Applied of HolonID * HolonID          // Applied(Agent, Inst)
    | Demanded of HolonID * int * HolonID   // Demanded(Agent, Resources, Inst)

type Holon =
    { 
        ID : HolonID;
        Name : string;
        mutable Resources : int;
        mutable RoleOf : RoleIn option;
        mutable SanctionLevel : int;
        mutable OffenceLevel : int;
        mutable MessageQueue : MessageType list
    }

// Supra-institution might not have roles?
type Institution = 
    {
        Self: Holon;
        Head: Holon;
        Gatekeeper: Holon;
        //Monitor: Holon
    }


