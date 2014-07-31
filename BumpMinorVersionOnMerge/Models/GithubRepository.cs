using System.ComponentModel.DataAnnotations;

namespace BumpMinorVersionOnMerge.Models
{
  /// <summary>
  /// skeleton model of a repository in a GitHub push event
  /// </summary>
  public class GithubRepository
  {
    /// <summary>
    /// contains 'my-repository-name' when the url = https://github.com/crunchie84/my-repository-name
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// full https checkout url of the repository (https://github.com/crunchie84/my-repository-name)
    /// </summary>
    [Required]
    public string Url { get; set; }
  }
}