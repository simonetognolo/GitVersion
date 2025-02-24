using GitVersion.BuildAgents;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionConverters.OutputGenerator;

public interface IOutputGenerator : IVersionConverter<OutputContext>
{
}

public sealed class OutputGenerator : IOutputGenerator
{
    private readonly IConsole console;
    private readonly IFileSystem fileSystem;
    private readonly IOptions<GitVersionOptions> options;
    private readonly ICurrentBuildAgent buildAgent;

    public OutputGenerator(ICurrentBuildAgent buildAgent, IConsole console, IFileSystem fileSystem, IOptions<GitVersionOptions> options)
    {
        this.console = console.NotNull();
        this.fileSystem = fileSystem.NotNull();
        this.options = options.NotNull();
        this.buildAgent = buildAgent.NotNull();
    }

    public void Execute(VersionVariables variables, OutputContext context)
    {
        var gitVersionOptions = this.options.Value;
        if (gitVersionOptions.Output.Contains(OutputType.BuildServer))
        {
            this.buildAgent.WriteIntegration(this.console.WriteLine, variables, context.UpdateBuildNumber ?? true);
        }
        if (gitVersionOptions.Output.Contains(OutputType.File))
        {
            var retryOperation = new RetryAction<IOException>();
            retryOperation.Execute(() => this.fileSystem.WriteAllText(context.OutputFile, variables.ToString()));
        }

        if (!gitVersionOptions.Output.Contains(OutputType.Json)) return;

        switch (gitVersionOptions.ShowVariable)
        {
            case null:
                this.console.WriteLine(variables.ToString());
                break;

            default:
                if (!variables.TryGetValue(gitVersionOptions.ShowVariable, out var part))
                {
                    throw new WarningException($"'{gitVersionOptions.ShowVariable}' variable does not exist");
                }

                this.console.WriteLine(part);
                break;
        }
    }

    public void Dispose()
    {
    }
}
