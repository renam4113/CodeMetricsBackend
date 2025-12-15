using Microsoft.AspNetCore.Mvc;
using CodeMetricsApi.Models;
using CodeMetrics.Clients;
using CodeMetrics.Service;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Cors;

namespace CodeMetricsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class CodeMetricsController : ControllerBase
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Instance
        {
            Project,
            Repository
        }

        private readonly ICodeMetricsService _service;

        public CodeMetricsController(ICodeMetricsService instanceService)
        {
            _service = instanceService;
        }



        [HttpGet("author/summary")]
        public async Task<IActionResult> GetAuthorSummary(
             [FromQuery] string email,
             [FromQuery] DateTimeOffset startDate,
             [FromQuery] DateTimeOffset endDate)
        {
            var result = await _service.GetAuthorSummaryAsync(email, startDate, endDate);
            return Ok(result);
        }


        [HttpGet("ProjectMetric")]
        public async Task<IActionResult> GetProjectMetric([FromQuery] string name, [FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate)
        {
            try
            {
                 return await _service.GetMetricByProject(name, startDate, endDate);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("GetByPeriod")]
        public async Task<IActionResult> GetCommitsByPeriod([FromQuery] Instance instance, [FromQuery]string name, [FromQuery] DateTimeOffset startDate,[FromQuery] DateTimeOffset endDate)
        {
            try
            {
                if (instance.ToString() == "Project") return await _service.GetProjectCommitsByPeriod(name, startDate, endDate);
                else return await _service.GetRepoCommitsByPeriod(name, startDate, endDate);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("Commits")]
        public async Task<IActionResult> GetCommits([FromQuery] string projectKey, [FromQuery] string repoName)
        {
            try
            {
                var response  = await _service.GetCommits(projectKey, repoName);
                return response;
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpGet("Commit")]
        public async Task<IActionResult> GetCommit([FromQuery] string projectKey, [FromQuery] string repoName, [FromQuery] string hash)
        {
            try
            {
                var response = await _service.GetCommit(projectKey, repoName, hash);
                return response;
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("author/performance")]
        public async Task<IActionResult> GetAuthorPerformance(
    [FromQuery] string email,
    [FromQuery] DateTimeOffset startDate,
    [FromQuery] DateTimeOffset endDate)
        {
            var result = await _service.GetAuthorPerformanceAsync(email, startDate, endDate);
            return Ok(result);
        }

        [HttpPost("UpdateDataBase")]
        public async Task<IActionResult> UpdateDataBase([FromQuery] string? projectKey, [FromQuery] string? repoName, [FromQuery] string? branch, [FromQuery] int limit = 100)
        {
            try
            {
                var response = await _service.UpdateDataBase(projectKey, repoName, branch, limit);
                return response;
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
