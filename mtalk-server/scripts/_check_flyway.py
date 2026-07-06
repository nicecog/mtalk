import paramiko
from pathlib import Path

env = {
    k.strip(): v.strip()
    for line in Path(r"C:\project\Unity_Projects\mtalk\.env.cafe24").read_text(encoding="utf-8").splitlines()
    if line.strip() and not line.startswith("#") and "=" in line
    for k, v in [line.strip().split("=", 1)]
}

client = paramiko.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect(
    env["CAFE24_VPS_HOST"],
    port=int(env.get("CAFE24_VPS_PORT", "22")),
    username=env["CAFE24_VPS_USER"],
    password=env["CAFE24_VPS_PASSWORD"],
    timeout=30,
)
cmd = (
    "docker exec mtalk-postgres psql -U mtalk -d mbody_revised -t -c "
    "\"SELECT version || ' ' || description FROM flyway_schema_history ORDER BY installed_rank DESC LIMIT 3;\""
)
_, stdout, _ = client.exec_command(cmd)
stdout.channel.recv_exit_status()
print(stdout.read().decode("utf-8", errors="replace"))
client.close()
