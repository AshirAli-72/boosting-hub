using BoostingHub.backend.DTOs;

namespace BoostingHub.backend.Services.Interfaces;

/// <summary>Auto-validates proof URLs before manual admin review.</summary>
public interface IProofVerificationService
{
    /// <summary>Runs all validation rules and returns the result.</summary>
    Task<ProofVerificationResult> ValidateProofAsync(int taskId, string proofUrl, int userId);

    /// <summary>Validates URL format, scheme, and reachability.</summary>
    Task<ProofVerificationResult> ValidateUrlAsync(string proofUrl);

    /// <summary>Checks the URL domain matches the expected platform.</summary>
    Task<ProofVerificationResult> ValidatePlatformAsync(string proofUrl, string expectedPlatform);

    /// <summary>Checks the same URL hasn't been submitted, and the worker hasn't already submitted for this task.</summary>
    Task<ProofVerificationResult> CheckDuplicateAsync(string proofUrl, int taskId, int userId);

    /// <summary>Verifies the proof URL aligns with the campaign/task requirements.</summary>
    Task<ProofVerificationResult> VerifyCampaignMatchAsync(string proofUrl, int taskId);
}
