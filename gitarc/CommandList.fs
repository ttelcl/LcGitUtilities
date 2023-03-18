module CommandList

open System
open System.IO

open ColorPrint
open CommonTools

type private Options = {
  RepoFolder: string option
}

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-C" :: folder :: rest ->
      rest |> parseMore {o with RepoFolder = Some(folder)}
    | "-h" :: _
    | "-help" :: _ ->
      Usage.usage "list"
      None
    | [] ->
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument: %s{x}"
  let oo = 
    args |> parseMore {
      RepoFolder = None
    }
  match oo with
  | None ->
    1
  | Some(o) ->
    let host = GitExecute.makeGitHost verbose true
    let cmd = 
      host.NewCommand()
        .WithFolder(o.RepoFolder |> Option.defaultValue null)
        .WithCommand("show-ref")
        .AddPost("--head")
    // let status = host.RunToConsole(cmd)
    let lines, status = host.RunToLines(cmd)
    // cp $"Got \fb%d{lines.Count}\f0 lines"
    for line in lines do
      let parts = line.Split([| ' ' |], 2)
      cp $"\fc%s{parts[0]} \fg%s{parts[1]}\f0"
    status

