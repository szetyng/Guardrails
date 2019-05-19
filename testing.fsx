#load "packages/FSharp.Charting/FSharp.Charting.fsx"
open FSharp.Charting

Chart.Line [ for x in 0 .. 10 -> x, x*x ] |> Chart.Show
Chart.Combine ([
    Chart.Line [ for x in 0 .. 10 -> x, x ]
    Chart.Line [ for x in 0 .. 10 -> x, x*x ]
]) |> Chart.Show