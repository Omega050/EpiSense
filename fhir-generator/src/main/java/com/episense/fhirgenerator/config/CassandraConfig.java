package com.episense.fhirgenerator.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.data.cassandra.config.EnableCassandraAuditing;
import org.springframework.data.cassandra.repository.config.EnableCassandraRepositories;

@Configuration
@EnableCassandraRepositories(basePackages = "com.episense.fhirgenerator.repository")
@EnableCassandraAuditing
public class CassandraConfig {
    
}
