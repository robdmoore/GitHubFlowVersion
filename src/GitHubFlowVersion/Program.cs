﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GitHubFlowVersion.BuildServers;
using GitHubFlowVersion.OutputStrategies;
using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public class Program
    {
        private const string MsBuild = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe";

        public static int Main(string[] args)
        {
            var arguments = Args.Configuration.Configure<GitHubFlowArguments>().CreateAndBind(args);

            var workingDirectory = arguments.WorkingDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                Run(arguments, workingDirectory);
                RunExecCommandIfNeeded(arguments, workingDirectory);
                RunMsBuildIfNeeded(arguments, workingDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            return 0;
        }

        private static void RunMsBuildIfNeeded(GitHubFlowArguments arguments, string workingDirectory)
        {
            if (!string.IsNullOrEmpty(arguments.ProjectFile))
            {
                var targetsArg = arguments.Targets == null ? null : " /target:" + arguments.Targets;
                Console.WriteLine("Launching {0} {1}{2}", MsBuild, arguments.ProjectFile, targetsArg);
                var results = ProcessHelper.Run(
                    Console.WriteLine, Console.Error.WriteLine,
                    null, MsBuild, arguments.ProjectFile + targetsArg, workingDirectory);
                if (results != 0)
                    throw new Exception("MsBuild execution failed, non-zero return code");
            }
        }

        private static void RunExecCommandIfNeeded(GitHubFlowArguments arguments, string workingDirectory)
        {
            if (!string.IsNullOrEmpty(arguments.Exec))
            {
                Console.WriteLine("Launching {0} {1}", arguments.Exec, arguments.ExecArgs);
                var results = ProcessHelper.Run(
                    Console.WriteLine, Console.Error.WriteLine,
                    null, arguments.Exec, arguments.ExecArgs, workingDirectory);
                if (results != 0)
                    throw new Exception("MsBuild execution failed, non-zero return code");
            }
        }

        private static void Run(GitHubFlowArguments arguments, string workingDirectory)
        {
            var fallbackStrategy = new LocalBuild();
            var buildServers = new IBuildServer[] {new TeamCity()};
            var currentBuildServer = buildServers.FirstOrDefault(s => s.IsRunningInBuildAgent()) ?? fallbackStrategy;

            var gitDirectory = GitDirFinder.TreeWalkForGitDir(workingDirectory);
            if (string.IsNullOrEmpty(gitDirectory))
            {
                if (currentBuildServer.IsRunningInBuildAgent()) //fail the build if we're on a TC build agent
                {
                    // This exception might have to change when more build servers are added
                    throw new Exception("Failed to find .git directory on agent. " +
                                        "Please make sure agent checkout mode is enabled for you VCS roots - " +
                                        "http://confluence.jetbrains.com/display/TCD8/VCS+Checkout+Mode");
                }

                throw new Exception("Failed to find .git directory.");
            }

            Console.WriteLine("Git directory found at {0}", gitDirectory);
            var repositoryRoot = Directory.GetParent(gitDirectory).FullName;

            var gitHelper = new GitHelper();
            var gitRepo = new Repository(gitDirectory);
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(gitRepo, gitHelper);
            var nextSemverCalculator = new NextSemverCalculator(new NextVersionTxtFileFinder(repositoryRoot),
                lastTaggedReleaseFinder);
            var buildNumberCalculator = new BuildNumberCalculator(nextSemverCalculator, lastTaggedReleaseFinder, gitHelper,
                gitRepo, currentBuildServer);

            var nextBuildNumber = buildNumberCalculator.GetBuildNumber();
            WriteResults(arguments, nextBuildNumber, currentBuildServer);
        }

        private static void WriteResults(GitHubFlowArguments arguments, SemanticVersion nextBuildNumber, IBuildServer currentBuildServer)
        {
            var variableProvider = new VariableProvider();
            var variables = variableProvider.GetVariables(nextBuildNumber);
            var outputStrategies = new IOutputStrategy[]
            {
                new BuildServerOutputStrategy(currentBuildServer),
                new JsonFileOutputStrategy(),
                new EnvironmentalVariablesOutputStrategy()
            };
            foreach (var outputStrategy in outputStrategies)
            {
                outputStrategy.Write(arguments, variables, nextBuildNumber);
            }
        }
    }
}