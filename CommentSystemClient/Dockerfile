# Use 20 version of Node.js
FROM node:20 AS build
WORKDIR /app

# Copy package.json and package-lock.json
COPY package.json package-lock.json ./
RUN npm install

# Copy all files
COPY . .
RUN npm run build --configuration=production

# Use nginx image
FROM nginx:alpine

# Remove default nginx website
RUN rm /etc/nginx/conf.d/default.conf

# Copу builded files to nginx
COPY --from=build /app/dist/comment-system-client/browser/. /usr/share/nginx/html/

# Add nginx configuration
COPY ./nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
