# Frouros

## Runtime Requirement

- systemd
- kubernetes
  - A host application must be executed as systemd service on physical node
  - A proxy application must be executed as pod on k8s (like daemon-set)
- docker

## Compile-time Requirement

- docker
- docker buildx
- dotnet-8.0-rc-1.x or above
