# CALLING CONVENTION

## Signature

```c
/*
 * =================================== NOTICE ===================================
 * - all types follow ILP32 and LP64
 * - struct must be packed with 4B
 * - magic number after INET packet is same as an ip-protocol-number for payload
 * ==============================================================================
 */

#define PR0_RAW   0
#define PR0_INET  1
#define PR0_INET6 2

typedef struct __pktreg pktreg_t;

__attribute__((aligned(4)))
struct __pktreg {
  uint64_t  magic;
  size_t    size;
  uint8_t*  payload;
  pktreg_t* next;
};

typedef void (*setres_t)(uint16_t code, const char* msg);

void initialize(
  setres_t setres
);

void entrypoint(
  uint32_t id, 
  struct timeval tv,
  const char* src, 
  const char* dst,  
  pktreg_t* pkt
);
```

## Code

- 00000-09999 : **Trace**
    - Trace symbol/mark
- 10000-19999 : **Debug**
    - Debug symbol/mark
- 20000-29999 : **Information**
    - Everything is okay
- 30000-39999 : **Warning**
    - Partial service may be dead or attacked; or mannual health-check is recommended
- 40000-49999 : **Fail**
    - Partial service was dead or attacked; or should be checked mannually
- 50000-59999 : **Critical**
    - Service was already dead or should be terminated or must be checked mannually

\* 0, 10000, 20000, 30000, 40000, 50000 is reserved; should not be used in module-code.

<br/>

- `SEVERITY = CODE / 10000`
- `ID = CODE % 10000`
