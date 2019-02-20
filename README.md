# Guardrails

## Todo
- make functions functional
- how to make institutional facts??
- split script into different principles so that it is easier to run

## Principles
### Principle 1: Clearly defined boundaries
Agent `A` can 
- `applyToInstitution` if it wasn't already a member, and if it qualifies as a member

`Gatekeeper` in a supra-holon is empowered to 
- `IncludeMember` if an agent has applied to the supra-holon and it qualifies (and hence is approved) as a member
- `ExcludeMember` if a member agent has exceeded the `sanctionLevel` of 2 (consider making this the job of a `Head`)

### Principle 2: Congruence
Agent `A` is empowered to 
- `Demand` R amount of resources from a supra-holon if `A` is a member of the supra-holon and has not demanded anything in this time slice (?) and it has not received any sanctions

Valid demands are added to the `demandQ`, and the `Head` is empowered to `Allocate` resources based on the resource allocation method. 

Initially, every holon has 0 resources, except for the supra-holon, which has 100. The amount changes when the member holon appropriates resources from the supra-holon.

### Principle 3: Collective Choice Arrangements
Agent `A` is empowered to vote on issue M if the issue is open and A is a member of the institution. This adds A's vote to the votelist. The head is obligated to declare a winner of the vote using whatever `WinnerDeterminationMethod` is being employed, when the issue is closed.