type Agent<'T> = MailboxProcessor<'T>
type Message = 
    | Apply of Agent<Message>*Agent<Message>*AsyncReplyChannel<bool>
    | Appropriate of int*Agent<Message>

type Holon(name) = 
    // member institution properties
    let mutable memberOf:Holon list = []
    let mutable sanctionLevel = 0

    // supra-institution properties
    let mutable members:Holon list = []
    let mutable gatekeeper:Holon list = []
    let mutable head:Holon list = []
    let mutable sanctionLimit = 0
    let mutable membershipLimit = 0

    let membershipCriteria inst applicant = true

    let checkApplication (inst:Agent<Message>) (applicant:Agent<Message>) (replyCh:AsyncReplyChannel<bool>) = 
        if membershipCriteria inst applicant then
            replyCh.Reply(true)
        else 
            replyCh.Reply(false)
    let addMember newbie = members <- (List.append members [newbie])      

    let agent = Agent.Start(fun agent ->
        let rec loop () = async {
            let! fullMsg = agent.Receive()
            match fullMsg with
            | Appropriate _ ->
                printfn "Received %A" fullMsg   
                return! loop()
            | Apply(inst,friend,replyCh) ->
                //printfn "Received %A" fullMsg
                printfn "%A received Apply" name 
                checkApplication inst friend replyCh
                return! loop()                        
        }
        loop()
    )

    // Properties
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
            hplusGatekeeper.Self.PostAndReply(fun replyChannel -> (Apply(inst,agent,replyChannel)))
        reply        

/// Applicant applies to inst  
/// inst's gatekeeper checks the application
// maybe include criteria as a function input
let applyToInstitution (applicant:Holon) (inst:Holon) = 
    let crit = [applicant.SanctionLevel > inst.SanctionLimit ; inst.MembershipLimit > inst.MemberOf.Length]
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
    let parks = new Holon("parks",100)

    parks.SetMembers parksLst
    List.map (fun (h:Holon) -> h.JoinHolon parks) parksLst |> ignore

    parks.SetGatekeeper (List.head parksLst)

    // Principle 1: Membership criteria
    parks.SetSanctionLimit 20
    parks.SetInstSize 6
    
    parks

let leslie = new Holon("leslie",100)
let ron = new Holon("ron",100)
let tom = new Holon("tom", 70)
let april = new Holon("april", 70)
let parksLst = [leslie; ron; tom; april]
let parks = createParks parksLst


let ben = new Holon("ben",100)
parks.Members
applyToInstitution ben parks

parks.Members.Length
ben.MemberOf

let mark = new Holon("mark",10)
applyToInstitution mark parks

