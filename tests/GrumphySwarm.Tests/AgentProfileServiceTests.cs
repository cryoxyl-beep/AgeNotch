using System.IO;
using GrumphySwarm.Core.Models;
using GrumphySwarm.Services;
using Xunit;

namespace GrumphySwarm.Tests;

public class AgentProfileServiceTests
{
    [Fact]
    public async Task GetProfilesAsync_WhenFileDoesNotExist_SeedsDefaultProfiles()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"agents_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonAgentProfileService(tempFile);
            var profiles = await service.GetProfilesAsync();

            Assert.NotNull(profiles);
            Assert.NotEmpty(profiles);
            Assert.Contains(profiles, p => p.TaskType == AgentTaskType.DesktopAssistant);
            Assert.Contains(profiles, p => p.TaskType == AgentTaskType.TaskMonitor);
            Assert.All(profiles, p => Assert.Contains("Standby", p.StatusNote));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveProfileAsync_PersistsAndUpdatesProfile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"agents_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonAgentProfileService(tempFile);

            var newProfile = new AgentProfile
            {
                Id = "agent_custom_test",
                Name = "Codebase Reviewer",
                Description = "Reviews PRs and audits code quality.",
                TaskType = AgentTaskType.Custom,
                StatusNote = "Standby (Execution Not Connected)"
            };

            await service.SaveProfileAsync(newProfile);

            var loadedProfiles = await service.GetProfilesAsync();
            var saved = loadedProfiles.FirstOrDefault(p => p.Id == "agent_custom_test");

            Assert.NotNull(saved);
            Assert.Equal("Codebase Reviewer", saved.Name);
            Assert.Equal(AgentTaskType.Custom, saved.TaskType);

            // Update
            saved.Description = "Updated description for code review.";
            await service.SaveProfileAsync(saved);

            var reloaded = await service.GetProfilesAsync();
            var updated = reloaded.FirstOrDefault(p => p.Id == "agent_custom_test");
            Assert.NotNull(updated);
            Assert.Equal("Updated description for code review.", updated.Description);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteProfileAsync_RemovesProfileFromStorage()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"agents_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonAgentProfileService(tempFile);

            var profile = new AgentProfile
            {
                Id = "agent_to_delete",
                Name = "Temporary Agent",
                Description = "Will be deleted."
            };

            await service.SaveProfileAsync(profile);
            var initial = await service.GetProfilesAsync();
            Assert.Contains(initial, p => p.Id == "agent_to_delete");

            bool deleted = await service.DeleteProfileAsync("agent_to_delete");
            Assert.True(deleted);

            var remaining = await service.GetProfilesAsync();
            Assert.DoesNotContain(remaining, p => p.Id == "agent_to_delete");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
