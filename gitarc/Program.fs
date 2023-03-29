// (c) 2023  ttelcl / ttelcl

open System

open CommonTools
open ExceptionTool
open Usage
open ColorPrint

let rec run arglist =
  // For subcommand based apps, split based on subcommand here
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _ ->
    usage "all"
    0
  | [] ->
    usage null
    0  // program return status code to the operating system; 0 == "OK"
  | "list" :: rest ->
    rest |> CommandList.run
  | "tree" :: rest ->
    rest |> CommandTree.run
  | "snap" :: rest ->
    rest |> CommandTree.run
  | x :: _ ->
    cp $"\frUnknown Command\f0: \fo%s{x}"
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



