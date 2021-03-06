﻿using Xunit;

namespace GitHubFlowVersion.Tests
{
    public class VariableProviderTests
    {
        private readonly VariableProvider _sut;

        public VariableProviderTests()
        {
            _sut = new VariableProvider();
        }

        [Fact]
        public void VerifyReleaseVariables()
        {
            var version = new SemanticVersion(1, 2, 3, buildMetaData: 4);
            var variables = _sut.GetVariables(version);

            Assert.Equal("1.2.3+004", variables["GitHubFlowVersion_FullSemVer"]);
            Assert.Equal("1.2.3", variables["GitHubFlowVersion_SemVer"]);
            Assert.Equal("1.2.3.4", variables["GitHubFlowVersion_FourPartVersion"]);
            Assert.Equal("1", variables["GitHubFlowVersion_Major"]);
            Assert.Equal("2", variables["GitHubFlowVersion_Minor"]);
            Assert.Equal("3", variables["GitHubFlowVersion_Patch"]);
            Assert.Equal("4", variables["GitHubFlowVersion_NumCommitsSinceRelease"]);
        }

        [Fact]
        public void VerifypreReleaseVariables()
        {
            var version = new SemanticVersion(1, 2, 3, suffix: "beta", buildMetaData: 4);
            var variables = _sut.GetVariables(version);

            Assert.Equal("1.2.3-beta+004", variables["GitHubFlowVersion_FullSemVer"]);
            Assert.Equal("1.2.3-beta", variables["GitHubFlowVersion_SemVer"]);
            Assert.Equal("1.2.3.4", variables["GitHubFlowVersion_FourPartVersion"]);
            Assert.Equal("1", variables["GitHubFlowVersion_Major"]);
            Assert.Equal("2", variables["GitHubFlowVersion_Minor"]);
            Assert.Equal("3", variables["GitHubFlowVersion_Patch"]);
            Assert.Equal("4", variables["GitHubFlowVersion_NumCommitsSinceRelease"]);
            Assert.Equal("beta", variables["GitHubFlowVersion_Tag"]);
        }
    }
}