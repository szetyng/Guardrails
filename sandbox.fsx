type ComplexNumber = { real: float; imaginary: float }

type Holon = {name: string; mutable memberOf: Holon option}

let leslie = {name="leslie"; memberOf=None}
let parks = {name="parks"; memberOf=None}

leslie.memberOf <- Some parks 
leslie.memberOf