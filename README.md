# autoversionbumper

This web-api is used to recieve a webhook push event from github. If the event is a push of commits to the `refs/head/develop` branch AND it contains commits merging a feature branch into it or a pull request being merged it will try to bump the minor version of the repository and push it back.

# Setup

- Create a Personal Api Token within Github for the user as which you will commit the automated bumps => https://github.com/settings/applications
- Check out the code, build it. Now update the `Appsettings.config` and deploy it somewhere (Azurewebsites is easy as pie)
- Done!

# Setup of new repo to watch

- Make sure that the repo you want to auto-bump has a develop branch and it contains a `version.txt` file containing only `{major}.{minor}`.
- Go to the WebHooks configuration within Github => https://github.com/crunchie84/autoversionbumper/settings/hooks and add a new webhook
    - Point the PayLoad URL to the deployed webapi /api/pushevent endpoint => https://example-deploy.azurewebsites.com/api/pushevent 
    - (Just the push event)
- Make sure that the user configured to commit the autobump has write access to the repo you just added the webhook to

# Usage

work on your code, create feature/my-epic-feature. Merge it into develop. push it to Github. see the awesomeness in your commit history!

# NOTES

Due to the fact that we use [libgit2sharp](https://github.com/libgit2/libgit2sharp) which does not play well with web publishing (yet) i have included the nativelibraries as 'copy to bin folder' in the solution. This is a workaround untill they have merged [PR#705](https://github.com/libgit2/libgit2sharp/pull/705) and rolled it out as new version.