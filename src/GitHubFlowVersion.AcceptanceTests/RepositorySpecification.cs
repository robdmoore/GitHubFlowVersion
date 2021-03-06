﻿using System;
using System.IO;
using GitHubFlowVersion.AcceptanceTests.Helpers;
using LibGit2Sharp;
using Xunit;
using TestStack.BDDfy;

namespace GitHubFlowVersion.AcceptanceTests
{
    public abstract class RepositorySpecification : IDisposable
    {
        protected readonly string RepositoryPath;
        protected readonly Repository Repository;

        protected RepositorySpecification()
        {
            RepositoryPath = PathHelper.GetTempPath();
            Repository.Init(RepositoryPath);
            Console.WriteLine("Created git repository at {0}", RepositoryPath);

            Repository = new Repository(RepositoryPath);
            Repository.Config.Set("user.name", "Test");
            Repository.Config.Set("user.email", "test@email.com");
        }

        [Fact]
        public virtual void RunSpecification()
        {
            // If we are actually running in teamcity, lets delete this environmental variable
            Environment.SetEnvironmentVariable("TEAMCITY_VERSION", null);
            Environment.SetEnvironmentVariable("GitBranchName", null);
            this.BDDfy();
        }

        public void Dispose()
        {
            Cleanup();
            Repository.Dispose();
            try
            {
                Directory.Delete(RepositoryPath, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath, e.Message);
            }
        }

        protected virtual void Cleanup(){}
    }
}
