using LocalAIFactory.Core.Dtos;

namespace LocalAIFactory.Agent.Prompts;

internal static class PromptBuilder
{
    public static string BuildSystemPrompt(string baseTemplate, RetrievedContext context)
    {
        if (context.Items.Count == 0) return baseTemplate;
        return baseTemplate
            + "\n\n=== RETRIEVED PROJECT KNOWLEDGE (APPROVED ITEMS TAKE PRECEDENCE) ===\n"
            + context.AsPromptText()
            + "\n=== END RETRIEVED KNOWLEDGE ===\n"
            + "Use the approved knowledge, approved code and business rules above as the source of truth where relevant.";
    }
}
