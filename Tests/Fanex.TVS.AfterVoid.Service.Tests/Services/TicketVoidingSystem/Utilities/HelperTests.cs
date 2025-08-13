using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Utilities;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Utilities;

public class TicketVoidingSystemHelperTests : IDisposable
{
    public TicketVoidingSystemHelperTests()
    {
        // Setup test data
        var testServers = new List<SiteDbGroupModel>
        {
            new() { Id = 0, ServerName = "DBCTG" },
            new() { Id = 1, ServerName = "DBSITE01" },
            new() { Id = 2, ServerName = "DBSITE02" },
            new() { Id = 3, ServerName = "DBSITE03" }
        };
        SiteDbGroupConstant.SetServerList(testServers);
    }

    public void Dispose()
    {
        // Cleanup
        SiteDbGroupConstant.SetServerList([]);
    }

    [Theory]
    [InlineData(0, "TestSp", "DBCTG.TestSp")]
    [InlineData(1, "GetUserInfo", "DBSITE01.GetUserInfo")]
    [InlineData(2, "UpdateTicket", "DBSITE02.UpdateTicket")]
    [InlineData(3, "DeleteRecord", "DBSITE03.DeleteRecord")]
    public void GetSettingKeyBySiteDbGroupId_ValidInput_ReturnsCorrectKey(int siteDbGroupId, string spName, string expected)
    {
        // Act
        var result = Helper.GetSettingKeyBySiteDbGroupId(siteDbGroupId, spName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "")]
    [InlineData(2, "   ")]
    public void GetSettingKeyBySiteDbGroupId_ValidSiteDbGroupIdWithNullOrEmptySpName_ReturnsCorrectKey(int siteDbGroupId, string spName)
    {
        // Act
        var result = Helper.GetSettingKeyBySiteDbGroupId(siteDbGroupId, spName ?? "");

        // Assert
        var expectedServerName = siteDbGroupId switch
        {
            0 => "DBCTG",
            1 => "DBSITE01",
            2 => "DBSITE02",
            _ => ""
        };
        var expected = $"{expectedServerName}.{spName ?? ""}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateRequestStringContent_ValidObject_ReturnsCorrectStringContent()
    {
        // Arrange
        var testData = new { Name = "Test", Age = 25, IsActive = true };

        // Act
        var result = Helper.CreateRequestStringContent(testData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/json", result.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", result.Headers.ContentType?.CharSet);

        var content = result.ReadAsStringAsync().Result;
        var deserialized = JsonSerializer.Deserialize<dynamic>(content);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void CreateRequestStringContent_NullObject_ReturnsCorrectStringContent()
    {
        // Act
        var result = Helper.CreateRequestStringContent<object>(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/json", result.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", result.Headers.ContentType?.CharSet);

        var content = result.ReadAsStringAsync().Result;
        Assert.Equal("null", content);
    }

    [Fact]
    public void CreateRequestStringContent_EmptyObject_ReturnsCorrectStringContent()
    {
        // Arrange
        var testData = new { };

        // Act
        var result = Helper.CreateRequestStringContent(testData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/json", result.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", result.Headers.ContentType?.CharSet);

        var content = result.ReadAsStringAsync().Result;
        Assert.Equal("{}", content);
    }

    [Fact]
    public async Task DeserializeResponseStream_ValidJsonResponse_ReturnsDeserializedObject()
    {
        // Arrange
        var testData = new { Name = "Test", Age = 25, IsActive = true };
        var jsonContent = JsonSerializer.Serialize(testData);
        var response = new HttpResponseMessage
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        var result = await Helper.DeserializeResponseStream<dynamic>(response);

        // Assert
        Assert.NotNull(result);
        var resultJson = JsonSerializer.Serialize(result);
        Assert.Contains("Test", resultJson);
        Assert.Contains("25", resultJson);
        Assert.Contains("true", resultJson);
    }

    [Fact]
    public async Task DeserializeResponseStream_NullJsonResponse_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        var result = await Helper.DeserializeResponseStream<object>(response);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeserializeResponseStream_ComplexObject_ReturnsDeserializedObject()
    {
        // Arrange
        var testData = new
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedDate = DateTime.Now,
            Tags = new[] { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 },
                { "key3", true }
            }
        };
        var jsonContent = JsonSerializer.Serialize(testData);
        var response = new HttpResponseMessage
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        var result = await Helper.DeserializeResponseStream<dynamic>(response);

        // Assert
        Assert.NotNull(result);
        var resultJson = JsonSerializer.Serialize(result);
        Assert.Contains("Test User", resultJson);
        Assert.Contains("test@example.com", resultJson);
        Assert.Contains("tag1", resultJson);
        Assert.Contains("tag2", resultJson);
        Assert.Contains("tag3", resultJson);
    }

    [Fact]
    public async Task DeserializeResponseStream_PropertyNameCaseInsensitive_WorksCorrectly()
    {
        // Arrange
        var testData = new { UserName = "Test", UserAge = 25 };
        var jsonContent = JsonSerializer.Serialize(testData);
        var response = new HttpResponseMessage
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        var result = await Helper.DeserializeResponseStream<dynamic>(response);

        // Assert
        Assert.NotNull(result);
        var resultJson = JsonSerializer.Serialize(result);
        Assert.Contains("Test", resultJson);
        Assert.Contains("25", resultJson);
    }

    [Fact]
    public async Task DeserializeResponseStream_InvalidJson_ThrowsException()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json")
        };

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            Helper.DeserializeResponseStream<object>(response));
    }

    [Fact]
    public async Task DeserializeResponseStream_NullResponse_ThrowsException()
    {
        // Arrange
        HttpResponseMessage response = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            Helper.DeserializeResponseStream<object>(response));
    }
}