using System.Web;
using FellsideDigital.Web.Models;
using FellsideDigital.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Tests;

/// <summary>
/// Presigning is a purely local (offline) signing operation, so these need no S3/Docker.
/// They pin the behaviour that makes invoice/document downloads work on mobile: a download
/// URL must carry a signed Content-Disposition=attachment so iOS Safari saves the file
/// (it ignores the HTML `download` attribute cross-origin), while a plain view URL must not.
/// </summary>
public class S3StorageServiceTests
{
    private static S3StorageService CreateSut() =>
        new(Options.Create(new StorageSettings
        {
            ServiceUrl = "http://localhost:9000",
            AccessKey  = "test-access-key",
            SecretKey  = "test-secret-key",
            BucketName = "documents",
        }), NullLogger<S3StorageService>.Instance);

    [Fact]
    public async Task View_url_opens_inline_with_no_content_disposition()
    {
        var url = await CreateSut().GetPresignedUrlAsync("invoices/abc.pdf", TimeSpan.FromMinutes(5));

        Assert.DoesNotContain("response-content-disposition", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Download_url_forces_attachment_with_filename()
    {
        var url = await CreateSut().GetPresignedUrlAsync(
            "invoices/abc.pdf", TimeSpan.FromMinutes(5), downloadFileName: "March Invoice.pdf");

        var query = HttpUtility.ParseQueryString(new Uri(url).Query);
        var disposition = query["response-content-disposition"];

        Assert.NotNull(disposition);
        Assert.Contains("attachment", disposition);
        Assert.Contains("March Invoice.pdf", disposition);
        // The disposition is part of the signature, so it can't be tampered with.
        Assert.Contains("X-Amz-Signature", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Download_filename_is_sanitised_of_quotes()
    {
        var url = await CreateSut().GetPresignedUrlAsync(
            "invoices/abc.pdf", TimeSpan.FromMinutes(5), downloadFileName: "in\"vo\\ice.pdf");

        var disposition = HttpUtility.ParseQueryString(new Uri(url).Query)["response-content-disposition"];

        // The embedded quote and backslash are stripped, leaving a clean, header-safe filename.
        Assert.Equal("attachment; filename=\"invoice.pdf\"", disposition);
    }
}
