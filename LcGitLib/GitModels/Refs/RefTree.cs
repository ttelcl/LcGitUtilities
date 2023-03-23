/*
 * (c) 2023  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcGitLib.GitModels.Refs
{
  /// <summary>
  /// A tree of GIT object refs. This is itself a RefTreeNode named "refs",
  /// with no parent
  /// </summary>
  public class RefTree: RefTreeNode
  {
    /// <summary>
    /// Create a new RefTree
    /// </summary>
    public RefTree()
      : base("refs", null)
    {
    }

    /// <summary>
    /// Get or set the GitId value of a ref
    /// </summary>
    /// <param name="path">
    /// The path identifying the ref
    /// </param>
    /// <returns>
    /// The GitId of the node that was found (possibly null), or null if the
    /// ref node wasn't found.
    /// </returns>
    public GitId? this[string path] {
      get => GetNode(path, false)?.Value;
      set {
        GetNode(path, true).Value = value;
      }
    }

    /// <summary>
    /// Get or create a node in the tree, given its full path
    /// </summary>
    /// <param name="path">
    /// The full path of the node, which must start with "refs/"
    /// </param>
    /// <param name="create">
    /// If true: the node is created and inserted if it does not yet exist.
    /// If false (default): the node is not create if it does not exist; null is returned instead
    /// </param>
    /// <returns>
    /// The node that was found or created, or null if not found and <paramref name="create"/>
    /// was false.
    /// </returns>
    public RefTreeNode GetNode(string path, bool create = false)
    {
      if(String.IsNullOrEmpty(path))
      {
        throw new ArgumentOutOfRangeException(
          nameof(path), "Path cannot be empty");
      }
      var parts = path.Split(new char[] { '/' });
      if(parts[0] != "refs")
      {
        // Technically speaking that error message is not correct, but 
        // "must start with 'refs/' or be exactly equal to 'refs'"
        // sounds overly convoluted.
        throw new ArgumentOutOfRangeException(
          nameof(path), "Path must start with 'refs/'");
      }
      RefTreeNode node = this;
      foreach(var part in parts.Skip(1))
      {
        var child = node.GetChild(part, create);
        if(child == null)
        {
          return null;
        }
        node = child;
      }
      return node;
    }

    /// <summary>
    /// Enumerate all refs in the tree that have been assigned a value
    /// </summary>
    public IEnumerable<RefTreeNode> EnumerateAllValuedNodes()
    {
      return
        EnumerateNodes()
        .Where(n => n.Value != null);
    }

  }
}
