# PROTOCOL

## ROUTE

|  route |  op   | desc                      |
|-------:|:-----:|:--------------------------|
|      / | `GET` | get log dump; see STRUCTS |

## STRUCTS

*\* All fields obsess network-byte-order*

### LOGDUMP

```
0        2        4 (octet)
+-----------------+
| signature       |
+-----------------+
| version         |
+-----------------+
| count of log    |
+-----------------+
| log line  ...   |
```

|        field | desc                                           |
|-------------:|:-----------------------------------------------|
|    signature | always "FROS"                                  |
|      version | semantic versioning; See https://semver.org/   |
| count of log | count of log (not in bytes; 1 means 24 octets) |
|     log line | log lines; see below                           |

### LOGLINE

```
0          2    3    4 (octet)   
+--------------------+
| Unix epoch (8 oct) |
+----------+----+----+
| L2       | L3 | Lx |
+----------+----+----+
| sip (16 octets)    |
+--------------------+
| dip (16 octets)    |
+----------+---------+
| sport    | dport   |
+----------+---------+
| size               |
+--------------------+
```

*\* uses exactly 52 octets*

|      field | desc                                                                                                       |
|-----------:|:-----------------------------------------------------------------------------------------------------------|
| unix epoch | unix timestamp (8 octets)                                                                                  |
|      proto | proto enum; see below                                                                                      |
|         L2 | address family; see "https://www.iana.org/assignments/address-family-numbers/address-family-numbers.xhtml" |
|         L3 | L3 protocol number (typically, ip protocol number)                                                         |
|         Lx | additional protocol number; see below                                                                      |
|      sport | source port                                                                                                |
|      dport | destination port                                                                                           |
|        sip | source ip (16 octets; use 4 octets )                                                                       |
|        dip | destination ip (16 octets)                                                                                 |
|       size | size of captured packet                                                                                    |

### Additional Protocol Number

| num | desc |
|----:|:-----|
|  01 | QUIC |
|  02 | HTTP |
| ... | ...  |
|  FF | None |
