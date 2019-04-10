# Guardrails

## Todo
- implement functions for the physical actions of an agent/institution
  - appropriate resources
  - refill resources etc
- feedback stuff, propensity to cheat stuff, revise behaviour stuff -> parameters to the physical functions
  - appropriation of resources takes in `r` amount of resources as an argument. Make this `r` decision here
  - when resources dwindle, call vote on changing stuff
- make a script only for initialisation of agents (makes it easier when you change stuff like record properties)

## Notes
- for some of the principles, work out if 'agent has to be a member of the institution' means `agent.RoleOf = Member` or as long as agent holds is in the institution, whether as Member, Monitor, Gatekeepr or Head.
- go through all functions and reevaluate where to remove messages from message queue and where not to - make a note in README.md and in comments if messages are removed

## Principle-related functions
Pure functions will take in inputs and return an output, with no side effects on the rest of the environment. However, the following functions will have side effects when it comes to things like changing the properties of agents and sending messages, which now that I think about it, shouldn't be a side effect?  

TODO: remove sending messages as a side-effect and make it a thing in the simulation instead (benefit: easier to keep track of when there are less side-effects). Or maybe not? Do things that make sense from the function name's POV, e.g.: assume `applyToInst` WILL send the message `Applied`, rather than just returning the message type without doing anything with it. Include in comment/documentation when functions send messages like so.

### Principle 1: Clearly defined boundaries
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

TODO: exclude members, after implementing sanctions part

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
- implement physical action to open and close issues
- when voting on `Ration`, how to decide what amount to set as ration?

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
- figure out how to do the third bullet point above in a simulation

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
**Notes**
- very closely related to principle 6
- sanction level = 1: cannot make demands anymore
- sanction level = 2: head is empowered to exclude agent
- head can reset agent's sanction level to 0 so that it can make demands again (via p6), but it will not reset its offence level. If the agent misbehaves AGAIN after its sanction level has been reset to 0 but offence level remains at 1 -> its offence level increases to 2, sanction level set to 2 -> agent is excluded from the institution.

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

`appealSanction agent s inst`
- side-effect: send message `Appeal (Agent, s, Inst)` to `inst.MessageQueue`
- will succeed if:
  - `agent` has the power to appeal

`powToAppeal agent s inst` -> `bool`
- returns `true` if:
  - `agent` has role `Member` in inst
  - `agent` sanction level is `s`

`upholdAppeal head agent s inst`
- side-effect: `agent` sanction level is decremented by 1
- will succeed if:
  - head has the power to uphold sanctions for this agent 

`powToUphold head agent s inst` -> `bool`
- returns `true` if:
  - `head` is head of inst
  - `agent` has submitted an appeal for sanction level `s` in `inst`
- side-effect: removes `Appeal` message from `inst.MessageQueue`


## Physical abilities of agents
### Initialisation
Initially, every holon has 0 resources, except for the supra-holon, which has 100. The amount changes when the member holon appropriates resources from the supra-holon, using `appropriateResources agent r inst` which has the previously mentioned side-effects.

### Simulation
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

Want to go through each agent and see what they want to do. This will have a lot to do with agent behaviour, compliancy, level of resources (if they poor, utility of cheating becomes higher etc). 