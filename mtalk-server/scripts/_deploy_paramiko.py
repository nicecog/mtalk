import os
import sys
import time
from pathlib import Path

import paramiko

ROOT = Path(__file__).resolve().parents[1]
ENV_FILE = ROOT / ".env.cafe24"
REMOTE_DIR = "/opt/mtalk"

UPLOADS = [
    (ROOT / "target" / "mtalk-server-1.0.0.jar", f"{REMOTE_DIR}/target/mtalk-server-1.0.0.jar"),
    (ROOT / "Dockerfile", f"{REMOTE_DIR}/Dockerfile"),
    (ROOT / "docker-compose.prod.yml", f"{REMOTE_DIR}/docker-compose.yml"),
    (ROOT / "docker" / "init-databases.sql", f"{REMOTE_DIR}/docker/init-databases.sql"),
    (ROOT / ".env.cafe24", f"{REMOTE_DIR}/.env"),
]


def load_env(path: Path) -> dict[str, str]:
    env: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        env[key.strip()] = value.strip()
    return env


def main() -> int:
    env = load_env(ENV_FILE)
    host = env["CAFE24_VPS_HOST"]
    port = int(env.get("CAFE24_VPS_PORT", "22"))
    user = env["CAFE24_VPS_USER"]
    password = env["CAFE24_VPS_PASSWORD"]

    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    print(f"Connecting to {host}:{port} ...")
    client.connect(host, port=port, username=user, password=password, timeout=30)

    sftp = client.open_sftp()
    try:
        stdin, stdout, stderr = client.exec_command(f"mkdir -p {REMOTE_DIR}/target {REMOTE_DIR}/docker")
        stdout.channel.recv_exit_status()

        for local, remote in UPLOADS:
            if not local.exists():
                print(f"Missing file: {local}", file=sys.stderr)
                return 1
            print(f"Upload {local.name} -> {remote}")
            temp_remote = f"{remote}.upload"
            sftp.put(str(local), temp_remote)
            stdin, stdout, stderr = client.exec_command(f"mv -f {temp_remote} {remote}")
            exit_code = stdout.channel.recv_exit_status()
            if exit_code != 0:
                err = stderr.read().decode("utf-8", errors="replace")
                print(f"Failed to move {temp_remote} -> {remote}: {err}", file=sys.stderr)
                return exit_code
    finally:
        sftp.close()

    deploy_cmd = (
        "cd /opt/mtalk && "
        "docker compose down 2>/dev/null || true; "
        "docker compose build --no-cache; "
        "docker compose up -d; "
        "sleep 18; "
        "docker compose ps; "
        "curl -sf http://127.0.0.1:8080/health; "
        "echo; echo DEPLOY_OK"
    )
    print("Running remote deploy ...")
    stdin, stdout, stderr = client.exec_command(deploy_cmd, get_pty=True)
    out = stdout.read().decode("utf-8", errors="replace")
    err = stderr.read().decode("utf-8", errors="replace")
    exit_code = stdout.channel.recv_exit_status()
    safe_out = out.encode("cp949", errors="replace").decode("cp949", errors="replace")
    print(safe_out)
    if err.strip():
        safe_err = err.encode("cp949", errors="replace").decode("cp949", errors="replace")
        print(safe_err, file=sys.stderr)
    client.close()

    if exit_code != 0 or "DEPLOY_OK" not in out:
        print(f"Deploy failed (exit={exit_code})", file=sys.stderr)
        return exit_code or 1
    print("DEPLOY_OK on VPS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
