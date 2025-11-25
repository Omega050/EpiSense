-- Migration to add missing columns to hemogramas table
ALTER TABLE fhir_generator.hemogramas ADD city text;
ALTER TABLE fhir_generator.hemogramas ADD neutrophils_band_form double;
