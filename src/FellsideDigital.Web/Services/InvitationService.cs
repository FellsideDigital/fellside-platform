using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FellsideDigital.Web.Services;

public class InvitationService(
    FellsideDigitalDbContext db,
    EmailService emailService,
    NavigationManager navigationManager,
    ILogger<InvitationService> logger) : IInvitationService
{
    private const int ExpiryDays = 7;

    public async Task<(ClientInvitation? Invitation, string? EmailError)> CreateInvitationAsync(ClientInvitation model, string adminUserId)
    {
        model.Id = Guid.NewGuid();
        model.Token = GenerateToken();
        model.CreatedAt = DateTime.UtcNow;
        model.ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays);
        model.Status = InvitationStatus.Pending;
        model.CreatedByUserId = adminUserId;

        db.ClientInvitations.Add(model);
        await db.SaveChangesAsync();

        var registrationUrl = navigationManager.ToAbsoluteUri(
            $"/Account/Register?token={Uri.EscapeDataString(model.Token)}").ToString();

        string? emailError = null;
        try
        {
            await emailService.SendInvitationAsync(model, registrationUrl);
        }
        catch (Exception ex)
        {
            emailError = ex.Message;
            logger.LogError(ex, "Invitation created but email failed to send for {Email}", model.Email);
        }

        return (model, emailError);
    }

    public async Task<ClientInvitation?> GetInvitationByTokenAsync(string token)
    {
        var invitation = await db.ClientInvitations
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation is null) return null;

        if (invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await db.SaveChangesAsync();
        }

        return invitation.Status == InvitationStatus.Pending ? invitation : null;
    }

    public async Task AcceptInvitationAsync(Guid invitationId, string newUserId)
    {
        var invitation = await db.ClientInvitations.FindAsync(invitationId);
        if (invitation is null) return;

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedUserId = newUserId;

        await db.SaveChangesAsync();
    }

    public Task<List<ClientInvitation>> GetAllInvitationsAsync() =>
        db.ClientInvitations
            .Include(i => i.CreatedBy)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

    public Task<List<ClientInvitation>> GetValidInvitationsAsync() =>
    db.ClientInvitations
        .Include(i => i.CreatedBy)
        .OrderByDescending(i => i.CreatedAt)
        .Where(i => i.Status != InvitationStatus.Revoked)
        .ToListAsync();

    public async Task<string?> ResendInvitationAsync(Guid id)
    {
        var invitation = await db.ClientInvitations.FindAsync(id);
        if (invitation is null || invitation.Status != InvitationStatus.Pending)
            return "Only pending invitations can be resent.";

        invitation.ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays);
        await db.SaveChangesAsync();

        var registrationUrl = navigationManager.ToAbsoluteUri(
            $"/Account/Register?token={Uri.EscapeDataString(invitation.Token)}").ToString();

        try
        {
            await emailService.SendInvitationAsync(invitation, registrationUrl);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend invitation email for {Email}", invitation.Email);
            return ex.Message;
        }
    }

    public async Task RevokeInvitationAsync(Guid id)
    {
        var invitation = await db.ClientInvitations.FindAsync(id);
        if (invitation is null) return;

        invitation.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync();
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
