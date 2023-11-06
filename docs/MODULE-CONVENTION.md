# CALLING CONVENTION

## Signature

```c
typedef int32_t (*getenv_t)(
    const char* key,
    int32_t offset,
    void* buf,
    int32_t size
);

typedef int32_t (*setenv_t)(
    const char* key,
    int32_t offset,
    const void* buf,
    int32_t size  
);

typedef int32_t (*retval_t)(
    int32_t code,
    const char* msg
);

void entrypoint(
    const char* ifname,

    const void* pkt,
    int64_t nbpkt,
    
    getenv_t getenv,
    setenv_t setenv,
    retval_t retval
);
```
