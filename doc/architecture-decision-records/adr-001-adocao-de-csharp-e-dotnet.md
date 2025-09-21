# ADR 001: Adoção de C# e .NET para o Backend

**Status:** Proposta

**Contexto:**
O projeto EpiSense necessita de uma plataforma de backend robusta para implementar a recepção de mensagens FHIR, o processamento analítico dos dados e a exposição de uma API REST. A plataforma escolhida deve ser performática, segura e ter um ecossistema maduro para lidar com as demandas de um sistema de saúde em tempo real. A equipe de desenvolvimento possui experiência prévia com tecnologias Microsoft.

**Decisão:**
Adotaremos a linguagem **C#** e o framework **.NET** (versão 8 ou superior) para o desenvolvimento de todos os serviços de backend do EpiSense. Isso inclui o receptor de `subscriptions` FHIR, o componente de análise e a API REST.

**Alternativas Consideradas:**

* **Java/Kotlin com Spring Boot:** Uma opção muito popular e robusta, sugerida na documentação do projeto. Foi considerada, mas a proficiência e a produtividade da equipe atual são maiores no ecossistema .NET.
* **Node.js com TypeScript:** Considerado por sua alta performance em operações de I/O, o que seria benéfico para a recepção de `subscriptions`. Foi descartado por ser dinamicamente tipado (mesmo com TypeScript) e por a equipe considerar o .NET mais maduro para a gestão de processos complexos em background e análises computacionais.

**Consequências:**

* **Positivas:**
* **Alta Produtividade:** A equipe pode aproveitar a experiência existente para acelerar o desenvolvimento.
* **Performance:** O .NET moderno é altamente performático, adequado para processamento de alto volume.
* **Ecossistema Forte:** Amplo suporte para bibliotecas essenciais, como clientes FHIR (Firely SDK), frameworks de teste e ferramentas de segurança.
* **Segurança:** O framework possui recursos robustos para implementar os requisitos de segurança, como autenticação mTLS.

* **Negativas:**
* Nenhuma consequência negativa significativa foi identificada para o escopo deste projeto. O ecossistema .NET atende a todos os requisitos técnicos e de qualidade definidos.
