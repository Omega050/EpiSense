-- Script de inicialização do PostgreSQL para o módulo de análise
-- Este script é executado automaticamente quando o container é criado pela primeira vez

-- Criar extensões úteis
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Criar schema para o módulo de análise (opcional)
CREATE SCHEMA IF NOT EXISTS analysis;

-- Conceder permissões
GRANT ALL PRIVILEGES ON SCHEMA analysis TO episense;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA analysis TO episense;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA analysis TO episense;

-- Configurar search_path padrão
ALTER DATABASE episense_analysis SET search_path TO public, analysis;

-- Adicione aqui outras configurações iniciais se necessário
