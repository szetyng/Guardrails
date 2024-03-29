# Guardrails

## Todo
- [x] implement functions for the physical actions of an agent/institution
  - [x] appropriate resources
  - [x] refill resources etc
- [x] don't send appropriation message if x=0
- [x] don't print all those messages anymore, too cluttered
- [ ] make sure that each message is removed from the queue after it has been acted on
  - [x] remove `Allocated` and `Appropriated` messages at the end of each time slice, since the monitor is not intended to sample each time slice
- [ ] feedback stuff, propensity to cheat stuff, revise behaviour stuff -> parameters to the physical functions
  - [ ] appropriation of resources takes in `r` amount of resources as an argument. Make this `r` decision here
  - [ ] when resources dwindle, call vote on changing stuff
- [x] make a script only for initialisation of agents (makes it easier when you change stuff like record properties)
- [x] only the top hierarchy refills, the middle hierarchy appropriates from them!
- [x] Clear all `VotedRaMeth(agent.ID)` messages after issue is closed

## Notes
- for some of the principles, work out if 'agent has to be a member of the institution' means `agent.RoleOf = Member` or as long as agent holds is in the institution, whether as Member, Monitor, Gatekeepr or Head.
- go through all functions and reevaluate where to remove messages from message queue and where not to - make a note in README.md and in comments if messages are removed
- in `allocateResources`, might be a problem if the head tries to allocate resources to someone who's not first in the queue, then the rest of the people might not be allocated as well. Depends on how simulation code is written so be careful

## Principle-related functions
Pure functions will take in inputs and return an output, with no side effects on the rest of the environment. However, the following functions will have side effects when it comes to things like changing the properties of agents and sending messages, which now that I think about it, shouldn't be a side effect?  

TODO: remove sending messages as a side-effect and make it a thing in the simulation instead (benefit: easier to keep track of when there are less side-effects). Or maybe not? Do things that make sense from the function name's POV, e.g.: assume `applyToInst` WILL send the message `Applied`, rather than just returning the message type without doing anything with it. Include in comment/documentation when functions send messages like so.

### Principle 1: Clearly defined boundaries
**Notes**
- holons can apply for membership as long as they are institution-less and they are within the sanction limit - these are the only criteria for membership, nothing else
- holons will be excluded from the institution when their sanction level exceeds the limit allowed

`applyToInst agent inst`
- will be successful if:
  - `agent` does not already occupy a role in `inst`
  - `agent` qualifies to be a member of `inst` (not implemented)
- side-effect: sends `Applied` to `inst.MessageQueue`

`includeToInst gatekeep agent inst`
- side-effect: 
  - `agent.RoleOf` is changed to `Some Member(inst.ID)`
- will be successful if gatekeeper is **empowered** to include members

`powToInclude gatekeep agent inst` -> `bool`
- returns `true` if:
  - there is a message in the `inst.MessageQueue` corresponding to `agent`'s application for membership
  - `gatekeep` occupies the role of gatekeeper in `inst`
  - `agent` has been approved as a member (not implemented)  
- side-effect: removes `Applied` from `inst.MessageQueue` if it exists. Easily changed to not remove the message if need be.

TODO: 
- [ ] exclude members, after implementing sanctions part

### Principle 2: Congruence
`demandResources agent r inst` 
- will be successful if `agent` is **empowered** to demand for resources from `inst`
- side-effect: `inst.MessageQueue` receives `Demanded(A, R, I)`

`powToDemand agent inst step` -> `bool`
- returns `true` if
  - `agent` is a member of `inst`
  - `agent` has not demanded from `inst` in this time slice
  - `agent` sanction level is 0

`powToAllocate head inst agent r` -> `int` (amount of resources allocated)
- side-effect: remove demand from message queue
- `raMethod = inst.RaMethod`
- returns `true` if
  - `agent` has made a demand of `r` to `inst`
  - `head` occupies role of head in `inst`
  - case: get `r` if `raMethod` = `queue` and if
    - the demand is at the head of the demand queue
    - if `r` <= amount of resources we have
    - `inst.RaMethod` is `queue`
  - case: `raMethod` is `ration` and if
    - the demand is that the head of the demand queue
    - `inst.RaMethod` is `ration`
    - if demand `r` is more than ration -> get ration 
    - if demand `r` is less than or equal to ration -> get demand `r`

