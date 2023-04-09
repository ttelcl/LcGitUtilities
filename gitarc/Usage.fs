// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  let doDetail tag =
    detail = "all" || detail = tag
  let doHeader tag =
    doDetail tag || detail = null
  if doHeader "snap" then
    cp "\fogitarc \fysnap\f0 [\fg-i\f0] [\fg-d\f0|\fg-p\f0] [\fg-tag \fc<debug-tag>\f0]"
    cp "   Snapshot all current branches, creating or updating their archive nodes."
  if doDetail "snap" then
    cp "   Branches prefixed with 'archive/' are treated as if that prefix"
    cp "   was missing."
    cp "   \fg-d\f0     After archiving, delete branches prefixed with 'archive/'"
    cp "   \fg-p\f0     After archiving, leave such branches as-is"
    cp "   \fg-q\f0     Quieter: do not print info on unchanged nodes (ignored with \fg-v\f0)"
    cp ""
  if doHeader "list" then
    cp "\fogitarc \fylist\f0"
    cp "   List archive nodes in the repo"
  if doDetail "list" then
    cp "   \fg-R \fc<remote>\f0    The remote to inspect (the name of a configured remote"
    cp "   \fx\fx\fx               or a remote repo). When omitted, the local repo is used."
    cp "   \fg-r \f0\fx            Shortcut for \fg-R \fcorigin\f0."
    cp ""
  if doHeader "tree" then
    cp "\fogitarc \fytree\f0 [\fg-r\f0|\fg-R \fc<remote>\f0]"
    cp "   List archive nodes in the local or remote repo in tree form"
  if doDetail "tree" then
    cp "   \fg-R \fc<remote>\f0    The remote to inspect (the name of a configured remote"
    cp "   \fx\fx\fx               or a remote repo). When omitted, the local repo is used."
    cp "   \fg-r \f0\fx            Shortcut for \fg-R \fcorigin\f0."
    cp ""
  cp "\fbCommon Options"
  cp "\fg-v               \fx\f0Verbose mode"
  cp "\fg-C \fc<folder>\f0      Act on the GIT repo for \fc<folder>\f0 (instead of current)"
  cp "\fg-h               \fx\f0Print more detailed help on one command"



