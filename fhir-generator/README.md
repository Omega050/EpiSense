# FHIR Generator - Automated Hemograma Generator

Gerador automatizado de hemogramas no formato FHIR para o projeto EpiSense.

## 🐳 Quick Start com Docker

**Forma mais fácil de rodar:**

```bash
# Da raiz do projeto EpiSense
docker-compose up -d

# Ver logs
docker-compose logs -f fhir-generator

# Verificar health
curl http://localhost:8080/actuator/health
```

📖 **Mais detalhes**: Veja [DOCKER-QUICKSTART.md](./DOCKER-QUICKSTART.md)

---

## 🎯 Funcionalidade

Este serviço **automaticamente**:
- Gera entre **20 e 100 hemogramas aleatórios** a cada execução
- Executa a cada **3 a 10 minutos** (intervalo aleatório)
- **Armazena todos os dados no ScyllaDB** antes de enviar
- **Envia para API externa** do EpiSense
- **Registra status de envio** e mantém histórico completo

## 🛠 Tecnologias

- **Java 25**
- **Spring Boot 3.4.0**
- **Maven**
- **HAPI FHIR R4 7.6.0**
- **ScyllaDB / Cassandra** (Spring Data Cassandra)
- **Spring Scheduler** (tarefas agendadas)
- **RestTemplate** (comunicação HTTP)

## 📋 Pré-requisitos

- JDK 25 instalado
- Maven 3.8+ instalado
- **ScyllaDB ou Cassandra** em execução na porta 9042
- API externa do EpiSense rodando (configurável)

## 🚀 Configuração

### 1. ScyllaDB Setup

#### Opção A: Docker (Recomendado)
```powershell
docker run -d --name scylla -p 9042:9042 scylladb/scylla
```

#### Opção B: Docker Compose
Adicione ao `docker-compose.yml`:
```yaml
scylla:
  image: scylladb/scylla
  ports:
    - "9042:9042"
  volumes:
    - scylla-data:/var/lib/scylla
```

#### Criar Schema
```powershell
# Copie o schema
docker cp src/main/resources/schema.cql scylla:/tmp/

# Execute
docker exec -it scylla cqlsh -f /tmp/schema.cql
```

### 2. Configurar API Externa

Edite `application.properties` ou `application.yml`:

```yaml
external:
  api:
    url: http://localhost:5000/api/ingestion/fhir  # URL da API do EpiSense
    timeout: 30000
```

### 3. Ajustar Parâmetros do Scheduler (Opcional)

```yaml
scheduler:
  enabled: true
  min-interval-minutes: 3    # Intervalo mínimo entre execuções
  max-interval-minutes: 10   # Intervalo máximo entre execuções
  min-batch-size: 20         # Mínimo de hemogramas por batch
  max-batch-size: 100        # Máximo de hemogramas por batch
```

## ▶️ Como executar

### Usando Maven
```powershell
mvn spring-boot:run
```

### Build e executar JAR
```powershell
mvn clean package
java -jar target/fhir-generator-0.0.1-SNAPSHOT.jar
```

## 📊 Endpoints Disponíveis

### Health Check
```http
GET http://localhost:8080/api/v1/hemograma/health
```

### Estatísticas
```http
GET http://localhost:8080/api/v1/hemograma/stats
```
Retorna:
```json
{
  "total": 450,
  "sent": 430,
  "pending": 20
}
```

### Buscar por Paciente
```http
GET http://localhost:8080/api/v1/hemograma/patient/{patientId}
```

### Actuator (Métricas)
```http
GET http://localhost:8080/actuator/health
GET http://localhost:8080/actuator/metrics
```

## 🏗 Arquitetura

```
┌─────────────────────────────────────────────────────┐
│         HemogramaGeneratorScheduler                 │
│  (Executa a cada 3-10 min aleatoriamente)          │
└──────────────┬──────────────────────────────────────┘
               │
               ▼
┌──────────────────────────┐
│   HemogramaService       │
│  - Gera 20-100 items     │
│  - Valores aleatórios    │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│   ScyllaDB (Cassandra)   │
│  - Armazena TUDO         │
│  - Status de envio       │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│   ExternalApiService     │
│  - Envia para API        │
│  - Atualiza status       │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│   API Externa EpiSense   │
│  POST /api/ingestion/fhir│
└──────────────────────────┘
```

## 📁 Estrutura do Projeto

```
fhir-generator/
├── src/main/java/com/episense/fhirgenerator/
│   ├── config/
│   │   ├── CassandraConfig.java
│   │   ├── FhirConfig.java
│   │   ├── RestTemplateConfig.java
│   │   └── SchedulerConfig.java
│   ├── entity/
│   │   └── Hemograma.java
│   ├── repository/
│   │   └── HemogramaRepository.java
│   ├── service/
│   │   ├── HemogramaService.java
│   │   └── ExternalApiService.java
│   ├── scheduler/
│   │   └── HemogramaGeneratorScheduler.java
│   ├── controller/
│   │   └── HemogramaController.java
│   └── FhirGeneratorApplication.java
└── src/main/resources/
    ├── application.yml
    └── schema.cql
```

## 🔄 Fluxo de Funcionamento

1. **Scheduler dispara** (intervalo aleatório 3-10 min)
2. **Gera quantidade aleatória** de hemogramas (20-100)
3. **Salva no ScyllaDB** com `sent_to_api = false`
4. **Envia para API externa** um por um
5. **Atualiza status** no ScyllaDB (`sent_to_api = true`, timestamp, status HTTP)
6. **Retry automático** a cada 5 minutos para hemogramas não enviados

## 🧪 Testando

### Verificar se está gerando
```powershell
# Ver logs
mvn spring-boot:run

# Verificar stats
Invoke-RestMethod http://localhost:8080/api/v1/hemograma/stats
```

### Consultar ScyllaDB
```bash
docker exec -it scylla cqlsh

USE episense;
SELECT COUNT(*) FROM hemogramas;
SELECT * FROM hemogramas LIMIT 10;
```

## 📝 Logs Importantes

O sistema loga:
- ✅ Início de cada batch
- ✅ Quantidade de hemogramas gerados
- ✅ Status de salvamento no ScyllaDB
- ✅ Status de envio para API externa
- ✅ Próximo intervalo de execução
- ❌ Erros e retentativas

## 🔧 Troubleshooting

### ScyllaDB não conecta
```yaml
# Ajuste no application.yml
spring:
  cassandra:
    contact-points: localhost  # ou IP do container
    port: 9042
    local-datacenter: datacenter1
```

### API externa não responde
- Verifique se a URL está correta
- O sistema tentará reenviar automaticamente
- Hemogramas ficam salvos no banco

### Desabilitar scheduler temporariamente
```yaml
scheduler:
  enabled: false
```

## 📈 Próximos Passos

- [ ] Dashboard de monitoramento
- [ ] Configuração de profiles (dev, prod)
- [ ] Métricas avançadas com Micrometer
- [ ] Autenticação na API externa
- [ ] Rate limiting no envio
