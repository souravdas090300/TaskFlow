#!/bin/bash

# TaskFlow CI/CD Setup Script
# This script helps you set up SSH keys and provides instructions for GitHub Actions

set -e

echo "=========================================="
echo "TaskFlow CI/CD Setup Script"
echo "=========================================="
echo ""

# Check if SSH key already exists
if [ -f ~/.ssh/taskflow_deploy ]; then
    echo "⚠️  SSH key already exists at ~/.ssh/taskflow_deploy"
    echo "Do you want to generate a new key? (y/n)"
    read -r response
    if [ "$response" != "y" ]; then
        echo "Using existing key"
        KEY_PATH="~/.ssh/taskflow_deploy"
    else
        rm ~/.ssh/taskflow_deploy ~/.ssh/taskflow_deploy.pub
        KEY_PATH="~/.ssh/taskflow_deploy"
    fi
else
    KEY_PATH="~/.ssh/taskflow_deploy"
fi

# Generate SSH key if it doesn't exist
if [ ! -f ~/.ssh/taskflow_deploy ]; then
    echo "🔑 Generating SSH key pair..."
    ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/taskflow_deploy -N ""
    echo "✅ SSH key generated"
fi

echo ""
echo "=========================================="
echo "STEP 1: Copy Public Key to VPS"
echo "=========================================="
echo ""
echo "Run this command to copy your public key to the VPS:"
echo ""
echo "  ssh-copy-id -i ~/.ssh/taskflow_deploy.pub YOUR_VPS_USER@YOUR_VPS_IP"
echo ""
echo "Replace YOUR_VPS_USER with your VPS username (e.g., ubuntu, root)"
echo "Replace YOUR_VPS_IP with your VPS IP address (e.g., 77.25.144.193)"
echo ""
read -p "Press Enter after you've copied the key to the VPS..."

echo ""
echo "=========================================="
echo "STEP 2: Test SSH Connection"
echo "=========================================="
echo ""
echo "Run this command to test the SSH connection:"
echo ""
echo "  ssh -i ~/.ssh/taskflow_deploy YOUR_VPS_USER@YOUR_VPS_IP"
echo ""
read -p "Press Enter after testing the connection..."

echo ""
echo "=========================================="
echo "STEP 3: Copy Private Key for GitHub"
echo "=========================================="
echo ""
echo "Your private key content (copy this to GitHub secret VPS_SSH_PRIVATE_KEY):"
echo ""
echo "=========================================="
cat ~/.ssh/taskflow_deploy
echo "=========================================="
echo ""
echo "Copy the entire output above (including BEGIN/END lines)"
echo ""

echo ""
echo "=========================================="
echo "STEP 4: GitHub Secrets Required"
echo "=========================================="
echo ""
echo "Add these secrets to your GitHub repository:"
echo "  Settings → Secrets and variables → Actions → New repository secret"
echo ""
echo "Required secrets:"
echo "  1. VPS_HOST_IP          - Your VPS IP (e.g., 77.25.144.193)"
echo "  2. VPS_USERNAME         - VPS username (e.g., ubuntu)"
echo "  3. VPS_SSH_PRIVATE_KEY  - The private key shown above"
echo "  4. DB_PASSWORD         - Strong database password"
echo "  5. GOOGLE_CLIENT_ID     - Google OAuth client ID"
echo "  6. GOOGLE_CLIENT_SECRET - Google OAuth client secret"
echo "  7. DUCKDNS_SUBDOMAIN    - DuckDNS subdomain (taskflow-sourav)"
echo "  8. DUCKDNS_TOKEN        - DuckDNS token"
echo ""

echo ""
echo "=========================================="
echo "STEP 5: Enable GitHub Actions"
echo "=========================================="
echo ""
echo "Go to: Settings → Actions → General"
echo ""
echo "Under 'Actions permissions':"
echo "  ✅ Select 'Allow all actions and reusable workflows'"
echo ""
echo "Under 'Workflow permissions':"
echo "  ✅ Select 'Read and write permissions'"
echo "  ✅ Check 'Allow GitHub Actions to create and approve pull requests'"
echo ""

echo ""
echo "=========================================="
echo "STEP 6: Initial VPS Setup"
echo "=========================================="
echo ""
echo "SSH into your VPS and run:"
echo ""
echo "  mkdir -p ~/taskflow"
echo "  cd ~/taskflow"
echo "  git clone https://github.com/YOUR_USERNAME/TaskFlow.git ."
echo "  sudo apt update"
echo "  curl -fsSL https://get.docker.com -o get-docker.sh"
echo "  sudo sh get-docker.sh"
echo "  sudo apt install docker-compose-plugin"
echo "  sudo usermod -aG docker \$USER"
echo ""
echo "Then create .env file:"
echo ""
echo "  cat > .env << EOF"
echo "  DB_PASSWORD=your-strong-password"
echo "  GOOGLE_CLIENT_ID=your-client-id"
echo "  GOOGLE_CLIENT_SECRET=your-client-secret"
echo "  DUCKDNS_SUBDOMAIN=taskflow-sourav"
echo "  DUCKDNS_TOKEN=your-token"
echo "  EOF"
echo ""

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "For detailed instructions, see: docs/CI_CD_SETUP.md"
echo ""
echo "After setup, push to main branch to trigger deployment:"
echo "  git add ."
echo "  git commit -m 'Your message'"
echo "  git push origin main"
echo ""
