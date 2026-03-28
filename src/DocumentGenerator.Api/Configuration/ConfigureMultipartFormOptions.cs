using DocumentGenerator.Application.Documents;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Api.Configuration;

public sealed class ConfigureMultipartFormOptions(IOptions<DocumentGenerationOptions> options)
    : IConfigureOptions<FormOptions>
{
    public void Configure(FormOptions optionsToConfigure)
    {
        var configuredLimit = options.Value.MaxUploadFileSizeBytes;
        var memoryThreshold = configuredLimit > int.MaxValue
            ? int.MaxValue
            : (int)configuredLimit;

        optionsToConfigure.MultipartBodyLengthLimit = configuredLimit;
        optionsToConfigure.MemoryBufferThreshold = memoryThreshold;
    }
}

