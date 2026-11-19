using System.Text;
using AI4SchoolAgent.Data;
using AI4SchoolAgent.Tools;
using GitHub.Copilot;
using Microsoft.Extensions.AI;

namespace AI4SchoolAgent.Services;

public class AgentService {
    private const string AutoModelId = "auto";

    private readonly SchoolDbContext _db;
    private readonly IConfiguration _configuration;

    public AgentService(SchoolDbContext db, IConfiguration configuration) {
        _db = db;
        _configuration = configuration;
    }

    /// <summary>Returns the models available to the configured GitHub account.</summary>
    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync() {
        await using var client = CreateCopilotClient();
        await client.StartAsync();

        var models = await client.ListModelsAsync();
        return models
            .Select(model => model.Id)
            .Prepend(AutoModelId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Gets a response from the agent using the requested available model.</summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="modelId">The ID of an available model.</param>
    /// <returns>The agent's response.</returns>
    public async Task<string> AskAsync(string prompt, string modelId) {
        await using var client = CreateCopilotClient();
        await client.StartAsync();

        var selectedModel = string.Equals(modelId, AutoModelId, StringComparison.OrdinalIgnoreCase)
            ? AutoModelId
            : (await client.ListModelsAsync())
                .Select(model => model.Id)
                .FirstOrDefault(model =>
                    string.Equals(model, modelId, StringComparison.OrdinalIgnoreCase));

        if (selectedModel is null)
            throw new ArgumentException("Select a model available to your GitHub account.", nameof(modelId));

        var toolService = new StudentsAgentTools(_db);

        var tools = new List<AIFunctionDeclaration> {
            AIFunctionFactory.Create(
                async (int studentId) =>
                    await toolService.GetStudentDetailsByIdAsync(studentId),
                "get_student_details",
                "Look up student details."),

            AIFunctionFactory.Create(
                async (int studentId, string reason) =>
                    await toolService.ProcessWithdrawalAsync(studentId, reason),
                "process_withdrawal",
                "Process a withdrawal request."),

            AIFunctionFactory.Create(
                async (string gender) =>
                    await toolService.GetStudentsByGenderAsync(gender),
                "get_students_by_gender",
                "Find students by gender"),

            AIFunctionFactory.Create(
                async (string school) =>
                    await toolService.GetStudentsBySchoolAsync(school),
                "get_students_by_school",
                "Find students by school"),

            AIFunctionFactory.Create(
                async () =>
                    await toolService.GetStudentsByAgeAsync(),
                "get_students_by_age",
                "Find students by age"),

            AIFunctionFactory.Create(
                async () =>
                    await toolService.GetAllStudentsAsync(),
                "get_all_students",
                "Get all students")
        };

        var config = new SessionConfig {
            Model = selectedModel,
            OnPermissionRequest = PermissionHandler.ApproveAll,
            Tools = tools
        };

        await using var session = await client.CreateSessionAsync(config);

        var responseBuilder = new StringBuilder();

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        session.On<SessionEvent>(evt => {
            switch (evt) {
                case AssistantMessageEvent msg:
                    responseBuilder.Append(msg.Data.Content);
                    break;

                case SessionIdleEvent:
                    tcs.TrySetResult(responseBuilder.ToString());
                    break;
            }
        });

        await session.SendAsync(new MessageOptions {
            Prompt = prompt
        });

        return await tcs.Task.WaitAsync(
            TimeSpan.FromSeconds(30));
    }

    private CopilotClient CreateCopilotClient() {
        var githubToken = _configuration["GitHubToken"];
        if (string.IsNullOrWhiteSpace(githubToken)) {
            throw new InvalidOperationException(
                "Configure a GitHub PAT with the Copilot:GitHubToken setting (environment variable Copilot__GitHubToken).");
        }

        return new CopilotClient(new CopilotClientOptions {
            GitHubToken = githubToken,
            UseLoggedInUser = false,
            LogLevel = CopilotLogLevel.Info
        });
    }
}

