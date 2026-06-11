using System.ComponentModel.DataAnnotations;

namespace FellsideDigital.Domain.Enums;

/// <summary>
/// Controls whether a note or timeline event is visible to the client in the portal,
/// or kept internal to admins only (ServiceNow work-note style).
/// </summary>
public enum TimelineVisibility
{
    Internal,
    [Display(Name = "Client visible")] ClientVisible
}

/// <summary>
/// The kind of project timeline event. Drives icon/tone selection in
/// <c>TimelineEventPresenter</c>; the human-readable description is snapshotted on each
/// event's <c>Summary</c>. New types can be added here without schema changes — the last
/// four are reserved for future file/document/task subsystems and are not yet emitted.
/// </summary>
public enum TimelineEventType
{
    ProjectCreated,
    [Display(Name = "Status changed")] StatusChanged,
    [Display(Name = "Phase changed")] PhaseChanged,
    [Display(Name = "Milestone completed")] MilestoneCompleted,
    [Display(Name = "Project completed")] ProjectCompleted,
    [Display(Name = "Project reopened")] ProjectReopened,
    [Display(Name = "Note added")] NoteAdded,
    [Display(Name = "Invoice created")] InvoiceCreated,
    [Display(Name = "Invoice updated")] InvoiceUpdated,
    [Display(Name = "Invoice sent")] InvoiceSent,
    [Display(Name = "Invoice paid")] InvoicePaid,
    [Display(Name = "Invoice overdue")] InvoiceOverdue,
    [Display(Name = "Invoice viewed")] InvoiceViewed,

    // Reserved for future subsystems — not yet emitted.
    [Display(Name = "File uploaded")] FileUploaded,
    [Display(Name = "Document shared")] DocumentShared,
    [Display(Name = "Task created")] TaskCreated,
    [Display(Name = "Task completed")] TaskCompleted
}
