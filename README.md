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
- dotnet-8.0.0 or above

## Configuration

### Host

appsettings.json:

```json
{
    "Queue": 0
}
```

- Queue: A queue-number that reserved for netfilter-queue
    - See also: [nfq_create_queue](https://manpages.debian.org/testing/libnetfilter-queue-doc/nfq_create_queue.3.en.html)

### Proxy

appsettings.json:

```json
{
    "Port": 65501,
    "Hosts": [
        "10.0.0.128",
        "10.0.0.129",
        "10.0.0.130",
        ...
    ]
}
```

- Port: A port-number which frouros will use
- Hosts: Other node's ip-address
