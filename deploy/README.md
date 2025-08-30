# Deploy

Helm chart for the EpiSense API is under `chart/episense`.

- Package chart:
  helm package chart/episense

- Install (example):
  helm upgrade --install episense chart/episense \
    --set image.repository=ghcr.io/your-org/episense-api \
    --set image.tag=0.1.0

- Port-forward to test:
  kubectl port-forward deploy/RELEASE-NAME-episense 8080:8080
  curl http://localhost:8080/health
