# Guardrails

## Todo


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

**FROM HERE ONWARDS, HAS NOT BEEN DONE**  
`powToAllocate head inst agent r raMethod` -> `bool`
- returns `true` if
  - `agent` has made a demand of `r` to `inst`
  - `head` occupies role of head in `inst`
  - case: `raMethod` = `queue` and if
    - the demand is at the head of the demand queue
    - `inst.RaMethod` is `queue`
  - case: `raMethod` is `ration` and if
    - the demand is that the head of the demand queue
    - `inst.RaMethod` is `ration`
    - if demand `r` is more than ration -> get ration (TODO: side-effect?)
    - if demand `r` is less than or equal to ration -> get demand



### Principle 3: Collective Choice Arrangements
Agent `A` is empowered to vote on issue M if the issue is open and A is a member of the institution. This adds A's vote to the votelist. The head is obligated to declare a winner of the vote using whatever `WinnerDeterminationMethod` is being employed, when the issue is closed.

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

Want to go through each agent and see what they want to do.