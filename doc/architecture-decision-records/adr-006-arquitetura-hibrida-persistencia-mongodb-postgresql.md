# ADR 006: Arquitetura Híbrida de Persistência com MongoDB e PostgreSQL

**Status:** Aceito

**Supersede:** ADR-002

**Contexto:**
Durante a evolução do projeto EpiSense, identificamos que diferentes tipos de dados possuem requisitos distintos de persistência. Os dados brutos FHIR precisam de flexibilidade de schema para suportar a variabilidade dos recursos FHIR, enquanto os dados analisados e resumos clínicos necessitam de estrutura relacional otimizada para consultas analíticas complexas e detecção de surtos epidemiológicos.

Esta decisão substitui a **ADR-002**, que propunha usar apenas MongoDB. A decisão evoluiu para uma abordagem de persistência polígota mais adequada às diferentes necessidades dos módulos do sistema.

**Decisão:**
Adotaremos uma **arquitetura híbrida de persistência** utilizando:

1. **MongoDB** para armazenamento de dados brutos FHIR (módulo de ingestão)
2. **PostgreSQL** com Entity Framework Core para dados analisados e resumos clínicos (módulo de análise)

O fluxo de dados será: `FHIR API → MongoDB (raw) → Analysis Service → PostgreSQL (structured)`

**Alternativas Consideradas:**

* **MongoDB Exclusivo:** Manter apenas MongoDB para toda a persistência. Foi descartado porque o banco de dados relacional possui melhor suporte a queries elaboradas para a detecção de surtos.

* **PostgreSQL Exclusivo com JSONB:** Usar apenas PostgreSQL. Foi descartado porque a flexibilidade de schema do MongoDB é superior para dados FHIR brutos que podem variar estruturalmente.

**Consequências:**

**Positivas:**

1. **Otimização por Caso de Uso:** MongoDB otimizado para ingestão flexível de FHIR, PostgreSQL otimizado para análises relacionais complexas.

2. **Performance de Análise:** Consultas SQL otimizadas para detecção de padrões epidemiológicos, índices GIN para campos JSONB de flags clínicas.

3. **Separação de Responsabilidades:** Clara separação entre dados de ingestão (transientes) e dados analíticos (permanentes para histórico epidemiológico).


**Negativas:**

1. **Complexidade Operacional:** Necessidade de gerenciar e monitorar dois sistemas de banco diferentes.

2. **Curva de Aprendizado:** Equipe precisa dominar duas tecnologias de banco diferentes.
