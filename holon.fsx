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

type WinDetMethod = 
    | Plurality

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
    let mutable gatekeeper:Holon option = None
    let mutable head:Holon option= None
    let mutable sanctionLimit = 2
    let mutable membershipLimit = 0
    let mutable resources = 0
    let mutable demandQ:Holon list = []
    let mutable raMethod = Queue
    let mutable wdMethod = Plurality
    let mutable rationLimit = 20
    let mutable issue = false
    let mutable votelist:ResAllocMethod list = []

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
    // Read-only properties
    member this.Self = agent
    member this.Name = name

    // Member holon properties
    member this.MemberOf = memberOf
    member this.JoinHolon h = memberOf <- (List.append memberOf [h])
    member this.SanctionLevel  /// Deduct sanctions by letting n be a negative number 
        with get() = sanctionLevel
        and set(change) = sanctionLevel <- sanctionLevel + change     
    member this.Demanded
        with get() = demanded
        and set(n) = demanded <- n    
    member this.Allocated 
        with get() = allocated
        and set(n) = allocated <- n      


    // Supra-holon properties
    member this.Members 
        with get() = members
        and set(memLst) = members <- memLst
    member this.AddMember h = members <- (List.append members [h])    
    member this.Head
        with get() = head
        and set(holon) = head <- holon
    member this.Gatekeeper
        with get() = gatekeeper
        and set(holon) = gatekeeper <- holon   
    member this.SanctionLimit
        with get() = sanctionLimit
        and set(n) = sanctionLimit <- n     
    member this.MembershipLimit 
        with get() = membershipLimit
        and set(n) = membershipLimit <- n    
    member this.DemandQ 
        with get() = demandQ
        and set(newQ) = demandQ <- newQ
    member this.RaMethod
        with get() = raMethod
        and set(meth) = raMethod <- meth    
    member this.WdMethod
        with get() = wdMethod
        and set(meth) = wdMethod <- meth    
    member this.RationLimit 
        with get() = rationLimit
        and set(n) = rationLimit <- n    
    member this.Issue
        with get() = issue
        and set(b) = issue <- b 
    member this.Votelist = votelist   
    member this.AddVote v = votelist <- votelist @ [v]
    member this.ClearVotes = votelist <- []   
    
    // Both
    member this.Resources 
        with get() = resources
        and set(n) = resources <- resources + n   
            
    /// Gatekeeper is empowered to include members into the institution
    member this.IncludeMember (inst:Holon) = 
        let instReply = 
            inst.Self.PostAndReply(fun replyChannel -> (Apply(replyChannel)))
        instReply    

