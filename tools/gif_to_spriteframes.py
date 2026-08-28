#!/usr/bin/env python3
"""Convert a GIF into PNG frames plus a Godot 4 SpriteFrames .tres resource."""

from __future__ import annotations

import argparse
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageSequence


def find_project_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / "project.godot").is_file():
            return candidate
    raise FileNotFoundError(
        "Could not find project.godot. Run this tool inside the Godot project."
    )


def godot_string(value: str) -> str:
    return value.replace("\\", "/").replace('"', '\\"')


def build_tres(
    frame_paths: Iterable[str],
    durations_ms: Iterable[int],
    loop: bool,
) -> str:
    frame_paths = list(frame_paths)
    durations_ms = list(durations_ms)
    load_steps = len(frame_paths) + 1

    lines = [f"[gd_resource type=\"SpriteFrames\" load_steps={load_steps} format=3]", ""]

    for index, resource_path in enumerate(frame_paths, start=1):
        lines.append(
            "[ext_resource type=\"Texture2D\" "
            f"path=\"{godot_string(resource_path)}\" id=\"{index}_frame\"]"
        )

    lines.extend(["", "[resource]", "animations = [{", '"frames": ['])

    for index, duration_ms in enumerate(durations_ms, start=1):
        suffix = "," if index < len(frame_paths) else ""
        lines.extend(
            [
                "{",
                f'"duration": {duration_ms}.0,',
                f'"texture": ExtResource("{index}_frame")',
                f"}}{suffix}",
            ]
        )

    lines.extend(
        [
            "],",
            f'"loop": {str(loop).lower()},',
            '"name": &"default",',
            '"speed": 1000.0',
            "}]",
            "",
        ]
    )

    return "\n".join(lines)


def convert(input_path: Path, output_dir: Path, resource_name: str | None) -> Path:
    input_path = input_path.resolve()
    output_dir = output_dir.resolve()
    project_root = find_project_root(output_dir)

    try:
        output_dir.relative_to(project_root)
    except ValueError as error:
        raise ValueError("Output directory must be inside the Godot project.") from error

    output_dir.mkdir(parents=True, exist_ok=True)
    stem = resource_name or input_path.stem

    for old_frame in output_dir.glob(f"{stem}_frame_*.png"):
        old_frame.unlink()

    durations_ms: list[int] = []
    frame_files: list[Path] = []

    with Image.open(input_path) as image:
        loop = image.info.get("loop", 1) == 0

        for index, frame in enumerate(ImageSequence.Iterator(image)):
            duration_ms = int(frame.info.get("duration", image.info.get("duration", 100)))
            duration_ms = max(duration_ms, 1)
            frame_path = output_dir / f"{stem}_frame_{index:03d}.png"

            frame.convert("RGBA").save(frame_path)
            frame_files.append(frame_path)
            durations_ms.append(duration_ms)

    if not frame_files:
        raise ValueError(f"GIF contains no frames: {input_path}")

    resource_paths = [
        "res://" + frame.relative_to(project_root).as_posix()
        for frame in frame_files
    ]

    tres_path = output_dir / f"{stem}_frames.tres"
    tres_path.write_text(
        build_tres(resource_paths, durations_ms, loop),
        encoding="utf-8",
    )

    return tres_path


def main() -> None:
    parser = argparse.ArgumentParser(
        description=(
            "Convert a GIF to Godot-importable PNG frames and a SpriteFrames .tres."
        )
    )
    parser.add_argument("input_gif", type=Path)
    parser.add_argument("output_dir", type=Path)
    parser.add_argument(
        "--name",
        dest="resource_name",
        help="Optional output resource name; defaults to the GIF filename.",
    )
    args = parser.parse_args()

    result = convert(args.input_gif, args.output_dir, args.resource_name)
    print(result)


if __name__ == "__main__":
    main()
