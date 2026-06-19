namespace LocalAIFactory.Core.Abstractions;

// Approval is how the platform learns. Approving an item flips its status, audits it, and
// re-indexes it so it is retrieved first in future prompts.
public interface IApprovalService
{
    Task ApproveKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default);
    Task DeprecateKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default);
    Task DeleteKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default);
    Task ApproveBusinessRuleAsync(int businessRuleId, CancellationToken ct = default);
    Task ApproveCodeSnippetAsync(int approvedCodeSnippetId, CancellationToken ct = default);
    Task<int> PromoteCodeBlockAsync(int extractedCodeBlockId, string title, CancellationToken ct = default);
    Task ApproveProjectProfileSectionAsync(int sectionId, CancellationToken ct = default);

    // Phase 1.1 bulk operations. 'kind' is one of:
    // knowledge | rule | entity | relationship | section | code
    Task<int> BulkApproveAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default);
    Task<int> BulkDeprecateAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default);
    Task<int> BulkDeleteAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default);
    Task<int> BulkPromoteCodeBlocksAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
