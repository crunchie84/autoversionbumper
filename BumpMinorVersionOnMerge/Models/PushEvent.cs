using System.ComponentModel.DataAnnotations;

namespace BumpMinorVersionOnMerge.Models
{
  /// <summary>
  /// PushEvent from github, only the parts which are relevant for us
  /// </summary>
  public class PushEvent
  {
    /// <summary>
    /// The branch on which the commits took place
    /// </summary>
    [Required]
    public string @Ref { get; set; }

    /// <summary>
    /// The commits which are available in this event
    /// </summary>
    [Required]
    public Commit[] Commits { get; set; }

    /// <summary>
    /// The repository of which this pushEvent is a part
    /// </summary>
    [Required]
    public GithubRepository Repository { get; set; }
  }
}