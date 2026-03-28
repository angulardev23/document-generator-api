using System.Text.Json;
using DocumentGenerator.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace DocumentGenerator.Application.Documents;

public sealed class GenerateDocumentCommandValidator(IOptions<DocumentGenerationOptions> options)
{
    public ValidatedGenerateDocumentCommand Validate(GenerateDocumentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<ValidationError>();

        ValidateTemplate(command, errors);
        var dataRoot = ValidateData(command.Data, errors);

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ValidatedGenerateDocumentCommand(
            command.TemplateFileName!,
            command.TemplateContent!,
            dataRoot!.Value);
    }

    private void ValidateTemplate(GenerateDocumentCommand command, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(command.TemplateFileName))
        {
            errors.Add(new ValidationError("template", "Template file is required."));
        }
        else if (!string.Equals(Path.GetExtension(command.TemplateFileName), ".docx", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError("template", "Only .docx template files are supported."));
        }

        if (command.TemplateContent is null)
        {
            errors.Add(new ValidationError("template", "Template file is required."));
            return;
        }

        if (command.TemplateContent.Length == 0)
        {
            errors.Add(new ValidationError("template", "Template file cannot be empty."));
        }

        if (command.TemplateContent.LongLength > options.Value.MaxUploadFileSizeBytes)
        {
            errors.Add(new ValidationError(
                "template",
                $"Template file exceeds the maximum allowed size of {options.Value.MaxUploadFileSizeBytes} bytes."));
        }
    }

    private static JsonElement? ValidateData(string? data, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            errors.Add(new ValidationError("data", "Data payload is required."));
            return null;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(data);

            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object) return jsonDocument.RootElement.Clone();
            errors.Add(new ValidationError("data", "Data payload must be a JSON object."));
            return null;

        }
        catch (JsonException)
        {
            errors.Add(new ValidationError("data", "Data payload must be valid JSON."));
            return null;
        }
    }
}
