#!/usr/bin/env bash
set -euo pipefail

admin_cidr="${SSH_ADMIN_CIDR:-}"
if [[ -z "$admin_cidr" ]]; then
    echo "Set SSH_ADMIN_CIDR to your trusted office/VPN CIDR before running this script." >&2
    exit 2
fi

sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow from "$admin_cidr" to any port 22 proto tcp comment 'restricted SSH'
sudo ufw allow 80/tcp comment 'HTTP redirect and ACME challenge'
sudo ufw allow 443/tcp comment 'HTTPS'
sudo ufw allow 443/udp comment 'HTTP/3'
sudo ufw --force enable
sudo ufw status verbose
