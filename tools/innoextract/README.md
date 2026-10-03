# Local innoextract

`innoextract` reads installers that Inno Setup built **without running
them**, and that is the only way this repository gets at an RTP whose
payload sits in `Setup-1.bin`.

Measured on 2026-10-03: `RPGVXAce_RTP.zip` carries four entries and
nothing else --

```text
RTP100/Setup.exe       585 073   Inno Setup 5.4.2 installer
RTP100/Setup-1.bin   194 188 008   the runtime
RTP100/ReadMe.txt        5 261   the licence
```

**And a reader that only unzips gets an EXE and a BIN file and no
runtime at all.** `--list` on the extracted installer reports 783
entries, `--extract` writes 780 files and 193.3 MB in 9.9 s.

Why an external tool and not a reader in this repository: **writing an
Inno Setup reader means reimplementing its compression and its version
handling**, -- **and `AGENTS.md` forbids running an EXE out of a
game**, -- **and reading it with a tool that was written for it is
neither.**

Binary is ignored by Git. Metadata and hashes are recorded here:

| File | Size | SHA-256 |
|---|---|---|
| `innoextract-1.9-windows.zip` | 520 705 | `6989342c9b026a00a72a38f23b62a8e6a22cc5de69805cf47d68ac2fec993065` |
| `innoextract.exe` (daraus) | 1 218 560 | `946bd06d8b3722a791fea4d84f496139eef6faef61640924cb72f5f1998a68e1` |

Source of the release:
<https://github.com/dscharrer/innoextract/releases/tag/1.9>

Expected local path:

```text
tools/innoextract/bin/innoextract.exe
```

**And `innoextract.exe` is a build tool and not part of the shipped
game**, -- **and the launcher asks its user before it puts it
somewhere.**
