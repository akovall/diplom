using diplom.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace diplom.Services
{
    public class ApiProjectService : IProjectService
    {
        private readonly ApiClient _api;

        public ApiProjectService()
        {
            _api = ApiClient.Instance;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _api.GetAsync<List<Project>>("/api/projects") ?? new();
        }

        public async Task<List<Project>> GetActiveProjectsAsync()
        {
            return await _api.GetAsync<List<Project>>("/api/projects/active") ?? new();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _api.GetAsync<Project>($"/api/projects/{id}");
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            return await _api.PostAsync<Project>("/api/projects", project) ?? project;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            return await _api.PutAsync<Project>($"/api/projects/{project.Id}", project) ?? project;
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            try
            {
                await _api.DeleteAsync($"/api/projects/{id}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ArchiveProjectAsync(int id)
        {
            try
            {
                await _api.PutAsync<object>($"/api/projects/{id}/archive", new { });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
