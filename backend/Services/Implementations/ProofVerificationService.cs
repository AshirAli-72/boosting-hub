using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public partial class ProofVerificationService : IProofVerificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ProofVerificationService> _logger;
    private readonly HttpClient _httpClient;
    private const int MaxUrlLength = 2048;
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

    private static readonly Dictionary<string, Regex> PlatformPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["YouTube"] = YouTubeRegex(),
        ["Instagram"] = InstagramRegex(),
        ["TikTok"] = TikTokRegex(),
        ["Facebook"] = FacebookRegex(),
        ["X"] = XRegex(),
        ["Twitter"] = XRegex(),
    };

    [GeneratedRegex(@"^(https?://)?(www\.)?(youtube\.com/watch\?v=|youtu\.be/)[\w-]{11}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex YouTubeRegex();

    [GeneratedRegex(@"^(https?://)?(www\.)?instagram\.com/(p|reel|tv)/[\w-]+|^(https?://)?(www\.)?instagram\.com/[\w.-]+/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InstagramRegex();

    [GeneratedRegex(@"^(https?://)?(www\.)?tiktok\.com/@[\w.-]+/video/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TikTokRegex();

    [GeneratedRegex(@"^(https?://)?(www\.)?(facebook\.com|fb\.watch)/(watch\?v=|[\w./-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FacebookRegex();

    [GeneratedRegex(@"^(https?://)?(www\.)?(x\.com|twitter\.com)/\w+/status/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex XRegex();

    private static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit.ly", "tinyurl.com", "shorturl.at", "goo.gl", "t.co",
        "ow.ly", "is.gd", "buff.ly", "rebrand.ly", "cutt.ly",
    };

    public ProofVerificationService(ApplicationDbContext db, ILogger<ProofVerificationService> logger, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ProofVerification");
    }

    public async Task<ProofVerificationResult> ValidateProofAsync(int taskId, string proofUrl, int userId)
    {
        var task = await _db.TaskGenerates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            return Fail("Task not found");

        if (task.Status != "Active")
            return Fail("Task is no longer active");

        var accepted = await _db.AcceptedTasks.AnyAsync(a => a.UserId == userId && a.TaskId == taskId);
        if (!accepted)
            return Fail("You have not accepted this task");

        var urlResult = await ValidateUrlAsync(proofUrl);
        if (!urlResult.Success) return urlResult;

        var platformResult = await ValidatePlatformAsync(proofUrl, task.Platform);
        if (!platformResult.Success) return platformResult;

        var campaignResult = await VerifyCampaignMatchAsync(proofUrl, taskId);
        if (!campaignResult.Success) return campaignResult;

        var duplicateResult = await CheckDuplicateAsync(proofUrl, taskId, userId);
        if (!duplicateResult.Success) return duplicateResult;

        return new ProofVerificationResult
        {
            Success = true,
            VerificationStatus = "PendingReview",
            ErrorMessage = null
        };
    }

    public Task<ProofVerificationResult> ValidateUrlAsync(string proofUrl)
    {
        if (string.IsNullOrWhiteSpace(proofUrl))
            return Task.FromResult(Fail("Proof URL cannot be empty"));

        if (proofUrl.Length > MaxUrlLength)
            return Task.FromResult(Fail($"Proof URL exceeds maximum length of {MaxUrlLength} characters"));

        if (!Uri.TryCreate(proofUrl, UriKind.Absolute, out var uri))
            return Task.FromResult(Fail("Proof URL is not a valid absolute URI"));

        if (uri.Scheme != "https")
            return Task.FromResult(Fail("Proof URL must use HTTPS"));

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www."))
            host = host[4..];

        if (BlockedDomains.Contains(host))
            return Task.FromResult(Fail("URL shorteners and known malicious domains are not allowed"));

        var suspiciousPatterns = new[] { "<script", "javascript:", "data:", "vbscript:", "onload=", "onerror=" };
        if (suspiciousPatterns.Any(p => proofUrl.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(Fail("Proof URL contains suspicious content"));

        return Task.FromResult(new ProofVerificationResult { Success = true, VerificationStatus = "None" });
    }

    public async Task<ProofVerificationResult> ValidatePlatformAsync(string proofUrl, string expectedPlatform)
    {
        if (!PlatformPatterns.TryGetValue(expectedPlatform, out var pattern))
        {
            _logger.LogWarning("Unknown platform {Platform}; skipping platform validation", expectedPlatform);
            return new ProofVerificationResult { Success = true, VerificationStatus = "None" };
        }

        if (!pattern.IsMatch(proofUrl))
            return Fail($"Proof URL does not match the expected platform ({expectedPlatform})");

        if (!Uri.TryCreate(proofUrl, UriKind.Absolute, out var uri))
            return Fail("Invalid URI");

        try
        {
            using var cts = new CancellationTokenSource(HttpTimeout);
            var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ProofVerificationResult { Success = true, VerificationStatus = "None" };
            }

            _logger.LogWarning("Proof URL {Url} returned HTTP {Status}", proofUrl, (int)response.StatusCode);
            return Fail($"Proof URL returned HTTP {(int)response.StatusCode} — it may not be accessible");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Proof URL {Url} timed out", proofUrl);
            return Fail("Proof URL is not reachable (request timed out)");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Proof URL {Url} request failed", proofUrl);
            return Fail("Proof URL is not reachable");
        }
    }

    public async Task<ProofVerificationResult> CheckDuplicateAsync(string proofUrl, int taskId, int userId)
    {
        var existingActiveProof = await _db.TaskProofs.AnyAsync(p =>
            p.UserId == userId &&
            p.TaskId == taskId &&
            p.VerificationStatus != "Rejected");
        if (existingActiveProof)
            return Fail("You have already submitted a proof for this task");

        return new ProofVerificationResult { Success = true, VerificationStatus = "None" };
    }

    public async Task<ProofVerificationResult> VerifyCampaignMatchAsync(string proofUrl, int taskId)
    {
        var task = await _db.TaskGenerates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            return Fail("Task not found");

        if (string.IsNullOrWhiteSpace(task.Url))
            return new ProofVerificationResult { Success = true, VerificationStatus = "None" };

        var isVideoTask = task.Service?.Contains("video", StringComparison.OrdinalIgnoreCase) == true ||
                          task.Service?.Contains("view", StringComparison.OrdinalIgnoreCase) == true ||
                          task.Service?.Contains("like", StringComparison.OrdinalIgnoreCase) == true ||
                          task.Service?.Contains("comment", StringComparison.OrdinalIgnoreCase) == true ||
                          task.Service?.Contains("subscribe", StringComparison.OrdinalIgnoreCase) == true;

        if (isVideoTask)
        {
            if (!Uri.TryCreate(proofUrl, UriKind.Absolute, out var proofUri))
                return Fail("Invalid proof URI");

            if (!Uri.TryCreate(task.Url, UriKind.Absolute, out var taskUri))
                return new ProofVerificationResult { Success = true, VerificationStatus = "None" };

            var proofContentId = ExtractContentId(proofUri, task.Platform);
            var taskContentId = ExtractContentId(taskUri, task.Platform);
            if (!string.IsNullOrEmpty(taskContentId) && proofContentId != taskContentId)
                return Fail("Proof URL does not match the campaign's target content");
        }

        return new ProofVerificationResult { Success = true, VerificationStatus = "None" };
    }

    private static string ExtractContentId(Uri uri, string platform)
    {
        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("youtube") || host.Contains("youtu.be"))
        {
            var query = uri.Query;
            if (query.Contains("v="))
            {
                var match = Regex.Match(query, @"[?&]v=([\w-]{11})");
                if (match.Success) return match.Groups[1].Value;
            }
            if (uri.Segments.Length > 0 && host.Contains("youtu.be"))
                return uri.Segments[^1].TrimEnd('/');
        }

        if (host.Contains("instagram"))
        {
            var segs = uri.Segments.Select(s => s.TrimEnd('/')).Where(s => s.Length > 0).ToArray();
            if (segs.Length >= 2 && (segs[0] == "p" || segs[0] == "reel" || segs[0] == "tv"))
                return segs[1];
        }

        if (host.Contains("tiktok"))
        {
            var match = Regex.Match(uri.AbsolutePath, @"/video/(\d+)");
            if (match.Success) return match.Groups[1].Value;
        }

        if (host.Contains("facebook") || host.Contains("fb.watch"))
        {
            var query = uri.Query;
            var match = Regex.Match(query, @"[?&]v=([\w-]+)");
            if (match.Success) return match.Groups[1].Value;
        }

        if (host.Contains("x.com") || host.Contains("twitter"))
        {
            var match = Regex.Match(uri.AbsolutePath, @"/\w+/status/(\d+)");
            if (match.Success) return match.Groups[1].Value;
        }

        return string.Empty;
    }

    private static ProofVerificationResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message, VerificationStatus = "Rejected" };
}
