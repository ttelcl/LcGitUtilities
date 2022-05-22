/*
 * (c) 2022  VTT / TTELCL
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperGitLib
{
  /// <summary>
  /// The content and location of a HyperGit config file
  /// </summary>
  public class HyperGitConfig
  {
    private List<HyperGitEntry> _entries;

    /// <summary>
    /// Create a new HyperGitConfig
    /// </summary>
    public HyperGitConfig(
      string filename = "repos.csv")
    {
      FileName = Path.GetFullPath(filename);
      if(!File.Exists(FileName))
      {
        throw new FileNotFoundException(
          "Missing config file", FileName);
      }
      RootFolder = Path.GetDirectoryName(FileName);
      _entries = new List<HyperGitEntry>();
      Entries = _entries.AsReadOnly();
      var first = true;
      foreach(var line in File.ReadLines(FileName))
      {
        if(first)
        {
          first = false;
          if(line != "disable,host,bucket,name,origin")
          {
            throw new InvalidOperationException(
              "Incompatible configuration file");
          }
        }
        else
        {
          var entry = HyperGitEntry.FromLine(this, line);
          _entries.Add(entry);
        }
      }
    }

    /// <summary>
    /// The full path to the config file
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The directory the config file is in
    /// </summary>
    public string RootFolder { get; }

    /// <summary>
    /// The entries loaded from this config
    /// </summary>
    public IReadOnlyList<HyperGitEntry> Entries { get; }

  }
}
