open System.Collections
type Agent<'T> = MailboxProcessor<'T>
type Message = 
    | Apply of AsyncReplyChannel<bool>
    | Appropriate of int*Agent<Message>
    // TODO
    // Exclude
    // Demand
    // Allocate
    // Report

type ResAllocMethod = 
    | Queue 
    | Ration

type Holon(name) = 
    // member institution properties
    let mutable memberOf:Holon list = []
    let mutable offenceLevel = 0
    let mutable sanctionLevel = 0
    let mutable demanded = 0
    let mutable allocated = 0
    // TODO -> set characteristics of the agent
    // eg: propensity to misappropriate
    // TOTHINK -> difference between pow, per and obl?
    
    // supra-institution properties
    let mutable members:Holon list = []
    let mutable gatekeeper:Holon list = []
    let mutable head:Holon list = []
    let mutable sanctionLimit = 2
    let mutable membershipLimit = 0
    let mutable resources = 100
    let mutable demandQ:Holon list = []
    let mutable raMethod = Queue
    let mutable rationLimit = 20

    let addMember newbie = members <- (List.append members [newbie])      

    let agent = Agent.Start(fun agent ->
        let rec loop () = async {
            let! fullMsg = agent.Receive()
            match fullMsg with
            | Appropriate _ ->
                printfn "Received %A" fullMsg   
                return! loop()
            | Apply(replyCh) ->
                printfn "Institution %A received Apply" name
                replyCh.Reply(true) // include the applicant
                return! loop()                        
        }
        loop()
    )

    // TODO -> make properties set and get instead
    // Properties, so that they can be read
    member this.Self = agent
    member this.Name = name
    member this.MemberOf = memberOf
    member this.Members = members
    member this.Gatekeeper = List.tryHead gatekeeper
    member this.Head = List.tryHead head
    member this.SanctionLevel = sanctionLevel
    member this.SanctionLimit = sanctionLimit
    member this.MembershipLimit = membershipLimit
    member this.Resources = resources
    member this.DemandQ = demandQ
    member this.GetDemanded = demanded
    member this.RaMethod = raMethod
    member this.GetAllocated = allocated
    member this.RationLimit = rationLimit

    // Setting properties
    member this.JoinHolon h = memberOf <- (List.append memberOf [h])
    member this.SetMembers lst = members <- lst
    member this.AddMember h = members <- (List.append members [h])
    member this.SetGatekeeper holon = gatekeeper <- [holon]
    member this.SetHead holon = head <- [holon]
    /// Deduct sanctions by letting n be a negative number
    member this.AmendSanctions n = sanctionLevel <- sanctionLevel + n
    member this.SetSanctionLimit n = sanctionLimit <- n
    member this.SetInstSize n = membershipLimit <- n 
    member this.AmendResources n = resources <- resources + n
    member this.AmendDemandQ newQ = demandQ <- newQ
    member this.SetDemand d = demanded <- d
    member this.ChangeRaMethod meth = raMethod <- meth
    member this.SetAllocated n = allocated <- n
    member this.ChangeRationLimit r = rationLimit <- r

    /// Gatekeeper is empowered to include members into the institution
    member this.IncludeMember (inst:Holon) = 
        let instReply = 
            inst.Self.PostAndReply(fun replyChannel -> (Apply(replyChannel)))
        instReply    

