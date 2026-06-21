using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>
/// Generic submit/approve/reject engine enforcing the core business controls:
///   * maker/checker separation (a submitter may not approve their own document),
///   * amount-threshold approval (amounts over the threshold require a separate approver role),
///   * mandatory rejection reason,
///   * an audit event for every transition,
///   * posting (GL/stock) happens only on the transition to Submitted, via a caller-supplied delegate.
/// Posted documents are immutable; corrections go through cancel/reversal, never silent edits.
/// </summary>
public class WorkflowService
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _user;
    private readonly AuditService _audit;

    public WorkflowService(ErpDbContext db, ICurrentUser user, AuditService audit)
    {
        _db = db;
        _user = user;
        _audit = audit;
    }

    private bool UserHasRole(string role) =>
        string.IsNullOrEmpty(role) || _user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    private WorkflowDefinition? DefFor(string docType) =>
        _db.WorkflowDefinitions.Include(d => d.Transitions).FirstOrDefault(d => d.DocType == docType);

    /// <summary>
    /// Submit a draft document. If the amount is within the definition's auto-approve threshold the
    /// document posts immediately; otherwise it moves to PendingApproval and awaits a separate approver.
    /// </summary>
    public WorkflowInstance Submit(DocumentBase doc, string docType, decimal amount, Action onPost)
    {
        if (doc.Status != DocStatus.Draft)
            throw new DomainException($"{docType} {doc.DocNo} is not in Draft state (current: {doc.Status}).");

        var def = DefFor(docType);
        if (def != null && !UserHasRole(def.SubmitRole))
            throw new DomainException($"User '{_user.Username}' lacks the submit role '{def.SubmitRole}' for {docType}.");

        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = def?.Id ?? 0,
            DocType = docType,
            DocumentId = doc.Id,
            SubmittedBy = _user.Username,
            Amount = amount,
            CurrentState = "PendingApproval",
            CreatedBy = _user.Username
        };
        _db.WorkflowInstances.Add(instance);

        bool autoApprove = def == null || amount <= def.ApprovalThreshold;
        AddApproval(instance, WorkflowAction.Submit, "Draft", autoApprove ? "Approved" : "PendingApproval");
        _audit.Record(docType, doc.Id, "Submit", $"amount={amount}; autoApprove={autoApprove}");

        if (autoApprove)
        {
            instance.CurrentState = "Approved";
            doc.Status = DocStatus.Submitted;
            onPost();
            _audit.Record(docType, doc.Id, "Approve", "auto-approved within threshold");
        }
        return instance;
    }

    /// <summary>Approve a pending document (the checker). Enforces maker≠checker and the approver role.</summary>
    public WorkflowInstance Approve(DocumentBase doc, string docType, Action onPost)
    {
        var instance = InstanceFor(docType, doc.Id);
        if (instance.CurrentState != "PendingApproval")
            throw new DomainException($"{docType} {doc.DocNo} is not awaiting approval (state: {instance.CurrentState}).");

        var def = DefFor(docType);
        if (def != null && !UserHasRole(def.ApproverRole))
            throw new DomainException($"User '{_user.Username}' lacks the approver role '{def.ApproverRole}' for {docType}.");

        if ((def?.MakerCannotApprove ?? true) &&
            string.Equals(instance.SubmittedBy, _user.Username, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Maker/checker violation: the submitter may not approve their own document.");

        instance.CurrentState = "Approved";
        doc.Status = DocStatus.Submitted;
        AddApproval(instance, WorkflowAction.Approve, "PendingApproval", "Approved");
        onPost();
        _audit.Record(docType, doc.Id, "Approve", null);
        return instance;
    }

    /// <summary>Reject a pending document. A reason is mandatory; the document returns to Draft.</summary>
    public WorkflowInstance Reject(DocumentBase doc, string docType, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A rejection reason is required.");

        var instance = InstanceFor(docType, doc.Id);
        if (instance.CurrentState != "PendingApproval")
            throw new DomainException($"{docType} {doc.DocNo} is not awaiting approval (state: {instance.CurrentState}).");

        var def = DefFor(docType);
        if (def != null && !UserHasRole(def.ApproverRole))
            throw new DomainException($"User '{_user.Username}' lacks the approver role '{def.ApproverRole}' for {docType}.");

        instance.CurrentState = "Rejected";
        doc.Status = DocStatus.Draft;
        AddApproval(instance, WorkflowAction.Reject, "PendingApproval", "Rejected", reason);
        _audit.Record(docType, doc.Id, "Reject", reason);
        return instance;
    }

    private WorkflowInstance InstanceFor(string docType, int docId)
    {
        var instance = _db.WorkflowInstances
            .Where(w => w.DocType == docType && w.DocumentId == docId)
            .OrderByDescending(w => w.Id)
            .FirstOrDefault();
        return instance ?? throw new DomainException($"No workflow instance for {docType} #{docId}. Submit it first.");
    }

    private void AddApproval(WorkflowInstance inst, WorkflowAction action, string from, string to, string? reason = null)
    {
        inst.Approvals.Add(new WorkflowApproval
        {
            Action = action,
            ActedBy = _user.Username,
            FromState = from,
            ToState = to,
            Reason = reason,
            ActedUtc = DateTime.UtcNow,
            CreatedBy = _user.Username
        });
    }
}
