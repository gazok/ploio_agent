# PROTOCOL

## ROUTE

/log
/packet
/pod

## STRUCTS

- Packet

```json
{
    "{packet-id}": {
        "Timestamp":    "2023-11-05T07:39:20.485611199Z",
        "Source":       "{pod-id}",
        "Destination":  "{pod-id}",
        "Size":         "{packet size}",
        "Raw":          "{base64 packet data}"
    },
    ...
}
```

- Pod

```json
{
    "{pod-id}": {
        "Name":         "coredns-5dd5756b68-sjf7n",
        "Namespace":    "kube-system",
        "State":        "SANDBOX_READY",
        "CreatedAt":    "2023-11-05T07:39:20.485611199Z",
        "Network": [
            "10.244.0.10",
            ...
        ]
    },
    ...
}
```

\* A `pod-id` is unique even on different namespaces, 
and is constant on its all lifetime. 
`pod-id` can be used as primary unique-identifier for it.

- Log

```json
{
    "{log-id}": {
        "Code": "",
        "Message": "",
        "Refs": [
            {
                "Source": "Packet",
                "Identifier": "{packet-id}"
            },
            {
                "Source": "Pod",
                "Identifier": "{pod-uid}"
            },
            ...
        ]
    },
    ...
}
```
