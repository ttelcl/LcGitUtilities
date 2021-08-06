module AppDb

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

let dbHelp () =
  py "lcgitutil db <subcommand> [-C <folder>] ..."
  pn "  Manage the meta-repository database."
  p2 "  -C <folder>      Use the repository surrounding the given anchor folder"
  p2 "                   Some commands support a 'label::role' syntax instead of a folder"
  py "lcgitutil db register [-role <role>]"
  pn "  Register the repository or update its registration"
  p2 "  -role <role>     Register as the specified role (potentially changing the role)"
  p2 "                   The <role> 'none' removes the existing role"
  py "lcgitutil db update-db [-rebuild] [-table]"
  pn "  Update the combined.repo-infos.json file"
  p2 "  -rebuild         Instead of a soft update, rebuild it from scratch"
  p2 "  -table           Also export a CSV version of the DB"
  py "lcgitutil db init"
  pn "  Ensure the directory structure of the stage area is as expected"
  py "lcgitutil db setup-stage [-label <label>] [-v|-q]"
  pn "  Setup a stage repository for an existing work repository"
  py "lcgitutil db list [-match <label>::<role>]"
  pn "  List registered repositories"
  p2 "  -match <lbl>::<role>   Select repos matching the label and role parts."
  p2 "                         Omit <lbl> or <role> to match all"
  ()

type private DbListOptions = {
  Filter: string
}

type private BucketInfo = {
  Bucket: RepoBucket
  Repos: RepoInfoBase array
  Key: string
}

let private runDbList args =
  let o = {
    Filter = null
  }
  let rec parseMore (o:DbListOptions) args =
    match args with 
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
      rest |> parseMore o
    | "-match" :: filter :: rest ->
      if filter.Contains("::") |> not then
        failwith "Expecting '-match' argument to contain '::'"
      rest |> parseMore {o with Filter = filter}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized option '%s'" x
  let o = args |> parseMore o
  let cfg = loadConfig false
  cfg.StageFolder |> Stage.SetupStage
  let stage = new Stage(cfg)
  let infos = stage.LoadRepoDb(UpdateLevel.Update)
  let bucketinfo (bucket: RepoBucket) =
    let repos =
      if o.Filter |> String.IsNullOrEmpty then
        bucket.Repositories
      else
        bucket.Repositories
        |> Seq.filter (fun rib -> rib.MatchesLabelAndRole(o.Filter))
    let repos =
      repos
      |> Seq.sortBy (fun ri -> (ri.Role, ri.Location, ri.Label, ri.InstanceId))
      |> Seq.toArray
    if repos.Length>0 then
      Some({
        Bucket = bucket
        Repos = repos
        Key = repos.[0].Label.ToLowerInvariant()
      })
    else
      None
  let bucketInfos =
    infos.Buckets
    |> Seq.choose bucketinfo
    |> Seq.sortBy (fun bi -> bi.Key)
    |> Seq.toArray
  bucketInfos.Length |> string |> lp "Number of Repo Groups with matches"
  
  for bi in bucketInfos do
    printfn ""
    let repos = bi.Repos
    let rootId = repos.[0].PrimaryRoot
    color Color.DarkYellow
    rootId |> printf "%s"
    let names = repos |> Seq.map (fun repo -> repo.Label) |> Set.ofSeq
    for name in names do
      resetColor()
      printf ", "
      color Color.Cyan
      printf "%s" name
    resetColor()
    printfn "."
    for repo in repos do
      let clr =
        if Directory.Exists(repo.Location) then
          match repo.Role with
          | "stage" -> Color.Green
          | "hot" -> Color.Yellow
          | "backup" -> Color.Gray
          | "transfer" -> Color.Blue
          | _ -> Color.Magenta
        else
          Color.DarkGray
      clr |> color
      printf "  %-10s" repo.Role
      color Color.White
      printf " %-24s" repo.Label
      color Color.DarkGray
      printf " %12s" repo.InstanceId
      clr |> color
      printf " %s" repo.Location
      resetColor()
      printfn "."
    ()
  ()

