type Agent<'T> = MailboxProcessor<'T>
type Message = 
    | Apply of Agent<Message>*Agent<Message>*AsyncReplyChannel<bool>
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

    let addMember newbie = members <- (List.append members [newbie])      
    let includeMember mem (replyCh:AsyncReplyChannel<bool>) = replyCh.Reply(true)

    let agent = Agent.Start(fun agent ->
        let rec loop () = async {
            let! fullMsg = agent.Receive()
            match fullMsg with
            | Appropriate _ ->
                printfn "Received %A" fullMsg   
                return! loop()
            | Apply(inst,friend,replyCh) ->
                //printfn "Received %A" fullMsg
                // I am the gatekeeper
                printfn "%A received Apply" name 
                includeMember friend replyCh
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
    member this.SendApplication (inst:Agent<Message>) (hplusGatekeeper:Holon) = 
        let reply = 
            hplusGatekeeper.Self.PostAndReply(fun replyChannel -> (Apply(inst, agent, replyChannel)))
        reply        

/// Applicant applies to inst  
/// inst's gatekeeper checks the application
// maybe include criteria as a function input
let applyToInstitution (applicant:Holon) (inst:Holon) = 
    let crit = [applicant.SanctionLevel < inst.SanctionLimit ; inst.MembershipLimit > inst.MemberOf.Length]
    let rec checking (supraH:Holon) (h:Holon) (lst:bool list) = 
        match lst with
        | [] -> true
        | true::rest -> checking supraH h rest
        | false::_ -> false
    let ans =
        match checking inst applicant crit with
        | true -> 
            match inst.Gatekeeper with
            | Some gatekeeper ->
                printfn "%A sending Apply to Institution %A via Gatekeeper %A" applicant.Name inst.Name gatekeeper.Name
                Some (applicant.SendApplication inst.Self gatekeeper)
            | None -> None           
        | false -> Some (false)
    
    match ans with
        | Some true -> 
            printfn "result of member %A applying to %A is %A" applicant.Name inst.Name ans
            inst.AddMember(applicant)
            applicant.JoinHolon(inst)
        | Some false ->    
            printfn "result of member %A applying to %A is %A" applicant.Name inst.Name ans  
        | None -> printfn "%A does not have a gatekeeper, it cannot admit new member %A" inst.Name applicant.Name  




let createParks(parksLst) =
    let parks = Holon("parks")

    parks.SetMembers parksLst
    List.map (fun (h:Holon) -> h.JoinHolon parks) parksLst |> ignore

    parks.SetGatekeeper (List.head parksLst)

    // Principle 1: Membership criteria
    parks.SetSanctionLimit 2
    parks.SetInstSize 6
    
    parks

let leslie = Holon("leslie")
let ron = Holon("ron")
let tom = Holon("tom")
let april = Holon("april")
let parksLst = [leslie; ron; tom; april]
let parks = createParks parksLst


let ben = Holon("ben")
parks.Members
applyToInstitution ben parks

parks.Members.Length
ben.MemberOf

let mark = Holon("mark")
applyToInstitution mark parks

