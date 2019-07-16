# Pepperdash Essentials

## RELEASE PROCESS CONTROLLED BY JENKINS CI PROCESS

#### How to merge

- Make changes
- Build
- Test on your system
- Commit and push changes
- Merge latest development branch into your branch
- Commit and push your branch
- Log a Pull Request in Bitbucket


#### How to resolve a Pull Request

- Review changes in PR branch
- Approve or disapprove
- If approved, merge into development in Bitbucket

#### How to publish a release version

- Tag development commit to be published with version number to trigger Jenkins CI build process.
- CI process will publish a new version to the release branch of the essentials-releases repo and tag it with the matching version number
