using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.API.Controllers.Admin
{
    [Route("api/admin/projects")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(IUnitOfWork unitOfWork, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            List<Project> projects;
            if (User.IsInRole("investor"))
            {
                var user = await _userManager.GetUserAsync(User);
                var investorId = user?.InvestorId ?? 0;
                projects = await _context.Projects
                    .Where(p => _context.InvestorCommitments.Any(c => c.InvestorId == investorId && c.ProjectId == p.Id))
                    .ToListAsync();
            }
            else
            {
                projects = await _context.Projects.ToListAsync();
            }

            var mapped = new List<object>();
            foreach (var p in projects)
            {
                mapped.Add(new
                {
                    id = p.Id.ToString(),
                    title = p.Title,
                    description = p.Description,
                    budget = p.TargetFunding ?? 0,
                    duration = "120 Days",
                    start_date = p.LaunchDate?.ToString("yyyy-MM-dd") ?? "—",
                    end_date = p.LaunchDate?.AddDays(120).ToString("yyyy-MM-dd") ?? "—",
                    comments = "Centralized project profile details",
                    status = p.Status == "Closed" || p.Status == "inactive" ? "inactive" : "active"
                });
            }
            return Ok(mapped);
        }

        [HttpPost]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateUpdateDTO dto)
        {
            var project = new Project
            {
                Title = dto.title,
                Description = dto.description,
                TargetFunding = dto.budget,
                FundedAmount = 0,
                LaunchDate = DateTime.TryParse(dto.start_date, out var sd) ? sd : DateTime.UtcNow,
                Status = dto.status == "inactive" ? "Closed" : "Open"
            };

            await _context.Projects.AddAsync(project);
            await _unitOfWork.CompleteAsync();

            return Ok(new
            {
                id = project.Id.ToString(),
                title = project.Title,
                description = project.Description,
                budget = project.TargetFunding,
                duration = dto.duration ?? "120 Days",
                start_date = project.LaunchDate?.ToString("yyyy-MM-dd"),
                end_date = project.LaunchDate?.AddDays(120).ToString("yyyy-MM-dd"),
                comments = dto.comments,
                status = dto.status
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] ProjectCreateUpdateDTO dto)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound(new { Message = "Project not found" });

            project.Title = dto.title;
            project.Description = dto.description;
            project.TargetFunding = dto.budget;
            if (DateTime.TryParse(dto.start_date, out var sd))
            {
                project.LaunchDate = sd;
            }
            project.Status = dto.status == "inactive" ? "Closed" : "Open";

            await _unitOfWork.CompleteAsync();

            return Ok(new
            {
                id = project.Id.ToString(),
                title = project.Title,
                description = project.Description,
                budget = project.TargetFunding,
                duration = dto.duration ?? "120 Days",
                start_date = project.LaunchDate?.ToString("yyyy-MM-dd"),
                end_date = project.LaunchDate?.AddDays(120).ToString("yyyy-MM-dd"),
                comments = dto.comments,
                status = dto.status
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound(new { Message = "Project not found" });

            _context.Projects.Remove(project);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Policy = "ElevatedOrManager")]
        public async Task<IActionResult> UpdateProjectStatus(int id, [FromBody] StatusUpdateDTO dto)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound(new { Message = "Project not found" });

            project.Status = dto.status == "inactive" ? "Closed" : "Open";
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "Project status modified." });
        }
    }

    public class ProjectCreateUpdateDTO
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal budget { get; set; }
        public string duration { get; set; } = string.Empty;
        public string start_date { get; set; } = string.Empty;
        public string end_date { get; set; } = string.Empty;
        public string comments { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    public class StatusUpdateDTO
    {
        public string status { get; set; } = string.Empty;
    }
}