### Principle 3: Collective Choice Arrangements
**TODO**
- [x] implement physical action to open and close issues
- [ ] when voting on `Ration`, how to decide what amount to set as ration?

**Notes**
- there's only one type of issue regarding resource allocation methods. Assuming only allowed to vote on this one type of issue. Other types are not required for now.

`powToVote agent inst issue` -> `bool`
- returns `true` if 
  - `agent` is a member of `inst`
  - status of `issue` is open
  - `agent` has not yet voted on `issue` in `inst`

`doVote agent inst issue vote`
- will succeed if
  - `agent` has the power to vote on this `issue` in this `inst`
- side-effect:
  - sends two messages to `inst` that the vote has been added, and that `agent` has voted (privately, won't know what `agent`'s vote is)

`declareWinner head inst issue` 
- will succeed if
  - `head` had the power to do so
- side-effect:
  - changes whatever `inst.issue` was to the winner using `inst`'s winner determination method. removes votes from message queue
- currently only has `Plurality` as a winner determination method

`powToDeclare head inst issue` -> `bool`
- returns `true` if
  - `issue` is closed
  - `head` is head of `inst`

### Principle 4: Monitoring
**Notes**
- in the long axiomatization paper, monitor has two responsibilities: power to report misbehaving agent after observing appropriations, and obligation to report current level of resources in the inst (i.e. sample the state of the environment)
- only implementing the former for now. A report of misappropriation can lead to a sanction (P5) and a dispute (P6)
- as for the latter, the current level of resources can be used to change P2's resource allocation methods by using P3's voting methods.

**TODO**
- [ ] figure out how to do the third bullet point above in a simulation

`powToAssignMonitor head monitor inst` -> `bool`
- returns `true` if 
  - `head` is head of inst
  - `monitor` is *currently* a member of inst

`assignMonitor head monitor inst`
- side-effect: monitor's `RoleOf` becomes `Some Monitor`
- will succeed if head has the power to assign monitor

`powToReport monitor agent inst` -> `bool`
- returns `true` is 
  - `monitor` is monitor
  - `agent` is member

### Principle 5: Graduated sanctions
**Todo**
- [ ] after monitor reports stuff, how to sanction:
  - [ ] activate p5 in simulation, which makes head check each member's offence level and sanction them accordingly
  - [ ] problem: if member appealed in previous time slice, head will automatically re-sanction something that was previously pardoned? Include a message type, `Forgiven` or something to keep track. Never remove this message from the queue
  
**Notes**
- very closely related to principle 6
- sanction level = 1: cannot make demands anymore
- sanction level = 2: head is empowered to exclude agent
- head can reset agent's sanction level to 0 so that it can make demands again (via p6), but it will not reset its offence level. If the agent misbehaves AGAIN after its sanction level has been reset to 0 but offence level remains at 1 -> its offence level increases to 2, sanction level set to 2 -> agent is excluded from the institution.
- above bullet point is disregarded. If head upholds appeal, then both the offence level and sanction level are decremented. Might want to implement a reset sanction function that does the above bullet point?

`reportGreed monitor agent inst`
- side-effect: increments agent's offence level by 1
- will succeed if:
  - agent appropriates more than it has been allocated

`sanctionMember head agent inst`
- side-effect: changes agent's sanction level to its offence level
- will succeed if:
  - `head` has the power to sanction in this `inst`

`powToSanction head inst` -> `bool`
- return `true` if head is head of inst

