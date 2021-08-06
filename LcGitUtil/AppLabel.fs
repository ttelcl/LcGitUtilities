module AppLabel

open System
open System.IO

open LcGitLib.RepoTools

open CommonTools

type RepoDescriptor = {
  Bare: bool
  Label: string
  GitFolder: string
  RepoFolder: string
}

let findRepo startFolder =
  let gitFolder = RepoUtilities.FindGitFolder(startFolder)
  if gitFolder = null then
    None
  else
    let name = Path.GetFileName(gitFolder)
    if name = ".git" then
      let repoFolder = Path.GetDirectoryName(gitFolder)
      let label = Path.GetFileName(repoFolder)
      Some({
        Bare = false
        Label = label
        GitFolder = gitFolder
        RepoFolder = repoFolder
      })
    else if name.EndsWith(".git") then
      let label = name.Substring(0, name.Length-4)
      Some({
        Bare = true
        Label = label
        GitFolder = gitFolder
        RepoFolder = gitFolder
      })
    else
      failwithf "Unexpected GIT folder name %s" gitFolder

type private LabelOptions = {
  StartFolder: string
}

let runLabel args =
  let o = {
    StartFolder = null
  }
  let rec parseMore o args = 
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-C" :: startFolder :: rest ->
      rest |> parseMore {o with StartFolder = startFolder}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument '%s'" x
  let o = args |> parseMore o
  let startFolder =
    if o.StartFolder |> String.IsNullOrEmpty then
      Environment.CurrentDirectory
    else
      o.StartFolder |> RepoUtilities.NormalizeDirectoryName
  if startFolder |> Directory.Exists |> not then
    failwithf "No such directory: %s" startFolder
  let repoInfo = startFolder |> findRepo
  match repoInfo with
  | None ->
    color Color.Red
    printf "Failed! "
    resetColor()
    printf "No repository found by searching from "
    color Color.DarkYellow
    printf "%s" startFolder
    resetColor()
    printfn "."
  | Some(rinf) ->
    printf "      Label: "
    color Color.Green
    printf "%s" rinf.Label
    resetColor()
    printfn ""
    printf "       Mode: "
    if rinf.Bare then
      color Color.Cyan
      printf "Bare"
    else
      color Color.Green
      printf "Normal"
    resetColor()
    printfn ""
    printf " GIT folder: "
    color Color.Cyan
    printf "%s" rinf.GitFolder
    resetColor()
    printfn ""
    printf "Repo folder: "
    if rinf.Bare then
      color Color.DarkGray
      printf "n.a."
    else
      color Color.Green
      printf "%s" rinf.RepoFolder
    resetColor()
    printfn ""
  ()

