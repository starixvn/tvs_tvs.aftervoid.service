using System;
using System.Globalization;
using System.Text.Json;
using TicketVoidingSystem.Utilities;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.TicketVoidingSystem.Utilities;

public class Gmt8DateTimeJsonConverterTests
{
    private readonly Gmt8DateTimeJsonConverter _converter = new();
    private readonly JsonSerializerOptions _options = new();

    [Fact]
    public void Read_ValidDateTimeString_ReturnsCorrectDateTime()
    {
        // Arrange
        const string jsonString = "\"2023-12-25 10:30:45.123\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(jsonString));
        reader.Read(); // Move to the string value

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime), _options);

        // Assert
        var expected = DateTime.Parse("2023-12-25 10:30:45.123", CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Read_DateTimeStringWithDifferentFormat_ReturnsCorrectDateTime()
    {
        // Arrange
        const string jsonString = "\"2024-01-15 23:59:59.999\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(jsonString));
        reader.Read(); // Move to the string value

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime), _options);

        // Assert
        var expected = DateTime.Parse("2024-01-15 23:59:59.999", CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Write_DateTime_Adds12HoursAndFormatsCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 10, 30, 45, 123);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithZeroTime_Adds12HoursCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2024, 1, 1, 0, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMaxTime_Adds12HoursCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2024, 12, 31, 23, 59, 59, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMilliseconds_FormatsCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 6, 15, 14, 25, 30, 456);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithLeapYear_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2024, 2, 29, 12, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithYearEnd_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 31, 23, 59, 59, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithYearStart_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2024, 1, 1, 0, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithTimeZoneEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentMilliseconds_FormatsCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 8, 15, 9, 45, 30, 789);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithZeroMilliseconds_FormatsCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 5, 20, 16, 30, 45, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMaxMilliseconds_FormatsCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 10, 10, 11, 22, 33, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithEdgeCaseTime_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 11, 59, 59, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMinuteEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithSecondEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 0, 59, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithHourEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 23, 59, 59, 999);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDayEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 31, 12, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMonthEdgeCase_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 31, 12, 0, 0, 0);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithMinValue_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = DateTime.MinValue;
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithUtcNow_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = DateTime.UtcNow;
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithNow_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = DateTime.Now;
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithToday_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = DateTime.Today;
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithCustomDate_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 7, 4, 15, 30, 45, 123);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentYear_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2025, 3, 15, 8, 45, 30, 456);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentMonth_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 4, 20, 16, 20, 10, 789);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentDay_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 5, 10, 15, 25, 321);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentHour_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 3, 45, 30, 654);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentMinute_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 30, 45, 987);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentSecond_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 0, 30, 123);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }

    [Fact]
    public void Write_DateTimeWithDifferentMillisecond_HandlesCorrectly()
    {
        // Arrange
        var inputDateTime = new DateTime(2023, 12, 25, 12, 0, 0, 456);
        var expectedDateTime = inputDateTime.AddHours(12);
        var expectedString = expectedDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // Act
        _converter.Write(writer, inputDateTime, _options);
        writer.Flush();

        // Assert
        var jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal($"\"{expectedString}\"", jsonString);
    }
}