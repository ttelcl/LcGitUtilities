module AppGraph

open System
open System.IO

open LcGitLib
open LcGitLib.GitRunning
open LcGitLib.FileUtilities
open LcGitLib.RawLog
open LcGitLib.RepoTools
open LcGitLib.MetaBase

open CommonTools
open ArgumentLogger
open PrintUtils
open Usage

let graphHelp () =
  py "lcgitutil graph ..."
  pn " Various commit graph based tools."
  py "lcgitutil graph to-dot -C <repo>"
  
  ()

let runGraph args =
  match args with
  | "to-dot" :: rest ->
    failwith "NYI"
  | [] ->
    graphHelp ()
  | "help" :: _ ->
    graphHelp ()
  | x :: _ ->
    color Color.DarkYellow
    printf "Unrecognized graph command '"
    color Color.Red
    printf "%s" x
    color Color.DarkYellow
    printf "'"
    resetColor()
    printfn "."
    graphHelp ()

