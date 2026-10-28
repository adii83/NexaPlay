# Online-Fix password fixtures

These archives are generated locally, contain only `42/payload.txt` with the UTF-8 bytes
`Online-Fix password fixture.\n`, and are never executed. They are copied into temporary test
directories and used with fake HTTP and Steam services. No real Drive package or game is used.

| Fixture | Encryption | Test password |
| --- | --- | --- |
| plain.rar | None | None |
| default.rar | RAR5 file data | online-fix.me |
| default-headers.rar | RAR5 headers and file data | online-fix.me |
| custom-headers.rar | RAR5 headers and file data | adi83geel |
| default-aes.zip | ZIP AES-256 | online-fix.me |
| custom-headers.7z | 7z AES, encrypted headers | adi83geel |

`generate.py` regenerates the fixtures using installed WinRAR and 7-Zip. Those tools are only
needed for regeneration; the application and regression tests use SharpCompress and require neither.
