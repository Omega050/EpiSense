# 🐳 Docker Setup para EpiSense

Este arquivo docker-compose configura o ambiente de desenvolvimento local com MongoDB.

## 🚀 Como Usar

### 1. Iniciar os Serviços
```bash
# Na raiz do projeto
docker-compose up -d
```

### 2. Verificar se está Funcionando
```bash
# Verificar containers rodando
docker-compose ps

# Ver logs do MongoDB
docker-compose logs mongodb
```

### 3. Parar os Serviços
```bash
docker-compose down
```

### 4. Parar e Limpar Dados
```bash
# ⚠️ ATENÇÃO: Isto apaga todos os dados!
docker-compose down -v
```

## 📊 Serviços Disponíveis

### MongoDB
- **URL**: `mongodb://localhost:27017`
- **Database**: `episense_dev`
- **Usuário**: `admin`
- **Senha**: `password`

### Mongo Express (Web UI)
- **URL**: http://localhost:8081
- **Interface web para visualizar dados do MongoDB**

## 🔍 Testando Conexão

Após iniciar o docker-compose, você pode testar a conexão:

```bash
# Via MongoDB CLI (se instalado)
mongosh "mongodb://admin:password@localhost:27017/episense_dev"

# Via aplicação .NET
cd backend/src/Apps/EpiSense.Api
dotnet run
```

## 📁 Estrutura do Banco

O script de inicialização cria:

### Collections:
- `raw_health_data`: Dados brutos de ingestão
- `analytics_aggregates`: Dados agregados para análise
- `alerts`: Alertas gerados pelo sistema

### Índices:
- Status de ingestão
- Data de recebimento
- Data de criação
- ID FHIR (sparse index)

## 🛠️ Solução de Problemas

### Erro "port already in use"
```bash
# Ver quais processos estão usando as portas
netstat -an | findstr :27017
netstat -an | findstr :8081

# Parar containers existentes
docker-compose down
```

### Erro de conexão
```bash
# Verificar se o container está rodando
docker ps | findstr mongo

# Verificar logs de erro
docker-compose logs
```

### Reset completo
```bash
# Parar tudo e limpar
docker-compose down -v
docker system prune -f

# Iniciar novamente
docker-compose up -d
```