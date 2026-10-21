using System.IO;
using GrumphySwarm.Core.Settings;
using Xunit;

namespace GrumphySwarm.Tests;

public class JsonSettingsServiceTests
{
    [Fact]
    public async Task LoadSettings_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonSettingsService(tempFile);
            var settings = await service.LoadSettingsAsync();

            Assert.NotNull(settings);
            Assert.Equal(240.0, settings.CollapsedWidth);
            Assert.Equal(34.0, settings.CollapsedHeight);
            Assert.Equal(660.0, settings.ExpandedWidth);
            Assert.Equal(140.0, settings.ExpandedHeight);
            Assert.Equal(NotchHorizontalPosition.Center, settings.HorizontalPosition);
            Assert.Equal(0.0, settings.HorizontalOffset);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveSettings_AndLoad_PersistsCustomValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new JsonSettingsService(tempFile);
            var customSettings = new NotchSettings
            {
                CollapsedWidth = 300,
                ExpandedWidth = 750,
                HoverCollapseDelayMs = 500,
                StartPinned = true,
                AccentColorHex = "#3B82F6"
            };

            await service.SaveSettingsAsync(customSettings);

            var loadedSettings = await service.LoadSettingsAsync();

            Assert.NotNull(loadedSettings);
            Assert.Equal(300, loadedSettings.CollapsedWidth);
            Assert.Equal(750, loadedSettings.ExpandedWidth);
            Assert.Equal(500, loadedSettings.HoverCollapseDelayMs);
            Assert.True(loadedSettings.StartPinned);
            Assert.Equal("#3B82F6", loadedSettings.AccentColorHex);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadSettings_WhenFileIsCorrupted_ReturnsDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"corrupted_settings_{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{ this is invalid json ::: }");

            var service = new JsonSettingsService(tempFile);
            var settings = await service.LoadSettingsAsync();

            Assert.NotNull(settings);
            Assert.Equal(240.0, settings.CollapsedWidth);
            Assert.Equal(140.0, settings.ExpandedHeight);
            Assert.Equal(NotchHorizontalPosition.Center, settings.HorizontalPosition);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
