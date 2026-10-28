"""Regenerate tiny, inert password-test archives using installed WinRAR and 7-Zip."""
from pathlib import Path
import os
import subprocess
import shutil
import tempfile

fixtures = Path(__file__).resolve().parent
rar = Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "WinRAR/Rar.exe"
seven_zip = Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "7-Zip/7z.exe"
if not rar.is_file() or not seven_zip.is_file():
    raise SystemExit("Fixture regeneration requires installed WinRAR and 7-Zip; running tests does not.")

with tempfile.TemporaryDirectory(prefix="nexaplay-password-fixtures-") as temporary:
    work = Path(temporary).resolve()
    assert work.parent == Path(tempfile.gettempdir()).resolve()
    assert work.name.startswith("nexaplay-password-fixtures-")
    (work / "42").mkdir()
    (work / "42/payload.txt").write_bytes(b"Online-Fix password fixture.\n")
    commands = [
        ("plain.rar", [str(rar), "a", "-cfg-", "-ma5", "-m0", "-idq", "-r", "-y"]),
        ("default.rar", [str(rar), "a", "-cfg-", "-ma5", "-m0", "-idq", "-r", "-y", "-ponline-fix.me"]),
        ("default-headers.rar", [str(rar), "a", "-cfg-", "-ma5", "-m0", "-idq", "-r", "-y", "-hponline-fix.me"]),
        ("custom-headers.rar", [str(rar), "a", "-cfg-", "-ma5", "-m0", "-idq", "-r", "-y", "-hpadi83geel"]),
        ("default-aes.zip", [str(seven_zip), "a", "-tzip", "-mem=AES256", "-ponline-fix.me", "-bd", "-bb0", "-y"]),
        ("custom-headers.7z", [str(seven_zip), "a", "-t7z", "-mhe=on", "-padi83geel", "-bd", "-bb0", "-y"]),
    ]
    for name, args in commands:
        output = work / name
        result = subprocess.run([*args, str(output), "42"], cwd=work, capture_output=True)
        if result.returncode != 0:
            raise SystemExit(f"Could not create fixture {name} (exit {result.returncode}).")
        shutil.copyfile(output, fixtures / name)
        print(f"Generated {name}: {(fixtures / name).stat().st_size} bytes")
