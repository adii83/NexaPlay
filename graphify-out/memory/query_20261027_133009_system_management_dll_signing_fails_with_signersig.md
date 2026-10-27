---
type: "query"
date: "2026-10-27T13:30:09.644496+00:00"
question: "System.Management.dll signing fails with SignerSign 0x80093102 while other files succeed"
contributor: "graphify"
outcome: "useful"
source_nodes: ["sign-build.ps1"]
---

# Q: System.Management.dll signing fails with SignerSign 0x80093102 while other files succeed

## Answer

Published System.Management.dll had no embedded signature, but a temporary copy of exactly the same bytes signed successfully with the existing PFX, both without and with DigiCert timestamp; verification passed. The actual published DLL then signed and verified successfully, followed by full processing and validation of 253 eligible publish files. The initial failure is transient and was not reproducible; timestamp response/signing transient is plausible but unproven. Do not blame PFX password, certificate validity or binary corruption without evidence. sign-build.ps1 now supports bounded retry, skip of already-valid timestamped signatures, post-sign verification, OnlyFiles, MaxAttempts and ForceResign. Policy checks passed on Windows PowerShell 5.1 and PowerShell 7; Debug x64 build passed.

## Outcome

- Signal: useful

## Source Nodes

- sign-build.ps1