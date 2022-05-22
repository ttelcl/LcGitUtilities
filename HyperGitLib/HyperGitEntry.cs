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
  /// Info about one row of a HyperGit config file
  /// </summary>
  public class HyperGitEntry
  {
    /// <summary>
    /// Create a new HyperGitEntry
    /// </summary>
    internal HyperGitEntry(
      HyperGitConfig owner,
      bool disable,
      string host,
      string bucket,
      string name,
      string origin)
    {
      Owner = owner;
      Disable = disable;
      Bucket0 = bucket;
      Name0 = name;
      Origin = origin;
      Host = host;
      Renamed = false;

      if(Origin.StartsWith("https://"))
      {
        if(Bucket0 == "")
        {
          throw new InvalidOperationException(
            $"HTTP origins require a bucket. {Origin}");
        }
        Bucket = Bucket0;
        var idx = Origin.LastIndexOf('/');
        var derivedName = Origin.Substring(idx+1);
        if(derivedName.EndsWith(".git"))
        {
          derivedName = derivedName.Substring(0, derivedName.Length - 4);
        }
        if(String.IsNullOrEmpty(Name0))
        {
          Name = derivedName;
        }
        else
        {
          Name = Name0;
        }
        Renamed = Name != derivedName;
      }
      else if(Origin.StartsWith("git@"))
      {
        var path = Origin.Substring(Origin.LastIndexOf(':')+1);
        if(path.EndsWith(".git"))
        {
          path = path.Substring(0, path.Length - 4);
        }
        var idx = path.LastIndexOf('/');
        string derivedBucket;
        string derivedName;
        if(idx == -1)
        {
          derivedBucket = "";
          derivedName = path;
        }
        else
        {
          derivedBucket = path.Substring(0, idx);
          derivedName = path.Substring(idx+1);
        }
        Name = String.IsNullOrEmpty(Name0) ? derivedName : Name0;
        Renamed = Name != derivedName;
        if(String.IsNullOrEmpty(Bucket0))
        {
          Bucket = derivedBucket;
        }
        else if(Bucket0 == "/")
        {
          Bucket = "";
        }
        else
        {
          Bucket = Bucket0;
        }
      }
      else
      {
        throw new InvalidOperationException(
          $"Unsupported origin style in {Origin}");
      }
      HostMatch = StringComparer.OrdinalIgnoreCase.Equals(Environment.MachineName, Host);
      if(String.IsNullOrEmpty(Bucket))
      {
        BucketPath = Owner.RootFolder;
      }
      else
      {
        BucketPath = Path.Combine(Owner.RootFolder, Bucket);
      }
      RepoPath = Path.Combine(BucketPath, Name + ".git");
    }

    /// <summary>
    /// Convert a line from a config file to an HyperGitEntry instance
    /// </summary>
    internal static HyperGitEntry FromLine(HyperGitConfig owner, string line)
    {
      var parts = line.Split(',');
      if(parts.Length != 5)
      {
        throw new InvalidOperationException(
          "Unrecognized config line format. Expecting 5 fields");
      }
      if(parts[0] != String.Empty && parts[0] != "!")
      {
        throw new InvalidOperationException(
          "Expecting the value of the first column to be blank or '!'");
      }
      return new HyperGitEntry(
        owner,
        parts[0] == "!",
        parts[1],
        parts[2],
        parts[3],
        parts[4]);
    }

    /// <summary>
    /// The config this entry is part of
    /// </summary>
    public HyperGitConfig Owner { get; }

    /// <summary>
    /// When true: ignore this row
    /// </summary>
    public bool Disable { get; }

    /// <summary>
    /// The host this row applies to
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// The original bucket name as specified in the config.
    /// This is an empty string to indicate the derived bucket should be used.
    /// This is "/" to indicate no bucket should be used
    /// </summary>
    public string Bucket0 { get; }

    /// <summary>
    /// The effective bucket folder path (relative to the root), or blank
    /// to indicate there is no bucket path. Derived from Bucket0 and Origin
    /// </summary>
    public string Bucket { get; }

    /// <summary>
    /// The effective name of the repo (minus a ".git" extension).
    /// Derived from Name0 and Origin
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The original name in the config file, or blank to derive the name
    /// from Origin
    /// </summary>
    public string Name0 { get; }

    /// <summary>
    /// The origin URL. Currently supports "https://" and "git@" urls
    /// </summary>
    public string Origin { get; }

    /// <summary>
    /// True if the host matches
    /// </summary>
    public bool HostMatch { get; }

    /// <summary>
    /// True if this record is enabled and matching the host name
    /// </summary>
    public bool IsEnabled => !Disable && HostMatch;

    /// <summary>
    /// The path to the bucket folder
    /// </summary>
    public string BucketPath { get; }
    
    /// <summary>
    /// The path to the target repository
    /// </summary>
    public string RepoPath { get; }

    /// <summary>
    /// True if repository name differs from the derived name
    /// </summary>
    public bool Renamed { get; }
  }
}
