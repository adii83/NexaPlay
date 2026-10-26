# Add Game: sumber API dan ZIP final

Keputusan 2026-10-26: pemasangan Lua tidak lagi mengomentari `setManifestid(...)`,
mengubah newline, atau menghapus BOM. Byte Lua dari ZIP final disalin apa adanya.

## Urutan sumber

`data/api.json` mencoba sumber aktif secara berurutan:

1. NexaPlay R2
2. ProjectLightningManifests
3. SPIN0ZAi
4. sojogamesdatabase1
5. sojogames2
6. ManifestHub
7. GameHub
8. Ryuu
9. Sadie
10. TwentyTwo Cloud
11. Sushi

`success_code` menerima angka (misalnya `200`) atau string `"2xx"` (200–299).
HTTP gagal, arsip rusak, atau hasil tanpa Lua numerik melanjutkan sumber berikutnya.
Pembatalan pengguna menghentikan seluruh rantai. Kesalahan menulis ke Steam
ditampilkan sebagai kegagalan pemasangan, bukan kegagalan sumber API.

Pemilihan konfigurasi tetap: `%LOCALAPPDATA%\NexaPlay\api.json` jika tersedia,
lalu `data/api.json` di sebelah executable, lalu `public/data/api.json`.
Konfigurasi lokal eksplisit harus diperbarui sendiri jika ingin memakai daftar baru.

## Persiapan khusus lima sumber GitHub

Hanya entri dengan `lua_processing` yang menjalani persiapan Lightning.
Entri R2 dan sumber lama sudah menyediakan payload siap dipasang.
Logika dipindahkan ke C# dalam `AddGamePayloadBuilder`; tidak memerlukan Python
dan tidak menjalankan isi arsip.

Mode `auto` memilih `spin0zai` jika URL mengandung `SPIN0ZAi`, selain itu
`standard`. Mode eksplisit `standard` dan `spin0zai` juga tersedia.

Aturan mengikuti skrip Python Lightning 5.0.11 yang diberikan pengguna,
dengan `include_manifests=True`:

- Ekstrak ke direktori sementara privat; abaikan `__MACOSX/`.
- Telusuri subfolder menggunakan urutan native filesystem Windows.
- Ambil semua `.lua` dan `.manifest`, lalu ratakan berdasarkan nama file.
  Nama yang bertabrakan ditimpa mengikuti urutan tersebut.
- Mode standar mempertahankan semua baris Lua, termasuk `setManifestid` aktif.
- Mode SPIN0ZAi hanya mempertahankan baris yang setelah trim awal dimulai
  dengan `addappid(`, `adddlc(`, atau `setManifestid(`, sesuai kapitalisasi itu.
- Kedua mode membaca UTF-8 dengan replacement untuk byte invalid, memakai LF,
  lalu menambahkan baris kosong dan footer `-- Made with love by NexaPlay ID 🖤🤍`
  sesuai revisi pengguna. Ini menggantikan footer Lightning pada skrip asli.
- Manifest tidak diubah. File lain dibuang. ZIP final bernama `<appid>.zip`
  dan hanya berisi file pada root, tanpa folder pembungkus GitHub.
- Batas mengikuti lampiran: unduhan 128 MiB, per anggota 64 MiB,
  total ekstraksi/pemrosesan 256 MiB, maksimum 10.000 anggota.
  Path traversal, nama Windows tidak valid, symlink, ZIP terenkripsi,
  dan kompresi selain stored/deflate ditolak.

## Pemasangan

Setelah ZIP final tervalidasi, semua manifest disalin ke `Steam\depotcache`.
Lua yang dipilih adalah `<appid>.lua`, atau Lua numerik pertama jika tidak ada.
Lua disalin ke `Steam\config\stplug-in\<appid>.lua` tanpa pengeditan isi.
Penulisan setiap file memakai file sementara lalu rename agar file lama tidak
rusak akibat penyalinan yang dibatalkan. ZIP sumber, ZIP final, dan staging
dibersihkan setelah selesai/gagal/batal, mengikuti lifecycle Add Game sebelumnya.

## Verifikasi

`dotnet run --project NexaPlay.SelfCheck -- --add-game-only` menguji urutan API,
status `2xx`, perataan ZIP, filter kedua mode, manifest tanpa perubahan,
Lua tanpa perubahan saat pemasangan (termasuk BOM/newline), benturan nama,
penolakan path tidak aman, fallback sumber, dan pembatalan.

Verifikasi tambahan 2026-10-26 membandingkan keluaran C# dengan skrip Python
lampiran pada 16 payload: nested folders, duplicate/case-collision, whitespace,
UTF-8 invalid, Unicode Path ZIP, ZIP64, dan direktori DOS. Nama dan setiap byte
file hasil cocok sebelum revisi branding footer. Keluaran saat ini sengaja
berbeda dari referensi pada footer NexaPlay ID; aturan filtering tetap sama.
Pengujian service memakai respons HTTP fixture dan folder
Steam sementara; tidak memasang game ke instalasi Steam pengguna.
