# Guardrails

## Todo
- split script into different principles so that it is easier to run

## Principle-related functions
Pure functions will take in inputs and return an output, with no side effects on the rest of the environment. However, the following functions will have side effects when it comes to things like changing the properties of agents and sending messages, which now that I think about it, shouldn't be a side effect.   

TODO: remove sending messages as a side-effect and make it a thing in the simulation instead (benefit: easier to keep track of when there are less side-effects)

### Principle 1: Clearly defined boundaries
`applyToInst agent inst` -> `MessageType option`
- if successful, output is `Some Applied(A)`
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
- side-effect: removes `Applied` from `inst.MessageQueue` if it exists

TODO: exclude members, after implementing sanctions part

### Principle 2: Congruence
`demandResources agent r inst` -> `MessageType option`
- will be successful if `agent` is **empowered** to demand for resources from `inst`
- side-effect: `inst.MessageQueue` receives `Demanded(A, R, I)`

`powToDemand agent inst step` -> `bool`
- returns `true` if
  - `agent` is a member of `inst`
  - `agent` has not demanded from `inst` in this time slice
  - `agent` sanction level is 0

`powToAllocate head inst agent r raMethod` -> `bool`
- returns `true` if
  - 

Agent `A` is empowered to 
- `Demand` R amount of resources from a supra-holon if `A` is a member of the supra-holon and has not demanded anything in this time slice (?) and it has not received any sanctions

Valid demands are added to the `demandQ`, and the `Head` is empowered to `Allocate` resources based on the resource allocation method. 

Initially, every holon has 0 resources, except for the supra-holon, which has 100. The amount changes when the member holon appropriates resources from the supra-holon.

### Principle 3: Collective Choice Arrangements
Agent `A` is empowered to vote on issue M if the issue is open and A is a member of the institution. This adds A's vote to the votelist. The head is obligated to declare a winner of the vote using whatever `WinnerDeterminationMethod` is being employed, when the issue is closed.