// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\fogitarc \fysnap\f0 [\fg-d\f0|\fg-p\f0]\f0"
  cp "   Snapshot all current branches, creating or updating their archive nodes."
  cp "   Branches prefixed with 'archive/' are treated as if that prefix"
  cp "   was missing."
  cp "   \fg-d\f0     After archiving, delete branches prefixed with 'archive/'"
  cp "   \fg-p\f0     After archiving, leave such branches as-is"
  cp ""
  cp "\fogitarc \fylist\f0"
  cp "   List archive nodes in the repo"
  cp ""
  cp "\fbCommon Options"
  cp "\fg-v               \fx\f0Verbose mode"
  cp "\fg-C \fc<folder>\f0      Act on the GIT repo for \fc<folder>\f0 (instead of current)"