let private runDbInit args =
  let o = ()
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
    | [] ->
      o
    | x :: _ ->
      failwithf "Unexpected argument '%s'" x
  let o = args |> parseMore o
  let cfg = loadConfig false
  cfg.StageFolder |> Stage.SetupStage
  let stage = new Stage(cfg)
  stage.StageFolder |> lp "Stage Folder"
  stage.StageVersion |> string |> lp "Format Version"
  printfn "Loading repo info (without update)"
  let infos = stage.LoadRepoDb(UpdateLevel.NoUpdate)
  let buckets = infos.Buckets |> Seq.toArray
  let repos = infos.Repositories |> Seq.toArray
  repos.Length |> string |> lp "Total Repos"
  buckets.Length |> string |> lp "Distinct Repos"
  ()

type private UpdateDboptions = {
  Mode: UpdateLevel
  EmitTable: bool
}

let private runDbUpdate args = 
  let o = {
    Mode = UpdateLevel.Update
    EmitTable = false
  }
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
      rest |> parseMore o
    | "-rebuild" :: rest ->
      rest |> parseMore {o with Mode=UpdateLevel.Rebuild}
    | "-noupdate" :: rest ->
      rest |> parseMore {o with Mode=UpdateLevel.NoUpdate}
    | "-table" :: rest ->
      rest |> parseMore {o with EmitTable = true}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument '%s'" x
  let o = args |> parseMore o
  let cfg = loadConfig false
  let stage = new Stage(cfg)
  let infos = stage.LoadRepoDb(o.Mode)
  let buckets = infos.Buckets |> Seq.toArray
  let repositories = infos.Repositories |> Seq.toArray
  printf "There are "
  color Color.Green
  repositories.Length |> printf "%d"
  resetColor()
  printf " repository instances covering "
  color Color.Green
  buckets.Length |> printf "%d"
  resetColor()
  printfn " distinct repositories."
  if o.EmitTable then
    let onm = Path.Combine(stage.RepoInfoFolder, "combined.repo-infos.csv")
    do
      use csv = onm |> startFile
      fprintf csv "label"
      fprintf csv ",root"
      fprintf csv ",repo-id"
      fprintf csv ",bare"
      fprintf csv ",role"
      fprintf csv ",founded"
      fprintf csv ",root-full"
      fprintf csv ",location"
      fprintfn csv ""
      for repo in repositories do
        repo.Label |> fprintf csv "%s"
        repo.ShortRoot |> fprintf csv ",%s"
        repo.InstanceId |> fprintf csv ",%s"
        repo.Bare |> string |> fprintf csv ",%s"
        repo.Role |> fprintf csv ",%s"
        repo.Founded |> fprintf csv ",%s"
        repo.PrimaryRoot |> fprintf csv ",%s"
        repo.Location |> fprintf csv ",%s"
        fprintfn csv ""
      ()
    onm |> finishFile
    ()
  ()

type private RegisterOptions = {
  AnchorFolder: string
  Role: string
  Label: string
}

let private dbRegister (o:RegisterOptions) (repo:GitRepository) (cmdHost:GitCommandHost) (stage:Stage) =
  let o =
    let isInStage = repo.IsInStage();
    if isInStage then
      if repo.Bare |> not then
        failwith "This repo is in the stage area but is not 'bare'. Registration refused."
      if Stage.IsStageFolder(Path.GetDirectoryName(repo.GitFolder)) |> not then
        failwith "This repo is a repo nested in the stage area, not at the top. Registration refused"
      if o.Role = null then
        {o with Role="stage"}
      elif o.Role = "stage" then
        o
      else
        failwithf "This repo is in the stage area so the role should be 'stage', not '%s'" o.Role
    else
      if o.Role = "stage" then
        failwith "The 'stage' role is reserved for repositories in the stage area"
      o
  let ri = repo.LoadInfo(false)
  let ri =
    if ri = null then
      color Color.DarkYellow
      printf "Repository info file does not yet exist. Creating it."
      resetColor()
      printfn "."
      repo.InitializeRepoInfo(cmdHost)
      repo.LoadInfo(true)
    else
      if (o.Label |> String.IsNullOrEmpty |> not) && (ri.Label <> o.Label) then
        color Color.DarkYellow
        printf "Changing label from '%s' to '%s'." ri.Label o.Label
        resetColor()
        printfn "."
        ri.HostBlob.Root.Strings.["label"] <- o.Label
        ri.HostBlob.Save()
        repo.LoadInfo(true)
      else
        ri
  let modified = stage.Register(repo, o.Role)
  let ri =
    if modified then
      color Color.Green
      printf "Repository successfully registered (or updated)"
      resetColor()
      printfn "."
      repo.LoadInfo(true)
    else
      color Color.Cyan
      printf "Repository registration was already up to date"
      resetColor()
      printfn "."
      ri
  ri.Label |> lp "Label"
  ri.Role |> lp "Role"
  ri.PrimaryRoot |> lp "Root Commit ID"
  ri.InstanceId |> lp "Instance ID"
  ri.Location |> lp "Location"
  ri.Founded |> lp "Founded"
  ()

