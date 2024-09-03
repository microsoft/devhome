The folders under "resources" are Git repositories that have had ".git" renamed to "dot-git" and ".gitmodules" renamed to "dot-gitmodules".

This makes Git treat them as normal files so they can be checked in. SandboxHelper will rename them back to ".git" and ".gitmodules" when it needs to "clone" the repos for testing.
