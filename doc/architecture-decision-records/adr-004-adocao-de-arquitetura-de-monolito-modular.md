# ADR 004: Adoção de Arquitetura de Monólito Modular

**Status:** Aceito

**Contexto:**
A separação lógica dos domínios (`Análise`, `Alertas`) é necessária, porém operar múltiplos microsserviços agora adicionaria complexidade desproporcional (infra, observabilidade, rede) para a fase atual.

**Decisão:**
Adotar **Monólito Modular**: um único processo .NET, estruturado em módulos com fronteiras claras. Comunicação interna via mediador/barramento in-memory (ver ADR 003). Cada módulo é dono lógico de sua persistência.

**Alternativas Consideradas:**

- Microsserviços Distribuídos: descartado (overhead operacional precoce).
- Monólito Tradicional (sem fronteiras): descartado (risco de acoplamento crescente e perda de manutenibilidade).

**Consequências:**

- Positivas:
  - Simplicidade operacional (deploy, observabilidade, scaling únicos).
  - Base preparada para futura extração seletiva.
  - Comunicação interna de baixa latência.
- Negativas:
  - Escala horizontal não granular (todos juntos).
  - Stack tecnológica unificada (.NET) para todos os módulos no início.