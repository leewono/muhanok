"""Create separate play and planning downloads without local caches or raw Mixamo."""
from pathlib import Path
import hashlib
import json
import zipfile

PROJECT = Path(__file__).resolve().parents[2]
OUTPUT = PROJECT.parent / "Distribution"


def package():
    OUTPUT.mkdir(exist_ok=True)
    build = PROJECT / "Builds/Windows"
    for required in ("Muhanok.exe", "UnityPlayer.dll", "Muhanok_Data/boot.config", "MonoBleedingEdge"):
        if not (build / required).exists():
            raise RuntimeError(f"Required game component missing: {required}")
    entries = {}
    for path in build.rglob("*"):
        if not path.is_file() or any("DoNotShip" in part for part in path.parts):
            continue
        if path.suffix.lower() in (".pdb", ".log"):
            continue
        entries[f"Game/{path.relative_to(build).as_posix()}"] = path
    entries["THIRD_PARTY_NOTICES.md"] = PROJECT / "THIRD_PARTY_NOTICES.md"
    entries["docs/FREE_ASSETS_IMPORTED.json"] = PROJECT / "docs/FREE_ASSETS_IMPORTED.json"
    entries["References/MixamoDownloads/Provenance.json"] = PROJECT / "References/MixamoDownloads/Provenance.json"
    for path in (PROJECT / "Unity/Assets/External/Staging/Kenney").rglob("*"):
        if path.is_file() and path.name.lower().startswith("license") and path.suffix != ".meta":
            entries[f"SOURCES/Kenney/{path.parent.name}/{path.name}"] = path
    launcher = '@echo off\r\nsetlocal\r\nstart "" /d "%~dp0Game" "%~dp0Game\\Muhanok.exe" %*\r\n'
    start_guide = """# 무한옥 Windows 실행 안내

1. ZIP 전체를 압축 해제합니다.
2. `StartGame.cmd`를 더블클릭합니다. 실제 게임은 `Game/Muhanok.exe`입니다.
3. **게임 시작**을 누르고 이름(순위표), 몸무게(선택), 플레이 방식을 고른 뒤 **시작하기**를 누릅니다.

키보드: A/D 또는 좌우 화살표는 이동, Space는 점프, S 또는 ↓는 숙이기, W 또는 ↑는 하이니입니다. 수레에서 A/D는 몸 기울이기입니다. Game 폴더 전체를 유지하세요. Unity·Python은 필요 없습니다.

웹캠 몸동작은 게임 안에서 바로 동작합니다. 시작 화면에서 **전신 / 상체 / 키보드** 중 하나를 고르고, 카메라 미리보기에 몸이 보이게 서세요. Python 설치는 필요 없습니다.

이 배포는 게임 아트 V10 + 2026-10-05 운동·가속 기능을 포함합니다. 최신 기획서는 별도 **Muhanok-Planning-V13.zip**에 있습니다. [소스와 폴더별 안내](https://github.com/kihwan04/PrisonRunner/blob/develop/PrisonRunner/START_HERE.md)를 확인하세요.
"""
    manifest = {name: hashlib.sha256(path.read_bytes()).hexdigest() for name, path in sorted(entries.items())}
    manifest["StartGame.cmd"] = hashlib.sha256(launcher.encode("ascii")).hexdigest()
    manifest["START_HERE.md"] = hashlib.sha256(start_guide.encode("utf-8")).hexdigest()
    readme = "무한옥 Windows 실행용\r\n\r\nZIP 전체를 압축 해제하고 StartGame.cmd를 더블클릭하세요.\r\n게임 시작 → 시작하기를 누르세요.\r\n키보드: A/D 이동, Space 점프, S 숙이기, W 하이니.\r\n실행에 필요한 Game 폴더 전체를 유지하세요.\r\n웹캠 플레이는 시작 화면에서 전신 또는 상체를 고르면 됩니다.\r\n최신 기획서는 별도 Muhanok-Planning-V13.zip에 있습니다.\r\n"
    with zipfile.ZipFile(OUTPUT / "Muhanok-Windows-x64.zip", "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for name, path in sorted(entries.items()):
            archive.write(path, name)
        archive.writestr("StartGame.cmd", launcher)
        archive.writestr("START_HERE.md", start_guide)
        archive.writestr("처음읽기.txt", readme.encode("utf-8-sig"))
        archive.writestr("SHA256.json", json.dumps(manifest, indent=2))
    with zipfile.ZipFile(OUTPUT / "Muhanok-Planning-V13.zip", "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        path = PROJECT / "docs/무한옥_화면별_게임기획서_V13.docx"
        archive.write(path, path.name)
        archive.write(PROJECT / "docs/SCREEN_PLAN_V13_QA.md", "SCREEN_PLAN_V13_QA.md")
        archive.writestr("처음읽기.txt", "최신 화면별 기획서는 V13입니다. 포함된 DOCX를 Word에서 여세요.\n게임 아트 V10과 문서 V13은 서로 다른 버전입니다.\n".encode("utf-8-sig"))
    for path in sorted(OUTPUT.glob("Muhanok-*.zip")):
        with zipfile.ZipFile(path) as archive:
            if archive.testzip() is not None:
                raise RuntimeError(f"ZIP integrity failed: {path.name}")
            if any(".venv/" in name or "DoNotShip" in name or name.lower().endswith(".fbx") for name in archive.namelist()):
                raise RuntimeError(f"Unexpected distribution file: {path.name}")
        print(f"{path.name}: {path.stat().st_size} bytes, SHA256 {hashlib.sha256(path.read_bytes()).hexdigest()}")
    hashes = "\n".join(f"{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.name}" for path in sorted(OUTPUT.glob("Muhanok-*.zip"))) + "\n"
    (OUTPUT / "SHA256SUMS.txt").write_text(hashes, encoding="ascii")


if __name__ == "__main__":
    package()
