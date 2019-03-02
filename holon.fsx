open System.Web.Services.Description
type HolonID = int

type RoleIn = 
    | Member of HolonID
    | Head of HolonID
    | Gatekeeper of HolonID
    | Monitor of HolonID

type MessageType = 
    | Applied of HolonID * HolonID

type Holon =
    { 
        ID : HolonID;
        Name : string;
        mutable Resources : int;
        mutable RoleOf : RoleIn option;
        mutable SanctionLevel : int;
        mutable OffenceLevel : int;
        // msg queue member
        mutable MessageQueue : MessageType list
    }

type Mailman() = 
    // something
    static let addToQ oldQ msg = oldQ @ [msg]

    static let inbox = MailboxProcessor.Start(fun inbox ->
        let rec loop q = async {
            let! msg = inbox.Receive()
            match msg with
            | Applied (a,i) -> 
                // do something
                let newState = addToQ q msg 
                return! loop newState
        }
        loop []
    )

    static member Send m = inbox.Post m
