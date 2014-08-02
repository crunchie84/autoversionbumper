using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;
using BumpMinorVersionOnMerge.Models;
using LibGit2Sharp;

namespace BumpMinorVersionOnMerge.Controllers
{
  /// <summary>
  /// The api controller which will act upon a new push event from GitHub
  /// </summary>
  public class PushEventController : ApiController
  {
    private const string CheckoutBaseDir = @"~/App_Data";
    private const string VersionFileName = @"version.txt";

    private const string FeatureMergeCommitMessage = @"Merge branch 'feature/";
    private const string PullRequestMergecommitMessage = @"Merge pull request #";
    private const string BranchDevelopRefString = @"refs/heads/develop";

    /// <summary>
    /// the Username contains a github-api token generated on my crunchie84 account. 
    /// </summary>
    private readonly Credentials gitCredentials;

    /// <summary>
    /// constructor
    /// </summary>
    public PushEventController()
    {
      // the personalAccessToken can be passed to GitHub via basic-auth username because there is no real other field for it. 
      gitCredentials = new UsernamePasswordCredentials { Username = ConfigurationManager.AppSettings["Github.PersonalAccessToken"], Password = "" };
    }

    /// <summary>
    /// eventhandler for pushEvents of github
    /// </summary>
    /// <param name="pushEvent"></param>
    /// <returns></returns>
    public HttpResponseMessage Post([Required]PushEvent pushEvent)
    {
      if (pushEvent == null)
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No pushState given");

      if (!ModelState.IsValid)
        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Not all required fields are available in the pushEvent");

      // if the pushEvent references the 'develop' branch and contains commit messages to which we are listening, bump the version
      if (pushEvent.Ref.Equals(BranchDevelopRefString, StringComparison.InvariantCultureIgnoreCase)
        && pushEvent.Commits.Any(c => c.Message.Contains(FeatureMergeCommitMessage) || c.Message.Contains(PullRequestMergecommitMessage)))
      {
        try
        {
          var repositoryCheckoutDir = cloneIfRequired(pushEvent, HostingEnvironment.MapPath(CheckoutBaseDir));

          //we have found merges of feature branches, bump the minor version
          using (var repo = new Repository(repositoryCheckoutDir))
          {
            //reset any uncommited changes just to be sure
            repo.Reset(ResetMode.Hard);

            //update to newest code from origin
            repo.Fetch("origin", new FetchOptions { Credentials = gitCredentials });

            // switch to develop branch (to be sure)
            if (repo.Branches.All(b => b.Name != "develop"))
              return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                string.Format("The repository '{0}' does not contain a 'develop' branch, we expect it to be available",
                  pushEvent.Repository.Url));
            repo.Checkout(repo.Branches.First(b => b.Name == "develop"));

            // merge origin develop into our local checkout
            var mergeResult = repo.Merge(repo.Branches["origin/develop"].Tip, createSignature());
            if (mergeResult.Status == MergeStatus.Conflicts)
              return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                "Could not update local develop branch with changes from origin due to mergeConflict");

            var versionFilePath = Path.Combine(repositoryCheckoutDir, VersionFileName);
            if (!File.Exists(versionFilePath))
              return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                "No version.txt file found in root dir of repository, could not bump version");

            var version = bumpVersionAndWriteToFile(versionFilePath);
            repo.Index.Stage(Path.GetFullPath(versionFilePath));
            repo.Commit("[AUTOMATED] bump minor version \r\n Due to merged feature branch or pull request we have now bumped the version to " + version, createSignature(), createSignature());

            repo.Network.Push(repo.Head, new PushOptions { Credentials = gitCredentials });
            return Request.CreateResponse(HttpStatusCode.OK,
              string.Format("Bumped minor version of repo {0} to {1}", pushEvent.Repository.Url, version));
          }
        }
        catch (Exception e)
        {
          return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
        }
      }

      return Request.CreateResponse(HttpStatusCode.NoContent);
    }

    private static string bumpVersionAndWriteToFile(string versionFilePath)
    {
      //open the version file
      var version = File.ReadAllText(versionFilePath);
      var major = version.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).First();
      var minor = int.Parse(version.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last());

      version = string.Format("{0}.{1}", major, minor + 1);

      File.WriteAllText(versionFilePath, version);
      return version;
    }

    /// <summary>
    /// validate if the checkout already exists or clone it otherwise
    /// </summary>
    /// <param name="pushEvent"></param>
    /// <param name="baseDir"></param>
    /// <returns></returns>
    private string cloneIfRequired(PushEvent pushEvent, string baseDir)
    {
      var repositoryCheckoutDir = Path.Combine(baseDir, pushEvent.Repository.Name.Replace(@"\", "").Replace(@"/", "").Replace("~", "").Replace("..", "")); //small cleanups to prevent escaping of current dir
      if (!Directory.Exists(repositoryCheckoutDir))
      {
        Repository.Clone(pushEvent.Repository.Url, repositoryCheckoutDir, new CloneOptions { Credentials = gitCredentials });
      }
      return repositoryCheckoutDir;
    }

    /// <summary>
    /// create a Git Signature object
    /// </summary>
    /// <returns></returns>
    private static Signature createSignature()
    {
      return new Signature(ConfigurationManager.AppSettings["Git.UserName"], ConfigurationManager.AppSettings["Git.EmailAddress"], DateTimeOffset.UtcNow);
    }
  }
}