### Principle 6: Conflict resolution
**Notes**
- only implemented simple appeals procedure, more complex alternative dispute resolution (ADR) methods can be done in future work
- `upholdAppeal` in axiomatisation paper under p6 involves decrementing both the sanction level and the offence level. However, in p5, the paper said that offence level will not be decremented. Currently not decrementing offence level under the reasoning that otherwise there is no use of having separate these two levels in the first place
  - offence level can be used to keep track of agent's criminal activity
  - sanction level is used to keep track of what punishment the agent is currently undergoing
- re: above bullet point. Disregarded. Both levels are decremented, to show the difference between a monitor doing its job and a head doing its job

`appealSanction agent s inst`
- side-effect: send message `Appeal (Agent, s, Inst)` to `inst.MessageQueue`
- will succeed if:
  - `agent` has the power to appeal

`powToAppeal agent s inst` -> `bool`
- returns `true` if:
  - `agent` has role `Member` in inst
  - `agent` sanction level is `s`

`upholdAppeal head agent s inst`
- side-effect: `agent` sanction level and offence level are decremented by 1
- will succeed if:
  - head has the power to uphold sanctions for this agent 

`powToUphold head agent s inst` -> `bool`
- returns `true` if:
  - `head` is head of inst
  - `agent` has submitted an appeal for sanction level `s` in `inst`
- side-effect: removes `Appeal` message from `inst.MessageQueue`


## Physical abilities of agents
### Misc
`refillResources inst r`
- side-effect: inst.Resources += r

### Principle 2
`allocateResources head inst agent`
- side-effect: inst.MessageQueue gets a new `Allocated` message
- will succeed if:
  - head has the power to allocate resources to the agent (agent needs to have already made a demand)

`appropriateResources agent inst r`
- side-effect: 
  - agent.Resources += some amount
  - inst.Resources -= some amount
  - inst.MessageQueue gets a new `Appropriated` message
- some amount = r if there inst has more than r resources available
- some amount = the entire inst.Resources otherwise, and the CPR is drained completely

### Principle 3
`openIssue head inst`
- side-effect: inst.IssueStatus = true
- will succeed if:
  - head is head

`closeIssue head inst`
- side-effect: inst.IssueStatus = false
- will succeed if:
  - head is head

### Principle 4
**Todo**
- [ ] please rewrite `monitorDoesJob`

`monitorDoesJob monitor inst agents`
- side-effect: agents who were greedy would have their OffenceLevel be incremented by 1
- monitor goes through inst.MessageQueue and collects all `Allocated` and `Appropriated` messages
- for each `Appropriated` message, check if there was a corresponding `Allocated` message
  - if there is no corresponding `Allocated` message, the agent is not allowed to appropriate 
  - if the corresponding `Allocated` message indicates that the agent took more than was given, then that's not good

### Principle 5
`headDoesJob head inst agents` 
- side-effect: some agents would have their sanction level be modified to be equal to their offence level
- head goes through all the agents in the inst and checks if there was a need to change the sanction level of the agent

### Principle 6
`headFeelsForgiving head inst agents`
- side-effect: for each agent who appealed, their OffenceLevel and SanctionLevel are decremented by 1
- head goes through MessageQueue of inst to get all `Appeal` messages
- for each `Appeal` message, head will uphold the appeal and forgive the agent's mistake

## Decision-making abilities of agents
**Todo**
- [x] p2: how does agent decide how much to demand?
- [ ] p2: how does agent decide how much to appropriate? According to allocation, or according to greedy wants?
- [x] p3: how does head decide to open issue?
  - for now: when resource level <= `k * resources`. `k` is a percentage that represents safety margin of the institution
- [ ] p3: how does head of top holon decide to open issue?
- [x] p3: how does agent decide on which `raMethod` to vote for?
- [ ] p3: if Ration wins, how does Head decide on what R to use. Something that works in tandem with `declareWinner`?
  - [ ] count number of members in an inst so that we can divide stuff
- [x] p4: monitor decides whether or not it wants to do its job -> if random number <= monitoring freq, where freq is in range (0,1)
- [ ] p6: how does agent decide if they want to appeal? Probably similar to compliancy in p2

