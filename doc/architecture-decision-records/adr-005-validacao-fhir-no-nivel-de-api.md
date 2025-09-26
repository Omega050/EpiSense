# ADR 005: Validação FHIR no Nível de API

**Status:** Aceito

**Contexto:**
Com a implementação de dois endpoints de ingestão (`/api/ingestion/test` e `/api/ingestion/observation`), surgiu a necessidade de decidir onde e como realizar a validação dos dados FHIR. As opções eram validar no nível da API (síncronamente) ou postergar a validação para o processamento assíncrono no domínio.

**Decisão:**
Implementar **validação FHIR no nível de API** através de métodos de extensão (`FhirExtensions.IsValid()`), fornecendo feedback imediato ao cliente sobre a validade estrutural dos dados antes da persistência.

**Alternativas Consideradas:**

* **Validação Assíncrona no Domínio:** Aceitar todos os dados na API e validar durante o processamento posterior via `IngestionService`. Foi descartada porque não oferece feedback imediato ao cliente e pode resultar em dados inválidos persistidos temporariamente.
* **Validação com Biblioteca FHIR Externa (Firely SDK):** Usar uma biblioteca completa de validação FHIR. Foi descartada para o MVP devido à complexidade adicional e overhead, priorizando validação customizada focada nos campos essenciais para o caso de uso.
* **Sem Validação:** Confiar inteiramente nos sistemas externos. Foi descartada por aumentar o risco de corrupção de dados e dificultar o debugging.

**Consequências:**

* **Positivas:**
  - **Feedback Imediato:** Clientes recebem erros de validação instantaneamente, melhorando a experiência de integração.
  - **Dados Limpos:** Apenas dados válidos são persistidos no MongoDB, mantendo a qualidade da base.
  - **Debugging Facilitado:** Erros de estrutura são identificados na origem, não durante processamento posterior.
  - **Flexibilidade:** Validação customizada permite focar nos campos críticos para o EpiSense sem overhead desnecessário.

* **Negativas:**
  - **Acoplamento API-Domínio:** Lógica de validação FHIR fica na camada de API, não no domínio puro.
  - **Duplicação Potencial:** Se validação mais robusta for necessária no domínio, pode haver sobreposição de regras.
  - **Manutenção:** Mudanças no perfil FHIR requerem atualização manual dos métodos de validação.

**Implementação:**
A validação é realizada através do método de extensão `FhirObservationRequest.IsValid()`, que verifica:
- `ResourceType` deve ser "Observation"
- `Id` é obrigatório
- `Status` é obrigatório  
- `Subject.Reference` é obrigatório
- `Code.Coding` deve ter pelo menos um item

Dados válidos recebem status `IngestionStatus.Validated`, enquanto dados inválidos retornam HTTP 400 com detalhes dos erros.