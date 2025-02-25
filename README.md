# CommentSystemSolution

# Comment System Setup Guide

## Project Overview
This repository contains a project implementing a **Single Page Application (SPA)** with a **comment feed**. 

### Architecture
- **Client (Angular)**: SPA application that listens for new comments via **WebSockets** from the `saver` service.
- **Microservices (.NET 9)**:
  - **app**: REST API service for receiving comments from the client, validating comments and CAPTCHAs, handling file uploads, forming comment objects, and sending them to the queue. It also provides a GraphQL API with pagination, sorting, and filtering for retrieving comments.
  - **saver**: Processes the comment queue and notifies the client of new comments via **WebSockets**.
  - **captcha**: Generates CAPTCHAs and stores them in a Redis cache.

The repository includes a **`docker-compose.yml`** file for automatic deployment of the entire project.
You will need a connection to your Azure cloud storage.

## Installation and Setup

### 1. Clone the Repository
```sh
# Navigate to the projects directory (create it if it does not exist)
mkdir -p ~/projects && cd ~/projects

# Clone the repository
git clone https://github.com/LarecOstrov/CommentSystemSolution.git
cd CommentSystemSolution
```
Add Azure cloud storage credentials:
```sh
# Open for edit .env file
sudo nano .env
```
Add AZURE_STORAGE_CONNECTION and AZURE_STORAGE_CONTAINER values.

Save and exit (`CTRL+O`, `Enter`, `CTRL+X`).

Check the .env file:
```sh
# Open for revision .env file
sudo cat .env
```


### 2. Install Docker and Docker Compose
#### Windows & macOS
Follow the installation guide from the official Docker documentation:
[Docker Installation Guide](https://docs.docker.com/desktop/setup/install/)

#### Linux (Ubuntu)
```sh
sudo apt update
sudo apt install -y docker.io docker-compose
sudo systemctl enable --now docker
sudo usermod -aG docker $USER
```
Verify installation:
```sh
docker --version
docker-compose --version
```
Ensure Docker Compose version is **2.0 or higher**.

#### Upgrading Docker Compose (Linux)
```sh
# Remove old version
sudo apt remove docker-compose

# Verify removal
docker-compose version  # If command still works, remove manually
sudo rm -f /usr/local/bin/docker-compose

# Install the latest version
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```
If Docker Compose does not work, try running:
```sh
/usr/local/bin/docker-compose version
```
Or add it to the PATH permanently:
```sh
echo 'export PATH=$PATH:/usr/local/bin' >> ~/.bashrc
source ~/.bashrc
```

---

## Running the Application

### 1. Local Development Setup
Use the **Development** environment settings:
```sh
cp CommentSystemSolution/CommentSystemClient/src/assets/environment.Development.json \
   CommentSystemSolution/CommentSystemClient/src/assets/environment.Production.json
```

### 2. Start the Application with Docker
Navigate to the directory containing `docker-compose.yml`:
```sh
cd CommentSystemSolution
```
Build and run containers:
```sh
docker-compose up --build -d
```

Verify that all services are running:
```sh
docker ps -a
```
Expected output:
```
✔ app                                        Built                                                                                                   0.0s 
✔ captcha                                    Built                                                                                                   0.0s 
✔ client                                     Built                                                                                                   0.0s 
✔ saver                                      Built                                                                                                   0.0s 
✔ Container rabbitmq_server                  Healthy                                                                                                13.6s 
✔ Container sql_server                       Healthy                                                                                                21.1s 
✔ Container redis_cache                      Healthy                                                                                                13.6s 
✔ Container commentsystemsolution-saver-1    Healthy                                                                                                23.5s 
✔ Container commentsystemsolution-captcha-1  Healthy                                                                                                18.0s 
✔ Container commentsystemsolution-app-1      Healthy                                                                                                54.3s 
✔ Container commentsystemsolution-client-1   Started
```

### 3. Check Running Services
```sh
docker ps -a  # All services should be healthy

docker logs -f <CONTAINER_ID>  # Check logs
```
Test API endpoints:
```sh
# GraphQL API
curl http://localhost:5000/graphql

# Angular client
curl http://localhost:4200
```

---

## Deploying with Nginx
### 1. Configure Nginx Proxy
Ensure the correct **Production** environment settings are in place:
```json
{
  "production": true,
  "addCommentRest": "/api/comments/",
  "getCommentsGraphQL": "/graphql",
  "getCaptchaRest": "/captcha/generate",
  "getWebSocket": "/ws"
}
```

Create an Nginx configuration file:
```sh
sudo nano /etc/nginx/sites-available/commentsystem
```
Add the following configuration:
```nginx
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://localhost:4200;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    location /api/comments/ {
        proxy_pass http://localhost:5000/api/comments/;
    }

    location /graphql {
        proxy_pass http://localhost:5000/graphql;
    }

    location /captcha/ {
        proxy_pass http://localhost:5004/api/captcha/;
    }

    location /ws {
        proxy_pass http://localhost:5002/ws;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
    }
}
```
Save and exit (`CTRL+O`, `Enter`, `CTRL+X`).

### 2. Enable Nginx Configuration
```sh
sudo ln -s /etc/nginx/sites-available/commentsystem /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
```
Verify configuration:
```sh
sudo nginx -t
```
If syntax is OK, restart Nginx:
```sh
sudo systemctl restart nginx
```
Your site should now be accessible at `http://<YOUR_IP>/`.

---

## Stopping and Cleaning Up
```sh
docker-compose down  # Stop containers

# Remove containers and images
docker ps -aq | xargs docker rm
docker images -q | xargs docker rmi -f
```
Remove only project images:
```sh
docker images | grep commentsystemsolution | awk '{print $3}' | xargs docker rmi -f
```

This guide provides a step-by-step process for setting up, running, deploying, and maintaining the **Comment System Solution** using Docker and Nginx.

