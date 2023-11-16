# Repository Tests
If your code affects repository cloning, or the repo tool, please manually verify these scenarios.  This is required for all PRs that affect repo cloning directly or indirectly.  Indirectly can be changes in DeveloperId code in an extension or an SDK change.

## Scenarios
Please make sure to verify all these scenarios.
### Cloning

#### Via account
1. Multiple repos can be selected and added in bulk and the selected repos show up in the repo config screen.

#### Via URL
1. If provided a URL to an existing repo, and the user has access to the repo, it shows up on the repo config page.

#### Repo config page
1. All repos shown on this page have the correct information, including icon, repo name, provider name, and clone location.

#### Loading page
1. All repos selected in the repo tool are cloned to their respective locations.
2. Any failures are logged and shown to the user. 

## Providers
### One enabled provider
1. If the user is logged into the provider, clicking on the "Account" tab brings the user to the list of repos they have access to.

### Multiple enabled providers
1. The account page displays a list of all enabled providers the user can choose.  The names displayed is the long form.  Example "Dev Home GitHub Extension (dev)" and not "Github"
