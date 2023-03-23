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
  /// A node in a namespace tree of refs
  /// </summary>
  public class RefTreeNode
  {
    private readonly Dictionary<string, RefTreeNode> _children;

    /// <summary>
    /// Create a new RefTreeNode
    /// </summary>
    internal RefTreeNode(string shortName, RefTreeNode parent)
    {
      if(String.IsNullOrEmpty(shortName))
      {
        throw new ArgumentOutOfRangeException(nameof(shortName), "A ref path segment name cannot be empty");
      }
      if(shortName.Contains('/'))
      {
        throw new ArgumentOutOfRangeException(nameof(shortName), "A ref path segment name cannot contain '/'");
      }
      Parent = parent;
      ShortName = shortName;
      _children = new Dictionary<string, RefTreeNode>();
      if(parent == null)
      {
        FullName = shortName;
      }
      else
      {
        FullName = parent.FullName + "/" + shortName;
      }
    }

    /// <summary>
    /// The parent node (possibly null)
    /// </summary>
    public RefTreeNode Parent { get; init; }

    /// <summary>
    /// The short name of this node (path excluded)
    /// </summary>
    public string ShortName { get; init; }

    /// <summary>
    /// The full name of the node
    /// </summary>
    public string FullName { get; init; }

    /// <summary>
    /// Get or set the target object id, or null if there is none
    /// </summary>
    public GitId? Value { get; set; }

    /// <summary>
    /// Retrieve an existing direct child node or optionally create a new one
    /// </summary>
    /// <param name="name">
    /// The child node's name
    /// </param>
    /// <param name="create">
    /// Whether or not to create the node if it didn't exist yet.
    /// Default false.
    /// </param>
    /// <returns>
    /// The existing node, or the new node if it was created, or null
    /// if the node did not exist and <paramref name="create"/> was false.
    /// </returns>
    public RefTreeNode GetChild(string name, bool create = false)
    {
      if(_children.TryGetValue(name, out RefTreeNode child))
      {
        return child;
      }
      else
      {
        if(create)
        {
          var node = new RefTreeNode(name, this);
          _children.Add(name, node);
          return node;
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// The children of this node
    /// </summary>
    public IReadOnlyCollection<RefTreeNode> Children { get => _children.Values; }

    /// <summary>
    /// Enumerate this and all descencent nodes
    /// </summary>
    public IEnumerable<RefTreeNode> EnumerateNodes()
    {
      yield return this;
      foreach(var node in Children.SelectMany(n => n.EnumerateNodes()))
      {
        yield return node;
      }
    }
  }
}