let private runDbRegister args =
  let o = {
    AnchorFolder = Environment.CurrentDirectory
    Role = null
    Label = null
  }
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
      rest |> parseMore o
    | "-role" :: "none" :: rest ->
      rest |> parseMore {o with Role = ""}
    | "-role" :: role :: rest ->
      rest |> parseMore {o with Role = role}
    | "-label" :: label :: rest ->
      rest |> parseMore {o with Label = label}
    | "-C" :: folder :: rest ->
      // The next may be relevant for updating an existing registration
      let folder = folder |> RepoDb.resolveRepoWitness
      rest |> parseMore {o with AnchorFolder = folder}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument %s" x
  let o = args |> parseMore o
  let repo = GitRepository.Locate(o.AnchorFolder)
  if o.Label |> String.IsNullOrEmpty |> not then
    repo.Label <- o.Label
  let cmdHost = makeCommandHost true
  let stage = new Stage(cmdHost.Configuration)
  dbRegister o repo cmdHost stage

type SetupStageOptions = {
  AnchorFolder: string
  Label: string
}

let private runDbSetupStage args =
  let o = {
    AnchorFolder = Environment.CurrentDirectory
    Label = null
  }
  bequiet <- false
  let rec parseMore (o:SetupStageOptions) args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-q" :: rest ->
      bequiet <- true
      rest |> parseMore o
    | "-C" :: anchor :: rest ->
      let anchor = anchor |> RepoDb.resolveRepoWitness
      rest |> parseMore {o with AnchorFolder = anchor}
    | "-label" :: label :: rest ->
      rest |> parseMore {o with Label = label}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized argument '%s'" x
  let o = args |> parseMore o
  let repo = GitRepository.Locate(o.AnchorFolder)
  if repo.Bare then
    failwithf "Expecting a non-bare repository as source, but got the bare repository: %s" repo.GitFolder
  let srcInfo = repo.LoadInfo(true)
  let cmdHost = makeCommandHost true
  let stage = new Stage(cmdHost.Configuration)
  let label =
    if o.Label |> String.IsNullOrEmpty then
      repo.Label
    else
      o.Label
  let repos = stage.LoadRepoDb(UpdateLevel.Update)
  let stageFolder = Path.Combine(stage.StageFolder, label+".git")
  stageFolder |> lp "Target Folder"
  if Directory.Exists(stageFolder) then
    failwithf "Stage repo folder already exists: %s" stageFolder
  let registration = repos.[srcInfo]
  if registration = null then
    color Color.DarkYellow
    printf "Source repo was not registered yet, registering it now"
    resetColor()
    printfn "."
    let oReg = {
      AnchorFolder = repo.GitFolder
      Role = null
      Label = label  // may trigger an edit!
    }
    dbRegister oReg repo cmdHost stage
  let result = stage.CreateStageRepo(cmdHost, repo, label)
  let oReg2 = {
    AnchorFolder = result.GitFolder
    Role = "stage"
    Label = null
  }
  dbRegister oReg2 result cmdHost stage
  ()

let runDb args =
  bequiet <- true
  match args with
  | "register" :: rest ->
    rest |> runDbRegister
  | "update-db" :: rest ->
    rest |> runDbUpdate
  | "init" :: rest ->
    rest |> runDbInit
  | "setup-stage" :: rest ->
    rest |> runDbSetupStage
  | "list" :: rest ->
    rest |> runDbList
  | x :: _ ->
    color Color.DarkYellow
    printf "Unrecognized command "
    color Color.Red
    printf "%s" x
    resetColor()
    printfn "."
    dbHelp()
  | [] ->
    dbHelp()
  ()

