module AppRecipe

open System
open System.IO

open LcGitLib
open LcGitLib.GitRunning
open LcGitLib.FileUtilities
open LcGitLib.RawLog
open LcGitLib.RepoTools

open CommonTools
open ArgumentLogger
open PrintUtils
open Usage

type private RecipeArgs = {
  Recipe: string
  StartFolder: string
}

let recipeUsage () =
  py "lcgitutil recipe <recipe> [-C <folder>] [-v]"
  pn "   Run a predefined recipe. Current recipes:"
  p2 "-C <folder>       Use the repository surrounding the given folder"
  p2 "-C <label>::<role>  Query the repository to use from the repo db"
  p2 "newest            Retrieve information on the newest commit"
  p2 "roots             Retrieve information on the root commit(s)"
  p2 "label             Print repo information"
  p2 "repo-init         Initialize the repo-info file for this repository"
  p2 "tips              Load the repo graph and print the tip commit(s)"
  p2 "graph             Load the repo graph and print some observations"
  p2 "graphtest         (debugging new graph code)"
  p2 "help              Prints this message"

let runRecipe recipe args =
  bequiet <- true
  let o = {
    Recipe = recipe
    StartFolder = null
  }
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      bequiet <- false
      rest |> parseMore o
    //| "-q" :: rest ->
    //  bequiet <- true
    //  rest |> parseMore o
    | "-C" :: folder :: rest ->
      let folder = folder |> RepoDb.resolveRepoWitness
      rest |> parseMore {o with StartFolder = folder}
    | [] ->
      o
    | x :: _ ->
      failwithf "Unrecognized option '%s'" x
  let o = args |> parseMore o
  let printEntry includeMessage (entry:CommitEntry) =
    eprintf "         Commit: "
    color Color.Yellow
    entry.CommitId |> eprintf "%s"
    resetColor()
    eprintfn "."
    eprintf "    Commit Time: "
    color Color.Cyan
    entry.CommitTime().ToString("yyyy-MM-dd HH:mm:ss zzz") |> eprintf "%s"
    resetColor()
    eprintfn "."
    eprintf "         Author: "
    color Color.Green
    entry.Author.User |> eprintf "%s"
    resetColor()
    eprintfn "."
    if includeMessage then
      eprintf "         Header: "
      color Color.White
      entry.MessageLines.[0] |> eprintf "%s"
      resetColor()
      eprintfn "."
  let startFolder =
    if o.StartFolder |> String.IsNullOrEmpty then
      Environment.CurrentDirectory
    else
      Path.GetFullPath(o.StartFolder)
  
  match o.Recipe with
  | "newest" ->
    let repo = GitRepository.Locate(startFolder) // fails if not valid
    let cmdHost = makeCommandHost true
    let newest = cmdHost.NewestEntry(startFolder)
    eprintfn "Newest entry is:"
    newest |> printEntry true

  | "roots" ->
    let repo = GitRepository.Locate(startFolder) // fails if not valid
    let cmdHost = makeCommandHost true
    let roots = cmdHost.RootEntries(startFolder)
    if roots.Count = 1 then
      eprintfn "The root entry is:"
    elif roots.Count = 0 then
      color Color.Red
      eprintf "There are no root entries"
      resetColor()
      eprintfn "."
    else
      color Color.DarkYellow
      eprintf "There are multiple root entries"
      color Color.Red
      eprintf "(%d)" roots.Count
      color Color.DarkYellow
      eprintf " Not all lcgitutil commands will work on this repository!"
      resetColor()
      eprintfn "."
    for entry in roots do
      entry |> printEntry true

  | "label" ->
    let repo = GitRepository.Locate(startFolder, false)
    if repo = null then
      color Color.Red
      printf "Not a git repository: "
      color Color.DarkYellow
      printf "%s" startFolder
      resetColor()
      printfn "."
    else
      repo.GitFolder |> lp "GIT folder"
      if repo.Bare then
        "True" |> lp "Bare"
        "(n.a.)" |> lp "Repo folder"
      else
        "False" |> lp "Bare"
        repo.RepoFolder |> lp "Repo folder"
      repo.LcGitFolder |> lp "LcGit folder"
      repo.Label |> lp "Label"
      let repoinfo = repo.LoadInfo(false)
      if repoinfo = null then
        color Color.DarkYellow
        printf "Repo info has not been initialized yet"
        resetColor()
        printfn "!"
      else
        repoinfo.Version |> string |> lpc "info.Version"
        repoinfo.Label |> lpc "info.Label"
        repoinfo.Bare |> string |> lpc "info.Bare"
        repoinfo.Role |> lpc "info.Role"
        repoinfo.PrimaryRoot |> lpc "info.RootId"
        repoinfo.ShortRoot |> lpc "info.ShortRoot"
        repoinfo.InstanceId |> lpc "info.Instance"
        repoinfo.ShortId |> lpc "info.ShortId"
        repoinfo.Location |> lpc "info.Location"
        repoinfo.Founded |> lpc "info.Founded"

  | "repo-init" ->
    let repo = GitRepository.Locate(startFolder, false)
    if repo = null then
      color Color.Red
      printf "Not a git repository: "
      color Color.DarkYellow
      printf "%s" startFolder
      resetColor()
      printfn "."
    else
      let repoinfo = repo.LoadInfo(false)
      let repoinfo =
        if repoinfo <> null then
          color Color.DarkYellow
          printf "Repo info has already been initialized"
          resetColor()
          printfn "!"
          repoinfo
        else
          let cmdHost = makeCommandHost true
          repo.InitializeRepoInfo(cmdHost)
          repo.LoadInfo(true)
      repoinfo.Version |> string |> lpc "info.Version"
      repoinfo.Label |> lpc "info.Label"
      repoinfo.Bare |> string |> lpc "info.Bare"
      repoinfo.Role |> lpc "info.Role"
      repoinfo.PrimaryRoot |> lpc "info.RootId"
      repoinfo.ShortRoot |> lpc "info.ShortRoot"
      repoinfo.InstanceId |> lpc "info.Instance"
      repoinfo.ShortId |> lpc "info.ShortId"
      repoinfo.Location |> lpc "info.Location"
      repoinfo.Other("founded", "") |> lpc "info.Founded"
      repoinfo.Founded |> lpc "info.Founded"
      
  
  | "tips" ->
    let repo = GitRepository.Locate(startFolder) // fails if not valid
    let cmdHost = makeCommandHost true
    let graph = cmdHost.LoadGraph(startFolder)
    graph.Nodes.Count |> printfn "Graph loaded, %d entries. Tip nodes (without children):"
    let childlessNodes =
      graph.Nodes.Values
      |> Seq.where (fun node -> node.Children.Count = 0)
      |> Seq.toArray
    for cn in childlessNodes do
      cn.Entry |> printEntry true
    ()

  | "graph" ->
    let repo = GitRepository.Locate(startFolder) // fails if not valid
    let cmdHost = makeCommandHost true
    let graph = cmdHost.LoadGraph(startFolder)
    graph.Nodes.Count |> printfn "Graph loaded, %d entries."
    let pro =
      graph.Nodes.Values
      |> Seq.maxBy (fun node -> node.Children.Count)
    let maxCount = pro.Children.Count
    let pros =
      graph.Nodes.Values
      |> Seq.where (fun node -> node.Children.Count = maxCount)
      |> Seq.toArray
    printf "The maximum child count is "
    color Color.Green
    maxCount |> printf "%d"
    resetColor()
    printf ", reached by these "
    color Color.Green
    pros.Length |> printf "%d"
    resetColor()
    printfn " commits:"
    for node in pros do
      node.Entry |> printEntry true
  
  | "graphtest" ->
    let repo = GitRepository.Locate(startFolder) // fails if not valid
    let cmdHost = makeCommandHost true
    printfn "Loading graph"
    let graph = repo.LoadSummaryGraph(cmdHost, true)
    graph.PrunedEdgeCount |> string |> lp "Pruned Edges"
    graph.Roots.Count |> string |> lp "Roots"
    let tipCount = graph.Tips.Count
    tipCount |> string |> lp "Tips"
    let cmap = graph.NewColorMap()
    for i in 0 .. graph.Roots.Count-1 do
      let root = graph.Roots.[i]
      let rootId = root.Id
      let rootId2 = root.Seed.CommitId
      let mask = 1 <<< i
      printf "Filling root %19d (%s) with mask %07X" rootId rootId2 mask
      resetColor()
      printfn "."
      let marked = cmap.MaskRecursive(mask, cmap.PickChildren, [|rootId|])
      let markedTips =
        cmap.WhereBitMask(mask)
        |> Seq.map (fun id -> graph.[id])
        |> Seq.where (fun node -> node.Children.Count=0)
        |> Seq.toArray
      printfn "Marked: %d (%d tips, %d unmarked)" marked markedTips.Length (tipCount - markedTips.Length)
      ()
    let counterMap = cmap.CountColors()
    printfn "Counters:"
    for kvp in counterMap |> Seq.sortBy (fun kv -> kv.Key) do
      printfn "%07X : %5d" kvp.Key kvp.Value
    ()
  
  | "help" ->
    recipeUsage()

  | x ->
    printf "Unknown recipe '"
    color Color.Red
    printf "%s" x
    resetColor()
    printfn "'"
    recipeUsage()
  ()
