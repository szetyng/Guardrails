open System.Web.Services.Description
open System.Collections.Generic
type HolonID = int

type RoleIn = 
    | Member of HolonID
    | Head of HolonID
    | Gatekeeper of HolonID
    | Monitor of HolonID

type ResAllocMethod = 
    | Queue
    | Ration of int option

type WinDetMethod = 
    | Plurality 

type MessageType = 
    | Applied of HolonID * HolonID              // Applied(Agent, Inst) TODO: is inst nec if it is being sent to inst?
    | Demanded of HolonID * int * HolonID       // Demanded(Agent, Resources, Inst)
    | Allocated of HolonID * int * HolonID      // Allocated(Agent, Resources, Inst)
    | Vote of ResAllocMethod * HolonID          // VoteRaMeth(raMethod, Inst)          
    | VotedRaMeth of HolonID                    // VotedRaMeth(Agent) 
    | Appropriate of HolonID * int * HolonID    // Appropriate(Agent, Resources, Inst)
    | Appeal of HolonID * int * HolonID         // Appeal(Agent, SanctionLevel, Inst)

type Holon =
    { 
        // Common to every holon
        ID : HolonID;
        Name : string;
        mutable Resources : int;
        mutable MessageQueue : MessageType list;

        // For sub-holons        
        mutable RoleOf : RoleIn option;
        mutable CompliancyDegree : float;
        mutable SanctionLevel : int;
        mutable OffenceLevel : int;

        // For supra-holons
        mutable RaMethod : ResAllocMethod option;
        mutable WdMethod : WinDetMethod option;
        mutable MonitoringFreq : float;
        mutable MonitoringCost : int
        mutable IssueStatus : bool
    }




