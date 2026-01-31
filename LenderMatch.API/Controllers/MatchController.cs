using LenderMatch.API.Data;
using LenderMatch.API.Entities;
using LenderMatch.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LenderMatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly MatchingService _matchingService;
    private readonly AppDbContext _context;
    private readonly ILogger<MatchController> _logger;

    public MatchController(
        MatchingService matchingService,
        AppDbContext context,
        ILogger<MatchController> logger)
    {
        _matchingService = matchingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Submit a loan application for lender matching
    /// POST /api/match
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MatchingWorkflowResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitApplication([FromBody] LoanApplication app)
    {
        try
        {
            _logger.LogInformation("Processing loan application for {BusinessName}", app.Business?.BusinessName ?? "Unknown");

            // Basic null checks before processing
            if (app == null)
            {
                return BadRequest(new { error = "Application cannot be null" });
            }

            // Execute the matching workflow
            var workflowResult = await _matchingService.EvaluateAsync(app);

            // If validation failed, return bad request with details
            if (!workflowResult.IsValid)
            {
                _logger.LogWarning("Application validation failed: {Errors}",
                    string.Join(", ", workflowResult.ValidationErrors));

                return BadRequest(new
                {
                    error = "Application validation failed",
                    validationErrors = workflowResult.ValidationErrors,
                    timestamp = workflowResult.EvaluatedAt
                });
            }

            // Persist the application and results
            await PersistApplicationAndResults(app, workflowResult);

            _logger.LogInformation(
                "Application processed successfully. {EligibleCount} of {TotalCount} lenders eligible",
                workflowResult.EligibleCount,
                workflowResult.TotalEvaluated);

            return Ok(workflowResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing loan application");
            return StatusCode(500, new
            {
                error = "An error occurred while processing the application",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get a specific loan application by ID
    /// GET /api/match/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LoanApplication), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplication(int id)
    {
        try
        {
            var application = await _context.LoanApplications
                .Include(a => a.Business)
                .Include(a => a.Guarantor)
                .Include(a => a.CreditProfile)
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound(new { error = $"Application with ID {id} not found" });
            }

            return Ok(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {Id}", id);
            return StatusCode(500, new { error = "Error retrieving application" });
        }
    }

    /// <summary>
    /// Re-evaluate an existing application against current lender criteria
    /// POST /api/match/{id}/re-evaluate
    /// </summary>
    [HttpPost("{id}/re-evaluate")]
    [ProducesResponseType(typeof(MatchingWorkflowResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReEvaluateApplication(int id)
    {
        try
        {
            var application = await _context.LoanApplications
                .Include(a => a.Business)
                .Include(a => a.Guarantor)
                .Include(a => a.CreditProfile)
                .Include(a => a.Request)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound(new { error = $"Application with ID {id} not found" });
            }

            _logger.LogInformation("Re-evaluating application {Id}", id);

            var workflowResult = await _matchingService.EvaluateAsync(application);

            return Ok(workflowResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-evaluating application {Id}", id);
            return StatusCode(500, new { error = "Error re-evaluating application" });
        }
    }

    /// <summary>
    /// Get all lenders and their programs
    /// GET /api/match/lenders
    /// </summary>
    [HttpGet("lenders")]
    [ProducesResponseType(typeof(List<Lender>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLenders()
    {
        try
        {
            var lenders = await _context.Lenders
                .Include(l => l.Programs)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Ok(lenders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lenders");
            return StatusCode(500, new { error = "Error retrieving lenders" });
        }
    }

    /// <summary>
    /// Validate an application without persisting or full evaluation
    /// POST /api/match/validate
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateApplication([FromBody] LoanApplication app)
    {
        try
        {
            // Quick validation only (don't run full matching)
            var workflowResult = await _matchingService.EvaluateAsync(app);

            var validationResult = new ValidationResult
            {
                IsValid = workflowResult.IsValid,
                Errors = workflowResult.ValidationErrors,
                DerivedFeatures = workflowResult.DerivedFeatures,
                ValidatedAt = workflowResult.EvaluatedAt
            };

            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating application");
            return StatusCode(500, new { error = "Error validating application" });
        }
    }

    /// <summary>
    /// Get matching statistics for dashboard
    /// GET /api/match/statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(MatchingStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var totalApplications = await _context.LoanApplications.CountAsync();
            var lenderCount = await _context.Lenders.CountAsync();
            var programCount = await _context.Lenders
                .SelectMany(l => l.Programs)
                .CountAsync();

            var stats = new MatchingStatistics
            {
                TotalApplications = totalApplications,
                TotalLenders = lenderCount,
                TotalPrograms = programCount,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return StatusCode(500, new { error = "Error retrieving statistics" });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Persists the loan application and matching results to the database
    /// </summary>
    private async Task PersistApplicationAndResults(LoanApplication app, MatchingWorkflowResult workflowResult)
    {
        try
        {
            // Only persist if application doesn't already have an ID
            if (app.Id == 0)
            {
                _context.LoanApplications.Add(app);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Persisted loan application with ID {Id}", app.Id);
            }

            // Note: In a real application, you would also persist MatchResults
            // This would require adding a MatchResult entity to the DbContext
            // For now, we're just persisting the application itself
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting application and results");
            throw;
        }
    }

    #endregion
}

#region DTOs

/// <summary>
/// Validation-only result without full matching
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DerivedFeatures DerivedFeatures { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Statistics for monitoring and dashboard
/// </summary>
public class MatchingStatistics
{
    public int TotalApplications { get; set; }
    public int TotalLenders { get; set; }
    public int TotalPrograms { get; set; }
    public DateTime GeneratedAt { get; set; }
}

#endregion