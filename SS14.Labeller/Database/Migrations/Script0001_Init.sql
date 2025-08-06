CREATE TABLE Discussions(
    DiscussionId INTEGER PRIMARY KEY,
    RepoOwner TEXT NOT NULL,
    RepoName TEXT NOT NULL,
    IssueNumber INTEGER NOT NULL
);

CREATE INDEX Discussions_Owner ON Discussions(RepoOwner);
CREATE INDEX Discussions_Name ON Discussions(RepoName);
CREATE INDEX Discussions_Issue ON Discussions(IssueNumber);