package com.episense.fhirgenerator.repository;

import com.episense.fhirgenerator.entity.Hemograma;
import org.springframework.data.cassandra.repository.CassandraRepository;
import org.springframework.data.cassandra.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface HemogramaRepository extends CassandraRepository<Hemograma, UUID> {
    
    @Query("SELECT * FROM hemogramas WHERE sent_to_api = false ALLOW FILTERING")
    List<Hemograma> findNotSent();
    
    @Query("SELECT * FROM hemogramas WHERE patient_id = ?0 ALLOW FILTERING")
    List<Hemograma> findByPatientId(String patientId);
    
}
