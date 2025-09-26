# Exemplos de Uso da API de Ingestão Atualizada

A API de ingestão do EpiSense agora suporta dois formatos de entrada:

## 1. Formato Anterior (String JSON) - Endpoint `/api/ingestion/test`

```json
{
  "fhirJson": "{\"resourceType\":\"Observation\",\"id\":\"hemograma-001\",\"status\":\"final\",...}"
}
```

## 2. Novo Formato (Objeto FHIR) - Endpoint `/api/ingestion/observation`

```json
{
  "resourceType": "Observation",
  "id": "hemograma-test-001",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory",
          "display": "Laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "58410-2",
        "display": "CBC panel - Blood by Automated count"
      }
    ],
    "text": "Complete Blood Count"
  },
  "subject": {
    "reference": "Patient/patient-001",
    "display": "João da Silva"
  },
  "effectiveDateTime": "2025-09-24T08:30:00Z",
  "issued": "2025-09-24T10:15:00Z",
  "performer": [
    {
      "reference": "Organization/lab-central-sp",
      "display": "Laboratório Central SP"
    }
  ],
  "component": [
    {
      "code": {
        "coding": [
          {
            "system": "http://loinc.org",
            "code": "6690-2",
            "display": "Leukocytes [#/volume] in Blood by Automated count"
          }
        ]
      },
      "valueQuantity": {
        "value": 12500,
        "unit": "cells/μL",
        "system": "http://unitsofmeasure.org",
        "code": "/uL"
      },
      "interpretation": [
        {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
              "code": "H",
              "display": "High"
            }
          ]
        }
      ],
      "referenceRange": [
        {
          "low": {
            "value": 4000,
            "unit": "cells/μL"
          },
          "high": {
            "value": 11000,
            "unit": "cells/μL"
          }
        }
      ]
    },
    {
      "code": {
        "coding": [
          {
            "system": "http://loinc.org",
            "code": "777-3",
            "display": "Platelets [#/volume] in Blood by Automated count"
          }
        ]
      },
      "valueQuantity": {
        "value": 95000,
        "unit": "platelets/μL",
        "system": "http://unitsofmeasure.org",
        "code": "/uL"
      },
      "interpretation": [
        {
          "coding": [
            {
              "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
              "code": "L",
              "display": "Low"
            }
          ]
        }
      ],
      "referenceRange": [
        {
          "low": {
            "value": 150000,
            "unit": "platelets/μL"
          },
          "high": {
            "value": 450000,
            "unit": "platelets/μL"
          }
        }
      ]
    }
  ]
}
```

## Vantagens do Novo Formato:

1. **Validação Automática**: O endpoint valida os campos obrigatórios automaticamente
2. **Tipo Seguro**: IntelliSense e verificação de tipos durante o desenvolvimento
3. **Melhor Documentação**: Swagger gera automaticamente a documentação da estrutura
4. **Parsing Mais Eficiente**: Não precisa fazer parsing de string para JSON internamente
5. **Tratamento de Erros Melhorado**: Retorna mensagens específicas de validação

## Campos Obrigatórios Validados:

- `resourceType`: Deve ser "Observation"
- `id`: Identificador único da observação
- `status`: Status da observação (ex: "final", "preliminary")
- `subject.reference`: Referência ao paciente
- `code.coding`: Pelo menos um código definindo o tipo de observação

## Como Testar:

### Via cURL:
```bash
curl -X POST "https://localhost:7080/api/ingestion/observation" \
  -H "Content-Type: application/json" \
  -d @hemograma-example.json
```

### Via PowerShell:
```powershell
$body = Get-Content "hemograma-example.json" -Raw
Invoke-RestMethod -Uri "https://localhost:7080/api/ingestion/observation" -Method Post -Body $body -ContentType "application/json"
```