"""Validate task records and rebuild progress pointers without changing statuses."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TASK_DIR = ROOT / "Docs" / "Tasks"
STATES = {"NOT_STARTED", "IN_PROGRESS", "WAITING_VALIDATION", "BLOCKED", "DONE"}


def task_record(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    match = re.match(r"\A---\r?\n(.*?)\r?\n---\r?\n", text, re.S)
    if not match:
        raise ValueError(f"Missing task metadata: {path.name}")
    values = {}
    for line in match.group(1).splitlines():
        key, separator, value = line.partition(":")
        if not separator:
            raise ValueError(f"Invalid metadata: {path.name}: {line}")
        values[key.strip()] = value.strip()
    required = {"id", "title", "status", "depends_on", "validation"}
    if required - values.keys():
        raise ValueError(f"Missing fields: {path.name}: {required - values.keys()}")
    values["depends_on"] = json.loads(values["depends_on"])
    values["validation"] = json.loads(values["validation"])
    if (not isinstance(values["depends_on"], list)
            or not all(isinstance(x, str) for x in values["depends_on"])
            or not isinstance(values["validation"], str)):
        raise ValueError(f"Invalid dependency/validation type: {path.name}")
    values.update(path=path, text=text)
    return values


def section(text: str, heading: str) -> str:
    found = re.search(rf"^## {re.escape(heading)}\s*\n(.*?)(?=^## |\Z)", text, re.M | re.S)
    return found.group(1).strip() if found else "Xem file task."


def write_atomic(path: Path, text: str) -> None:
    temp = path.with_name(path.name + ".tmp")
    temp.write_text(text, encoding="utf-8", newline="\n")
    temp.replace(path)


def main() -> None:
    tasks = [task_record(path) for path in sorted(TASK_DIR.glob("[0-9]*.md"))]
    if not tasks:
        raise ValueError("No task files")
    ids = [task["id"] for task in tasks]
    if len(set(ids)) != len(ids):
        raise ValueError("Duplicate task IDs")
    by_id = dict(zip(ids, tasks))
    for index, task in enumerate(tasks):
        if task["status"] not in STATES:
            raise ValueError(f"Unknown status: {task['id']}")
        for dep in task["depends_on"]:
            if dep not in by_id or ids.index(dep) >= index:
                raise ValueError(f"Unknown, circular or out-of-order dependency: {task['id']} -> {dep}")
        if task["status"] == "IN_PROGRESS" and any(by_id[d]["status"] != "DONE" for d in task["depends_on"]):
            raise ValueError(f"Task started before dependencies DONE: {task['id']}")
        if task["status"] == "DONE":
            if re.search(r"^- \[ \]", task["text"], re.M):
                raise ValueError(f"Unchecked criteria in DONE task: {task['id']}")
            record = (ROOT / task["validation"]).resolve()
            if not task["validation"] or not record.is_relative_to(ROOT / "Docs" / "Validation") or not record.is_file():
                raise ValueError(f"Missing valid validation file for DONE task: {task['id']}")
            report = record.read_text(encoding="utf-8")
            if not re.search(rf"^Task: {re.escape(task['id'])}\s*$", report, re.M) or not re.search(r"^Result: PASS\s*$", report, re.M):
                raise ValueError(f"Validation is not PASS for task: {task['id']}")
    unfinished = [t for t in tasks if t["status"] != "DONE"]
    active = [t for t in unfinished if t["status"] == "IN_PROGRESS"]
    if len(active) > 1 or (active and active[0] is not unfinished[0]):
        raise ValueError("Multiple active tasks or an earlier unfinished task was skipped")

    progress = ["# Tiến độ Cây Thế Giới", "", "File tổng hợp tự động từ metadata task. Không sửa status ở đây.", "",
                "| Task | Công việc | Trạng thái | Phụ thuộc |", "| --- | --- | --- | --- |"]
    for t in tasks:
        progress.append(f"| [{t['id']}]({t['path'].name}) | {t['title']} | {t['status']} | {', '.join(t['depends_on']) or 'Không'} |")
    progress += ["", f"Đã nghiệm thu: {len(tasks) - len(unfinished)}/{len(tasks)} task.", "",
                 "Có tài liệu hoặc script chưa chứng minh gameplay đã chạy. Chỉ DONE khi kiểm tra thực tế đạt.", ""]
    current = ["# Công việc hiện tại", "", "Con trỏ tổng hợp từ file task. Không sửa trạng thái riêng tại đây.", ""]
    if unfinished:
        task = unfinished[0]
        relative = task["path"].relative_to(ROOT).as_posix()
        deps = ", ".join(f"{d} ({by_id[d]['status']})" for d in task["depends_on"]) or "Không"
        following = unfinished[1]["id"] if len(unfinished) > 1 else "Lập task đồ họa sau nghiệm thu khung"
        current += [f"Task: [{task['id']} {task['title']}]({relative})", f"Trạng thái: {task['status']}",
                    f"Phụ thuộc: {deps}", "", "## Mục tiêu", "", section(task["text"], "Mục tiêu"),
                    "", "## Definition of Done", "", "Hoàn tất checklist trong file task và AGENTS.md: compile, Unity Play Mode, acceptance, hồi quy, Validation PASS, bằng chứng và bàn giao.",
                    "", "## Acceptance hiện tại", "", section(task["text"], "Acceptance"),
                    "", "## Lỗi và bàn giao", "", section(task["text"], "Bàn giao"),
                    "", "## Tiếp theo", "", f"{following}. Chỉ bắt đầu sau khi task hiện tại DONE.", ""]
    else:
        current += ["Mọi task khung đã DONE theo metadata và Validation. Lập task đồ họa Blender với phạm vi và nghiệm thu trước khi làm.", ""]
    write_atomic(TASK_DIR / "Progress.md", "\n".join(progress))
    write_atomic(ROOT / "CURRENT_TASK.md", "\n".join(current))
    print(f"Validated {len(tasks)} tasks; {len(tasks) - len(unfinished)} DONE. Current: {unfinished[0]['id'] if unfinished else 'none'}")


if __name__ == "__main__":
    main()
