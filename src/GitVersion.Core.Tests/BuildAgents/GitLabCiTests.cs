using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.BuildAgents;

[TestFixture]
public class GitLabCiTests : TestBase
{
    private GitLabCi buildServer;
    private IServiceProvider sp;

    [SetUp]
    public void SetUp()
    {
        this.sp = ConfigureServices(services => services.AddSingleton<GitLabCi>());
        this.buildServer = this.sp.GetRequiredService<GitLabCi>();
    }

    [Test]
    public void GenerateSetVersionMessageReturnsVersionAsIsAlthoughThisIsNotUsedByJenkins()
    {
        var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
        this.buildServer.GenerateSetVersionMessage(vars).ShouldBe("0.0.0-Beta4.7");
    }

    [Test]
    public void GenerateMessageTest()
    {
        var generatedParameterMessages = this.buildServer.GenerateSetParameterMessage("name", "value");
        generatedParameterMessages.Length.ShouldBe(1);
        generatedParameterMessages[0].ShouldBe("GitVersion_name=value");
    }

    [Test]
    public void WriteAllVariablesToTheTextWriter()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        assemblyLocation.ShouldNotBeNull();
        var f = PathHelper.Combine(assemblyLocation, "jenkins_this_file_should_be_deleted.properties");

        try
        {
            AssertVariablesAreWrittenToFile(f);
        }
        finally
        {
            File.Delete(f);
        }
    }

    private void AssertVariablesAreWrittenToFile(string file)
    {
        var writes = new List<string?>();
        var semanticVersion = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "beta1",
            BuildMetaData = "5"
        };

        semanticVersion.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");
        semanticVersion.BuildMetaData.Sha = "commitSha";

        var configuration = new TestEffectiveConfiguration();
        var variableProvider = this.sp.GetRequiredService<IVariableProvider>();

        var variables = variableProvider.GetVariablesFor(semanticVersion, configuration, false);

        this.buildServer.WithPropertyFile(file);

        this.buildServer.WriteIntegration(writes.Add, variables);

        writes[1].ShouldBe("1.2.3-beta.1+5");

        File.Exists(file).ShouldBe(true);

        var props = File.ReadAllText(file);

        props.ShouldContain("GitVersion_Major=1");
        props.ShouldContain("GitVersion_Minor=2");
    }
}
