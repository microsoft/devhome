// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LibGit2Sharp;

internal sealed class CommitWrapper
{
    public string MessageShort { get; private set; }

    public string AuthorName { get; private set; }

    public string AuthorEmail { get; private set; }

    public DateTimeOffset AuthorWhen { get; private set; }

    public string Sha { get; private set; }

    public CommitWrapper(Commit commit)
    {
        MessageShort = commit.MessageShort;
        AuthorName = commit.Author.Name;
        AuthorEmail = commit.Author.Email;
        AuthorWhen = commit.Author.When;
        Sha = commit.Sha;
    }

    public CommitWrapper(string messageShort, string authorName, string authorEmail, DateTimeOffset authorWhen, string sha)
    {
        MessageShort = messageShort;
        AuthorName = authorName;
        AuthorEmail = authorEmail;
        AuthorWhen = authorWhen;
        Sha = sha;
    }
}
