
# Getting Started : Deploy

This document describes how to deploy *Frouros* service with docker & systemd.
It assumes that you already know about docker and systemd and doesn't provide any description for these.

## 0. Requirements

### Compile Requirements

- docker
- docker-buildx (optional)
- dotnet 8.0.0 or above

### Runtime Requirements

- docker
- kubernetes or docker-compose (optional)

## 1. About *Frouros* System

*Frouros* is made up of these components:

- Host
- Proxy
- Module
- Central Server(hereinafter referred to as *Server*)

### Host

Host has responsibility of capturing packets by netfilter rule.
Then, Passing responsibility of doing verdict whether deny or accept packets
(hereinafter referred to as *verdict-responsibility*),
It allows Proxy to act as firewall.

### Proxy

Proxy constructs an abstract layer in firewall.
To resolve addresses (such as ip address) in packet,
Configured Proxies in intranet constructs ARP-like system virtually.
And, Passing *verdict-responsibility*,
It allows each Module can decide whether deny or accept captured packet.

### Module

Module is a simple component in firewall of proxy.

If a packet captured,
A *membrane* is constructed for the packet.
And, Passing through membrane, 
the packet will be denied or accepted by each module.

### Central Server

Central-Server is just used as log-server and user-setting container.

## 1. How to build

### Host

```shell
cd src/Frouros.Host
dotnet publish -c Release -r <target-platform> -o <output-directory>
```

- target-platform: A target RID. 
  - `linux-x64` for x86-64 linux, and `linux-x86` for i386 linux.
  - See also [https://learn.microsoft.com/en-us/dotnet/core/rid-catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
- output-directory: A directory where the publish output will be placed.

### Proxy

- Build docker image with `src/Dockerfile` in its directory
  - \<none\> image will be generated. It's because of [multi-stage build](https://docs.docker.com/build/building/multi-stage/), not a bug!
- Mount host machine's `/opt/frouros.d/` into container's `/opt/frouros.d`

### Central Server

Central-Server is not responsibility of frouros team.

See [central-server's manual](https://github.com/gazok/ploio_server)

## 2. How to configure

Configuration can be done with `appsettings.json` or environment variables etc.
See [https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0)

### Host

- `Queue` (uint16) : Netfilter queue index

### Proxy

- `Port` (uint16) : Port assigned for frouros
- `Hosts` (uri[]) : Neighbor hosts' URIs (can be IP or domain etc.)

## 3. How to run

### Host

Run executable in any way

### Proxy

Run docker image

### Module

Place `your-module.so` and `your-module.json` in `/opt/frouros.d/mods`.
Configuration file format is described below:

```json
{
    "GUID": "<your-guid>",
    "Name": "Your new module",
    "Description": "Description here"
}
```

## 4. How to maintain

Unfortunately, *Frouros* provides no way to health-check service currently.
