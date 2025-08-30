# ADR 002: Uso do MongoDB com Driver Nativo para Persistência

**Status:** Proposta

**Contexto:**
O projeto EpiSense precisa de um banco de dados para armazenar os hemogramas recebidos (recursos FHIR `Observation`) e os alertas gerados. O sistema necessita realizar análises complexas em janelas de tempo deslizantes para detectar padrões coletivos. A flexibilidade do esquema é desejável, já que o padrão FHIR é baseado em documentos JSON. A decisão precisava ser validada no Marco 3.

**Decisão:**
Adotaremos o **MongoDB** como nosso banco de dados principal. O acesso ao banco será feito exclusivamente através do **driver C# nativo oficial do MongoDB**. Não utilizaremos um ORM tradicional como o Entity Framework, aproveitando as funcionalidades de mapeamento objeto-documento (ODM) que o próprio driver oferece.

**Alternativas Consideradas:**
* **PostgreSQL com JSONB:** Uma alternativa forte, sugerida na documentação do projeto. Permite armazenamento de JSON e consultas estruturadas. Foi descartado porque a natureza do dado (documentos FHIR) se alinha de forma mais natural com o modelo do MongoDB, simplificando as operações de escrita e leitura dos recursos completos.
* **Entity Framework Core com Provedor para Cosmos DB:** Uma opção que manteria a consistência com uma ferramenta de ORM. Foi descartada por não ser a ferramenta ideal para bancos NoSQL que não sejam o Cosmos DB e por introduzir uma camada de abstração que poderia limitar o acesso a funcionalidades avançadas do MongoDB, como as Aggregation Pipelines, que são cruciais para as análises coletivas.

**Consequências:**
* **Positivas:**
    * **Alinhamento de Paradigma:** O modelo de documento do MongoDB é perfeito para armazenar recursos FHIR em JSON, eliminando a necessidade de mapeamento complexo (impedance mismatch).
    * **Flexibilidade:** Facilita a evolução do esquema de dados caso o perfil FHIR seja atualizado.
    * **Poder Analítico:** O uso do driver nativo nos dá acesso total ao Aggregation Framework do MongoDB, ideal para implementar as análises em janela deslizante de forma eficiente e performática.
    * **Produtividade Balanceada:** O driver permite usar LINQ para consultas simples (modo ODM) e o Aggregation Framework para as complexas, oferecendo um ótimo balanço entre produtividade e poder.

* **Negativas:**
    * **Menor Rigidez Transacional:** O modelo do MongoDB tem garantias transacionais diferentes de um banco SQL tradicional. Para o caso de uso do EpiSense, que consiste majoritariamente em escrita e leitura de dados analíticos, isso não é considerado um impeditivo.