### P2: how does agent decide how much to demand?
- Greedy: if greediness value is larger than 0.5 -> demand for 0.75 of capacity
- Not greedy: if greediness value is less than or equal to 0.5 -> demand for half of capacity
- base holons are mostly not greedy
- all intermediary holons are greedy to be safe

### P3 
How does head decide if they should open the issue for voting? Only for intermediary holons
- open issue if `resources <= tMin*capacity` or `resources >= tMax*capacity`
- for now, `tMin=0.25` and `tMax=0.75` for intermediary holons

How does agent decide on which raMethod to vote for?
- Random
- Scraped idea: if `resources > 0.4*capacity`, then vote for Queue. Else, Ration

How does head decide the amount to ration if Ration wins?
- `resources/nrOfMembers`, except that `nrOfMembers is temporarily fixed as 9 at the moment


**Maybe todo, maybe simplify**
- [ ] p1: how does homeless agent decide to apply or not? 
- [ ] p1: how does member decide if they want to stay or leave the institution?

Might just do away with application and decision to stay or leave institution, just create fixed institutions for now, no way for member to voluntarily leave (but heads can still exclude misbehaving members).

## Initialisation
A list of all the holonic agents are passed to the `simulate` function. In the beginning, a list of `supraHolons` are identified from the list of holons. In this project, there will be 3 `supraHolons` with the following hierarchy:
```
                offices
               /       \
            parks     brooklyn
              |          |
        Head=ron        Head=ray
        Gate=leslie     Gate=terry
        Monitor=ben     Monitor=amy
        Other members   Other members
```

Thus, `supraHolons = [offices ; parks ; brooklyn]`. `offices.RoleOf = None`, while `parks` and `brooklyn` would have `RoleOf = Some (Member (offices))`.  

Initially, every holon has 0 resources, except for the supra-holons. The amount changes when the member holon appropriates resources from the supra-holon, using `appropriateResources agent r inst` which has the previously mentioned side-effects.

Initially, the supra-holon at the highest hierarchy will have 500 resources while the supra-holons below it will have 200 - these are the maximum capacity of the holons. Base holons' maximum capacity is 20.

In this experiment, `parks` and `brooklyn` have 9 members each (people in power do not make demands), and each member would demand for 10 resources in each time slice. Thus, at the end of the time slice, the supra-holon will only have 110 resources left if each member appropriates 10 resources each. 

In accordance to principle 8, `parks` and `brooklyn` are also empowered to make demands to `offices`. The resources of the top-most supra-holon, i.e. `offices`, are refilled near the end of each time slice (after appropriation of resources and paying the monitor).

The `Head` of each supra-holon (`parks`, `brooklyn` and `offices`) will then decide whether or not they would want to call for a vote on changing the resource allocation method - based on the amount of resources left. This kicks off principle 3. Principle 3 starts after members have made appropriations and after monitoring is done, BEFORE the institution is refilled, and before the intermediary holons make their appropriations.

In every time slice, the resources can be refilled at either high, medium or low levels (not exceeding their maximum capacity, of course). The experiment will go on for 50 time slices; the refill rate will be changed in blocks of 5 time slices, so it changes 10 times in this repeating sequence: high, high, medium, low.    

| Rate    | Resource amount after refill            |
|---|---|
| High    | `min(ResourceCap, R+ResourceCap)`       |
| Medium  | `min(ResourceCap, R+0.5*ResourceCap)`   |
| Low     | `min(ResourceCap, R+0.25*ResourceCap)`  |  



## Simulation
Want to go through each agent and see what they want to do. This will have a lot to do with agent behaviour, compliancy, level of resources (if they poor, utility of cheating becomes higher etc). 

Also, for simplicity and to make sure that they are never expelled from the institution (thus, assume that they do their jobs perfectly), `Head`, `Monitor` and `Gatekeeper` do not make demands and do not appropriate resources. `Monitor` still gets paid to do its job, since this cost is not meant to incentivise monitor (assume perfect monitor) but to deter the institution from having them sample the environment at every time slice.