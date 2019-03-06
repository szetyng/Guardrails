type HolonID = int

type Role = 
    | Member
    | Head
    | Gatekeeper
    | Monitor

type InstFacts = 
    | RoleOf of HolonID * HolonID option * Role option
    | Applied of HolonID * HolonID
    

type Holon =
    { 
        ID : HolonID;
        Name : string;
        mutable Resources : int;
        mutable MemberOf : Holon option;
        mutable Role : Role option;
        mutable SanctionLevel : int;
        mutable OffenceLevel : int;
        mutable Members : Holon list;
    }

