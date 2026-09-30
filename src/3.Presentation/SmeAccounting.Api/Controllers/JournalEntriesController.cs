using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmeAccounting.Api.Extensions;
using SmeAccounting.Api.Requests;
using SmeAccounting.Application.Features.JournalEntries.Dtos;

namespace SmeAccounting.Api.Controllers;

[ApiController]
[Route("api/v1/journal-entries")]
public sealed class JournalEntriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public JournalEntriesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Create draft + post journal entry. Debits must equal credits. Closed period returns 422.</summary>
    /// <response code="200">Posted entry.</response>
    /// <response code="400">Shape validation failed.</response>
    /// <response code="422">Domain invariant violated (GL.UNBALANCED, ACCOUNT.CLOSED_PERIOD).</response>
    [HttpPost]
    [ProducesResponseType(typeof(JournalEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<JournalEntryDto>> Post([FromBody] PostJournalEntryRequest req, CancellationToken ct)
        => (await _mediator.Send(req.ToCommand(), ct)).ToActionResult();
}
