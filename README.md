# Document Generator API

Minimal ASP.NET Core microservice for generating `.docx` files fully in memory with `DocxTemplater`.

Repository-managed example templates live in `templates/`.

## Projects

- `DocumentGenerator.Api`: HTTP endpoint, Swagger, exception handling, and dependency injection.
- `DocumentGenerator.Application`: use case orchestration, validation, options, and application exceptions.
- `DocumentGenerator.Domain`: core contracts and abstractions.
- `DocumentGenerator.Infrastructure`: `DocxTemplater` integration and JSON-to-template-model conversion.
- `DocumentGenerator.Tests`: unit and integration tests.

## Endpoint

`POST /api/documents/generate`

`POST /api/documents/investment-contract`

- Content type: `multipart/form-data`
- Form field `template`: required `.docx` template upload
- Form field `data`: required JSON object string
- Investment contract endpoint content type: `application/json`
- Investment contract endpoint body: raw JSON object

Examples:

- `{{title}}`
- `{{customer.name}}`
- `{{#items}}{{.name}}{{/items}}`

Example repository template:

- `templates/InvestmentContract.docx`

## Configuration

`src/DocumentGenerator.Api/appsettings.json`

```json
{
  "DocumentGeneration": {
    "MaxUploadFileSizeBytes": 5242880,
    "OutputFilenamePrefix": "generated-document"
  }
}
```

Environment variable examples:

- `DocumentGeneration__MaxUploadFileSizeBytes=10485760`
- `DocumentGeneration__OutputFilenamePrefix=contracts`

## Local run

```bash
dotnet restore DocumentGenerator.slnx
dotnet build DocumentGenerator.slnx
dotnet run --project src/DocumentGenerator.Api
```

Swagger is available in development at `/swagger`.

## Tests

```bash
dotnet test DocumentGenerator.slnx
```

## Docker

```bash
docker build -t document-generator-api .
docker run --rm -p 8080:8080 document-generator-api
```

## Curl example

```bash
curl -X POST "http://localhost:5180/api/documents/generate" \
  -F "template=@./template.docx" \
  -F 'data={"title":"Hello","customer":{"name":"Jane"},"items":[{"name":"Item A"}]}'
```

```bash
curl -X POST "http://localhost:5180/api/documents/investment-contract" \
  -H "Content-Type: application/json" \
  -d '{"ContractDate":"2026-03-30","FirstName":"Carlitos","LastName":"Escalante","CompanyName":"Example Ventures","InvestmentAmount":"100000 USD","EquityPercentage":"10%"}' \
  -o investment-contract.docx
```
