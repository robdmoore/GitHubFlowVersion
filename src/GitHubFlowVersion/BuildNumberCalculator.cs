﻿using GitHubFlowVersion.BuildServers;
using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public class BuildNumberCalculator
    {
        private readonly INextSemverCalculator _nextSemverCalculator;
        private readonly ILastTaggedReleaseFinder _lastTaggedReleaseFinder;
        private readonly IGitHelper _gitHelper;
        private readonly IRepository _gitRepo;
        private readonly IBuildServer _buildServer;

        public BuildNumberCalculator(
            INextSemverCalculator nextSemverCalculator,
            ILastTaggedReleaseFinder lastTaggedReleaseFinder,
            IGitHelper gitHelper, IRepository gitRepo, 
            IBuildServer buildServer)
        {
            _nextSemverCalculator = nextSemverCalculator;
            _lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            _gitHelper = gitHelper;
            _gitRepo = gitRepo;
            _buildServer = buildServer;
        }

        public SemanticVersion GetBuildNumber()
        {
            var currentBranch  = _gitRepo.Head;
            var commitsSinceLastRelease = _gitHelper.NumberOfCommitsOnBranchSinceCommit(currentBranch, _lastTaggedReleaseFinder.GetVersion().Commit);
            SemanticVersion semanticVersion = _nextSemverCalculator.NextVersion();
            if (_buildServer.IsBuildingAPullRequest())
                semanticVersion = semanticVersion.WithSuffix("PullRequest" + _buildServer.CurrentPullRequestNo());
            return semanticVersion.WithBuildMetaData(commitsSinceLastRelease);
        }
    }
}