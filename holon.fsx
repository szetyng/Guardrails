type Agent<'T> = MailboxProcessor<'T>
type Message = 
    | Apply of AsyncReplyChannel<bool>
    | Appropriate of int*Agent<Message>
    // TODO
    // Exclude
    // Demand
    // Allocate
    // Report

type Holon(name) = 
    // member institution properties
    let mutable memberOf:Holon list = []
    let mutable offenceLevel = 0
    let mutable sanctionLevel = 0
    // TODO -> set characteristics of the agent
    // eg: propensity to misappropriate
    // TOTHINK -> difference between pow, per and obl?
    
    // supra-institution properties
    let mutable members:Holon list = []
    let mutable gatekeeper:Holon list = []
    let mutable head:Holon list = []
    let mutable sanctionLimit = 2
    let mutable membershipLimit = 0
    let mutable resources = 0

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
    
    /// Gatekeeper is empowered to include members into the institution
    member this.IncludeMember (inst:Holon) = 
        let instReply = 
            inst.Self.PostAndReply(fun replyChannel -> (Apply(replyChannel)))
        instReply    

/// Applicant applies to inst
let applyToInstitution (applicant:Holon) (inst:Holon) = 
    let crit = [applicant.SanctionLevel < inst.SanctionLimit ; inst.MembershipLimit > inst.MemberOf.Length]
    let rec checkCrit (supraH:Holon) (h:Holon) lst = 
        match lst with 
        | [] -> true
        | true::rest -> checkCrit supraH h rest
        | false::_ -> false
    let applicationResult = 
        match checkCrit inst applicant crit with
        | true ->
            match inst.Gatekeeper with
            | Some gatekeeper ->
                printfn "%A is applying to Institution %A via Gatekeeper %A" applicant.Name inst.Name gatekeeper.Name
                Some (gatekeeper.IncludeMember inst)
            | None -> None
        | false -> Some (false)

    match applicationResult with
    | Some true ->
        printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
        inst.AddMember(applicant)
        applicant.JoinHolon(inst)
    | Some false -> printfn "Result of agent %A applying to %A is %A" applicant.Name inst.Name applicationResult
    | None -> printfn "%A does not have a gatekeeper, or gatekeeper did not reply - it cannot admit new member %A" inst.Name applicant.Name  
